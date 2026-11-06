using System.Collections;
using UnityEngine;
using RelicTrail.Combat;
using RelicTrail.UI;

namespace RelicTrail.Player
{
    public enum AttackType
    {
        None,
        Light,
        Heavy
    }

    /// <summary>
    /// Firearm combat controller for 'The Lost Relic'.
    /// Features:
    /// - 3D Gun model mounted to character's right hand with muzzle flash, recoil kickback, and procedural audio.
    /// - Light Attack (LMB): Semi-auto gunshot, fast, 25 damage, 10 stamina, bullet tracer to crosshair.
    /// - Heavy Attack (RMB): Charged high-caliber blast, 65 damage, 28 stamina, camera shake & heavy impact.
    /// - Screen-center crosshair aiming with automatic lock-on tracking support.
    /// - Continuous collision bullet tracers that reliably damage IDamageable enemies.
    /// </summary>
    [RequireComponent(typeof(PlayerInputHandler))]
    [AddComponentMenu("Relic Trail/Player/Player Attack")]
    public class PlayerAttack : MonoBehaviour
    {
        [Header("Light Attack Settings (LMB)")]
        [SerializeField] private float lightDamage = 25f;
        [SerializeField] private float lightStaminaCost = 10f;
        [SerializeField] private float lightCooldown = 0.20f;

        [Header("Super Charged Attack Settings (Q)")]
        [SerializeField] private float superChargedDamage = 85f;
        [SerializeField] private float superChargedStaminaCost = 30f;
        [SerializeField] private float superChargedChargeDuration = 0.45f;
        [SerializeField] private float superChargedRecoveryDuration = 0.25f;
        [SerializeField] private float superChargedCooldown = 1.0f;
        [SerializeField] private float superChargedCameraShakeIntensity = 0.28f;

        [Header("Aim & Hit Detection")]
        [SerializeField] private float maxAimDistance = 80f;
        [SerializeField] private LayerMask hitLayers = ~0;

        [Header("Weapon Visuals & Audio")]
        [SerializeField] private Transform weaponHolder;
        [SerializeField] private GunWeaponVisual gunVisual;
        [SerializeField] private AudioClip customLightShotSound;
        [SerializeField] private AudioClip customHeavyShotSound;

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private string animShootingParam = "IsShooting";
#pragma warning disable 0414
        [SerializeField] private string animAttackTrigger = "";
#pragma warning restore 0414

        // Components
        private PlayerInputHandler input;
        private PlayerStamina stamina;
        private PlayerMovement movement;
        private ThirdPersonCamera tpCamera;
        private AudioSource audioSource;
        private int shootingHash;

        // State
        private AttackType currentAttack = AttackType.None;
        private bool isAttacking;
        private float lastAttackTime;
        private float lastSuperChargedTime;

        public event System.Action OnAttackFired;
        public bool IsAttacking => isAttacking;
        public AttackType CurrentAttack => currentAttack;
        public float HeavyDamage => superChargedDamage;
        public float SuperChargedDamage => superChargedDamage;

        private void Awake()
        {
            input = GetComponent<PlayerInputHandler>();
            stamina = GetComponent<PlayerStamina>();
            movement = GetComponent<PlayerMovement>();
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (animator != null && !string.IsNullOrEmpty(animShootingParam))
            {
                shootingHash = Animator.StringToHash(animShootingParam);
            }

            SetupGunVisual();
        }

        private void Start()
        {
            FindCamera();
            CrosshairUI.EnsureCrosshairExists();
        }

        private void FindCamera()
        {
            if (tpCamera == null && Camera.main != null)
            {
                tpCamera = Camera.main.GetComponent<ThirdPersonCamera>();
            }
        }

