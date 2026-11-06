using UnityEngine;

namespace RelicTrail.Player
{
    /// <summary>
    /// Two-Handed Weapon Inverse Kinematics (IK) Controller.
    /// Locks the character's left hand securely onto the firearm's foregrip / handguard.
    /// Works with Unity's Animator IK Pass on Humanoid avatars.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [AddComponentMenu("Relic Trail/Player/Player Weapon IK")]
    public class PlayerWeaponIK : MonoBehaviour
    {
        [Header("IK Targets")]
        [SerializeField] private Transform leftHandGrip;
        [SerializeField] private GunWeaponVisual weaponVisual;

        [Header("Weights")]
        [Range(0f, 1f)] [SerializeField] private float leftHandPositionWeight = 1.0f;
        [Range(0f, 1f)] [SerializeField] private float leftHandRotationWeight = 1.0f;

        private Animator animator;
        private PlayerHealth playerHealth;
        private float currentWeight = 1.0f;

        public Transform LeftHandGrip => leftHandGrip;
        public float CurrentWeight => currentWeight;

        public void SetGripTarget(Transform gripTarget)
        {
            leftHandGrip = gripTarget;
        }

        public void SetWeaponVisual(GunWeaponVisual visual)
        {
            weaponVisual = visual;
            if (weaponVisual != null && weaponVisual.LeftHandGripPoint != null)
            {
                leftHandGrip = weaponVisual.LeftHandGripPoint;
            }
        }

        private void Awake()
        {
            animator = GetComponent<Animator>();
            playerHealth = GetComponentInParent<PlayerHealth>();
        }

        private void Start()
        {
            FindGripTarget();
        }

        public void FindGripTarget()
        {
            if (leftHandGrip != null) return;

            if (weaponVisual == null)
            {
                weaponVisual = GetComponentInParent<PlayerAttack>()?.GetComponentInChildren<GunWeaponVisual>(true);
                if (weaponVisual == null)
                {
                    weaponVisual = GetComponentInChildren<GunWeaponVisual>(true);
                }
            }

            if (weaponVisual != null && weaponVisual.LeftHandGripPoint != null)
            {
                leftHandGrip = weaponVisual.LeftHandGripPoint;
            }
        }

        private void Update()
        {
            // If character is dead, smoothly release the IK grip
            if (playerHealth != null && playerHealth.IsDead)
            {
                currentWeight = Mathf.MoveTowards(currentWeight, 0f, Time.deltaTime * 6f);
            }
            else
            {
                currentWeight = Mathf.MoveTowards(currentWeight, 1f, Time.deltaTime * 6f);
            }

            if (leftHandGrip == null)
            {
                FindGripTarget();
            }
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (animator == null || !animator.isHuman) return;

            float weight = currentWeight;
            if (leftHandGrip != null && weight > 0.001f)
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, leftHandPositionWeight * weight);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, leftHandRotationWeight * weight);
                animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandGrip.position);
                animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandGrip.rotation);
            }
            else
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
            }
        }
    }
}