        /// <summary>
        /// Mounts and configures the 3D gun on the player's right hand bone.
        /// </summary>
        public void SetupGunVisual()
        {
            // Remove any legacy weapon objects (e.g. old axe)
            var existingWeapons = GetComponentsInChildren<Transform>(true);
            foreach (var t in existingWeapons)
            {
                if (t != null && (t.name.Contains("Axe") || t.name.Contains("WeaponVisual") || t.name.Contains("WeaponHolder") || t.name == "Gun_Model"))
                {
                    if (Application.isPlaying) Destroy(t.gameObject);
                    else DestroyImmediate(t.gameObject);
                }
            }

            // Find right hand bone
            Transform rightHand = null;
            if (animator != null && animator.isHuman)
            {
                rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            }

            if (rightHand == null)
            {
                Transform[] allBones = GetComponentsInChildren<Transform>(true);
                foreach (var b in allBones)
                {
                    string bName = b.name.ToLower();
                    if (bName.EndsWith("end")) continue;
                    if (bName == "hand.r" || bName == "hand_r" || bName == "r_hand" || bName == "r-hand" || bName == "hand (r)" || bName == "r_weapon")
                    {
                        rightHand = b;
                        break;
                    }
                }

                if (rightHand == null)
                {
                    foreach (var b in allBones)
                    {
                        string bName = b.name.ToLower();
                        if (bName.EndsWith("end")) continue;
                        if (bName.Contains("hand") && (bName.Contains(".r") || bName.Contains("_r") || bName.EndsWith("r")))
                        {
                            rightHand = b;
                            break;
                        }
                    }
                }
            }

            if (rightHand == null)
            {
                // Fallback mount on right side of player torso
                GameObject holder = new GameObject("WeaponHolder");
                holder.transform.SetParent(transform);
                holder.transform.localPosition = new Vector3(0.28f, 1.15f, 0.25f);
                rightHand = holder.transform;
            }

            weaponHolder = rightHand;

            // Attach GunWeaponVisual component onto right hand
            gunVisual = weaponHolder.GetComponent<GunWeaponVisual>();
            if (gunVisual == null)
            {
                gunVisual = weaponHolder.gameObject.AddComponent<GunWeaponVisual>();
            }
            else
            {
                gunVisual.BuildGunModel();
            }

            // Ensure PlayerWeaponIK is attached to the Animator GameObject for Two-Handed IK
            if (animator != null)
            {
                var ik = animator.GetComponent<PlayerWeaponIK>();
                if (ik == null)
                {
                    ik = animator.gameObject.AddComponent<PlayerWeaponIK>();
                }
                ik.SetWeaponVisual(gunVisual);
            }
        }

        private void Update()
        {
            if (RelicTrail.Core.GameManager.Instance != null && RelicTrail.Core.GameManager.Instance.CurrentGameState != RelicTrail.Core.GameState.Playing)
            {
                return;
            }

            FindCamera();

            if (isAttacking) return;

            // Q Key: Super Charged Attack (Charging visual FX -> powerful high-damage blast)
            if (input != null && input.SuperChargedAttackTriggered)
            {
                input.ConsumeSuperChargedAttack();
                TryStartSuperChargedAttack();
                return;
            }

            // LMB: Shoot / Light Attack (Semi-auto gunshot)
            if (input != null && input.LightAttackTriggered)
            {
                input.ConsumeLightAttack();
                TryStartLightAttack();
                return;
            }
        }

        private void TryStartLightAttack()
        {
            if (Time.time < lastAttackTime + lightCooldown) return;

            float cost = stamina != null ? stamina.LightAttackCost : lightStaminaCost;
            if (stamina != null && !stamina.TryConsumeStamina(cost))
            {
                return;
            }

            lastAttackTime = Time.time;
            StartCoroutine(LightAttackRoutine());
        }

        public void TryStartSuperChargedAttack()
        {
            if (isAttacking) return;
            if (Time.time < lastSuperChargedTime + superChargedCooldown) return;

            float cost = stamina != null ? stamina.HeavyAttackCost : superChargedStaminaCost;
            if (stamina != null && !stamina.TryConsumeStamina(cost))
            {
                return;
            }

            lastSuperChargedTime = Time.time;
            StartCoroutine(SuperChargedAttackRoutine());
        }

        public void TryStartHeavyAttack()
        {
            TryStartSuperChargedAttack();
        }

        private IEnumerator LightAttackRoutine()
        {
            isAttacking = true;
            currentAttack = AttackType.Light;

            // Align player rotation towards aim direction
            Vector3 aimTarget = GetAimTargetPoint();
            AlignFacing(aimTarget);

            // Trigger firing feedback (gun recoil kick + muzzle flash)
            if (gunVisual != null)
            {
                gunVisual.TriggerFireFeedback(false);
            }

            if (animator != null && shootingHash != 0)
            {
                animator.SetBool(shootingHash, true);
            }

            // Firing feedback (recoil kick + muzzle flash) is handled by gunVisual; no melee punch animation.

            // Play gunshot audio
            AudioClip shotSfx = customLightShotSound ?? gunVisual?.LightShotSound;
            if (shotSfx != null && audioSource != null)
            {
                audioSource.PlayOneShot(shotSfx, 0.85f);
            }

            // Expand crosshair on recoil
            OnAttackFired?.Invoke();

            // Spawn fast bullet tracer towards actual aim target
            Vector3 muzzlePos = gunVisual != null && gunVisual.MuzzlePoint != null
                ? gunVisual.MuzzlePoint.position
                : transform.position + transform.forward * 0.5f + Vector3.up * 1.2f;

            Vector3 shotDirection = (aimTarget - muzzlePos).normalized;
            BulletProjectile.Spawn(muzzlePos, shotDirection, lightDamage, false, gameObject, hitLayers);

            yield return new WaitForSeconds(lightCooldown);

            if (animator != null && shootingHash != 0)
            {
                animator.SetBool(shootingHash, false);
            }

            currentAttack = AttackType.None;
            isAttacking = false;
        }

        private IEnumerator SuperChargedAttackRoutine()
        {
            isAttacking = true;
            currentAttack = AttackType.Heavy;

            // Play charge audio sweep
            AudioClip chargeSfx = gunVisual?.ChargeSound;
            if (chargeSfx != null && audioSource != null)
            {
                audioSource.PlayOneShot(chargeSfx, 0.85f);
            }

            // 1. Charge wind-up phase: pulsing energy glow at muzzle, tracking target
            float chargeTimer = 0f;
            while (chargeTimer < superChargedChargeDuration)
            {
                chargeTimer += Time.deltaTime;
                float progress = Mathf.Clamp01(chargeTimer / superChargedChargeDuration);

                Vector3 currentAimTarget = GetAimTargetPoint();
                AlignFacing(currentAimTarget);

                if (gunVisual != null)
                {
                    gunVisual.SetChargeEffect(true, progress);
                }

                yield return null;
            }

            // Turn off charge visual before detonation
            if (gunVisual != null)
            {
                gunVisual.SetChargeEffect(false);
                gunVisual.TriggerFireFeedback(true);
            }

            // Firing feedback (recoil kick + muzzle flash) is handled by gunVisual; no melee punch animation.

            // 2. Fire Devastating High-Damage Blast
            Vector3 finalAimTarget = GetAimTargetPoint();
            AlignFacing(finalAimTarget);

            AudioClip heavySfx = customHeavyShotSound ?? gunVisual?.HeavyShotSound;
            if (heavySfx != null && audioSource != null)
            {
                audioSource.PlayOneShot(heavySfx, 1.0f);
            }

            // Noticeable camera shake on heavy hit blast
            if (tpCamera != null)
            {
                tpCamera.TriggerShake(0.25f, superChargedCameraShakeIntensity);
            }

            OnAttackFired?.Invoke();

            // Spawn heavy high-impact projectile dealing super charged damage
            Vector3 muzzlePos = gunVisual != null && gunVisual.MuzzlePoint != null
                ? gunVisual.MuzzlePoint.position
                : transform.position + transform.forward * 0.5f + Vector3.up * 1.2f;

            if (animator != null && shootingHash != 0)
            {
                animator.SetBool(shootingHash, true);
            }

            Vector3 shotDirection = (finalAimTarget - muzzlePos).normalized;
            BulletProjectile.Spawn(muzzlePos, shotDirection, superChargedDamage, true, gameObject, hitLayers);

            // 3. Recovery Phase
            yield return new WaitForSeconds(superChargedRecoveryDuration);

            if (animator != null && shootingHash != 0)
            {
                animator.SetBool(shootingHash, false);
            }

            currentAttack = AttackType.None;
            isAttacking = false;
        }

        private IEnumerator HeavyAttackRoutine()
        {
            yield return SuperChargedAttackRoutine();
        }

        /// <summary>
        /// Calculates the 3D world target point the gun should fire towards,
        /// prioritizing locked enemies or raycasting from screen center crosshair.
        /// </summary>
        public Vector3 GetAimTargetPoint()
        {
            // 1. If locked onto an enemy, aim directly at enemy chest level
            if (tpCamera != null && tpCamera.IsLockedOn && tpCamera.LockedTarget != null)
            {
                return tpCamera.LockedTarget.position + Vector3.up * 1.0f;
            }

            // 2. Screen center crosshair raycast
            Camera cam = Camera.main;
            if (cam != null)
            {
                Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                RaycastHit[] hits = Physics.RaycastAll(ray, maxAimDistance, hitLayers, QueryTriggerInteraction.Ignore);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                foreach (var h in hits)
                {
                    if (h.collider.gameObject == gameObject || h.collider.transform.IsChildOf(transform))
                    {
                        continue;
                    }
                    if (h.collider.isTrigger && h.collider.GetComponent<IDamageable>() == null)
                    {
                        continue;
                    }
                    return h.point;
                }

                // If raycast hits nothing in range, shoot forward into the distance
                return ray.origin + ray.direction * maxAimDistance;
            }

            // 3. Fallback: straight forward from player
            return transform.position + transform.forward * maxAimDistance + Vector3.up * 1.2f;
        }

        private void AlignFacing(Vector3 targetPoint)
        {
            Vector3 toTarget = targetPoint - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(toTarget.normalized);
            }
        }
    }
}
