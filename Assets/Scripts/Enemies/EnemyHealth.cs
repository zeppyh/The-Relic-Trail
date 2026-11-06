using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using RelicTrail.Core;
using RelicTrail.Combat;

namespace RelicTrail.Enemies
{
    /// <summary>
    /// Manages enemy health, damage taking, hit flashing, and defeat handling.
    /// </summary>
    [AddComponentMenu("Relic Trail/Enemies/Enemy Health")]
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 70f;
        [SerializeField] private float currentHealth;
        [SerializeField] private float flashDuration = 0.12f;

        [Header("Defeat & Feedback")]
        [SerializeField] private GameObject defeatEffectPrefab;
        [SerializeField] private AudioClip hitSound;
        [SerializeField] private AudioClip deathSound;
        [SerializeField] private float destroyDelay = 2.0f;

        [Header("Biome & Rewards")]
        [SerializeField] private BiomeType enemyBiome = BiomeType.ForgottenForest;
        [SerializeField] private int coinReward = 0;
        [SerializeField] private bool countTowardsForestMission = true;

        [Header("Coin Drops")]
        [SerializeField] private bool dropCoinsOnDeath = true;
        [SerializeField] private GameObject coinDropPrefab;
        [SerializeField] private int minCoinsToDrop = 1;
        [SerializeField] private int maxCoinsToDrop = 3;

        [Header("Respawn Settings")]
        [SerializeField] private bool respawnOnDeath = true;
        [SerializeField] private float respawnDelay = 15f;

        private bool isDead;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private List<Renderer> renderers = new List<Renderer>();
        private List<Color> originalColors = new List<Color>();

        public event Action<float, float> OnHealthChanged;
        public event Action OnDefeated;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsDead => isDead;
        public BiomeType EnemyBiome
        {
            get => enemyBiome;
            set => enemyBiome = value;
        }
        public bool CountTowardsForestMission
        {
            get => countTowardsForestMission;
            set => countTowardsForestMission = value;
        }
        public bool RespawnOnDeath
        {
            get => respawnOnDeath;
            set => respawnOnDeath = value;
        }

        public void SetRespawnOnDeath(bool enable) => respawnOnDeath = enable;

        private void Awake()
        {
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
            currentHealth = maxHealth;
            CacheRenderers();
        }

        private void CacheRenderers()
        {
            renderers.Clear();
            originalColors.Clear();
            var found = GetComponentsInChildren<Renderer>(true);
            foreach (var r in found)
            {
                if (r is ParticleSystemRenderer || r is TrailRenderer) continue;
                renderers.Add(r);
                if (r.material != null && r.material.HasProperty("_BaseColor"))
                {
                    originalColors.Add(r.material.GetColor("_BaseColor"));
                }
                else if (r.material != null && r.material.HasProperty("_Color"))
                {
                    originalColors.Add(r.material.GetColor("_Color"));
                }
                else
                {
                    originalColors.Add(Color.white);
                }
            }
        }

        public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
        {
            if (isDead) return;

            currentHealth = Mathf.Max(0f, currentHealth - amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (hitSound != null)
            {
                AudioSource.PlayClipAtPoint(hitSound, transform.position, 0.7f);
            }
            else if (AudioManager.Instance != null && AudioManager.Instance.MagicHitSfx != null)
            {
                AudioManager.Instance.PlayMagicHit(transform.position);
            }

            // Stagger / knockback flinch reaction
            if (hitDirection != Vector3.zero)
            {
                Vector3 knock = hitDirection.normalized * 0.35f;
                knock.y = 0f;
                transform.position += knock;
            }

            StartCoroutine(FlashRoutine());

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        private IEnumerator FlashRoutine()
        {
            SetColor(Color.red);
            yield return new WaitForSeconds(flashDuration);
            RestoreColor();
        }

        private void SetColor(Color col)
        {
            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] == null || renderers[i].material == null) continue;
                if (renderers[i].material.HasProperty("_BaseColor"))
                    renderers[i].material.SetColor("_BaseColor", col);
                else if (renderers[i].material.HasProperty("_Color"))
                    renderers[i].material.SetColor("_Color", col);
            }
        }

        private void RestoreColor()
        {
            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] == null || renderers[i].material == null) continue;
                if (i < originalColors.Count)
                {
                    if (renderers[i].material.HasProperty("_BaseColor"))
                        renderers[i].material.SetColor("_BaseColor", originalColors[i]);
                    else if (renderers[i].material.HasProperty("_Color"))
                        renderers[i].material.SetColor("_Color", originalColors[i]);
                }
            }
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;

            OnDefeated?.Invoke();

            // Drop physical gold coins on defeat for player to collect
            if (dropCoinsOnDeath)
            {
                DropCoins();
            }
            else if (coinReward > 0 && GameManager.Instance != null)
            {
                GameManager.Instance.AddCoins(coinReward);
            }

            // Record forest mission progress if applicable
            if (enemyBiome == BiomeType.ForgottenForest && countTowardsForestMission && GameManager.Instance != null)
            {
                GameManager.Instance.RecordForestEnemyKill();
            }

            if (deathSound != null)
            {
                AudioSource.PlayClipAtPoint(deathSound, transform.position, 0.85f);
            }
            else if (AudioManager.Instance != null && AudioManager.Instance.EnemyDefeatSfx != null)
            {
                AudioManager.Instance.PlayEnemyDefeat(transform.position);
            }

            if (defeatEffectPrefab != null)
            {
                Instantiate(defeatEffectPrefab, transform.position + Vector3.up * 0.8f, Quaternion.identity);
            }

            // Disable components
            if (TryGetComponent<CharacterController>(out var cc)) cc.enabled = false;
            if (TryGetComponent<Collider>(out var col)) col.enabled = false;
            if (TryGetComponent<NavMeshAgent>(out var agent)) agent.enabled = false;
            if (TryGetComponent<EnemyAI>(out var ai)) ai.enabled = false;

            if (respawnOnDeath)
            {
                // Hide renderers while dead
                for (int i = 0; i < renderers.Count; i++)
                {
                    if (renderers[i] != null) renderers[i].enabled = false;
                }
                StartCoroutine(RespawnRoutine());
            }
            else
            {
                Destroy(gameObject, destroyDelay);
            }
        }

        private IEnumerator RespawnRoutine()
        {
            // Allow death animation to play out before hiding
            yield return new WaitForSeconds(2.0f);

            // Hide renderers while dead
            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] != null) renderers[i].enabled = false;
            }

            float remainingWait = Mathf.Max(0.1f, respawnDelay - 2.0f);
            yield return new WaitForSeconds(remainingWait);

            transform.position = spawnPosition;
            transform.rotation = spawnRotation;
            currentHealth = maxHealth;
            isDead = false;
            RestoreColor();

            // Reset Animator back to Idle on respawn
            var anim = GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.Rebind();
                anim.Play("Idle", 0, 0f);
            }

            // Un-hide renderers
            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] != null) renderers[i].enabled = true;
            }

            if (TryGetComponent<CharacterController>(out var cc2)) cc2.enabled = true;
            if (TryGetComponent<Collider>(out var col)) col.enabled = true;
            if (TryGetComponent<NavMeshAgent>(out var agent) && agent.isOnNavMesh) agent.enabled = true;
            if (TryGetComponent<EnemyAI>(out var ai))
            {
                ai.enabled = true;
                ai.ResetToSpawn();
            }

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void DropCoins()
        {
            int count = UnityEngine.Random.Range(minCoinsToDrop, maxCoinsToDrop + 1);
            if (count <= 0) return;

            GameObject prefabToSpawn = coinDropPrefab;
#if UNITY_EDITOR
            if (prefabToSpawn == null)
            {
                prefabToSpawn = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThirdParty/LowPolyMedievalPropsLite/Prefabs/Coin_01.prefab");
            }
#endif

            Vector3 spawnBase = transform.position + Vector3.up * 0.8f;
            for (int i = 0; i < count; i++)
            {
                GameObject coinObj = null;
                if (prefabToSpawn != null)
                {
                    coinObj = Instantiate(prefabToSpawn, spawnBase, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f));
                }
                else
                {
                    coinObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    coinObj.name = "DroppedCoin";
                    coinObj.transform.position = spawnBase;
                    coinObj.transform.localScale = new Vector3(0.4f, 0.08f, 0.4f);
                }

                if (coinObj != null)
                {
                    coinObj.transform.localScale = Vector3.one * 1.5f;
                    var item = coinObj.GetComponent<RelicTrail.Progression.CollectibleItem>();
                    if (item == null) item = coinObj.AddComponent<RelicTrail.Progression.CollectibleItem>();

                    float angle = (i * (360f / Mathf.Max(1, count)) + UnityEngine.Random.Range(-25f, 25f)) * Mathf.Deg2Rad;
                    float hSpeed = UnityEngine.Random.Range(2.2f, 3.8f);
                    float vSpeed = UnityEngine.Random.Range(3.5f, 5.2f);
                    Vector3 launchVel = new Vector3(Mathf.Cos(angle) * hSpeed, vSpeed, Mathf.Sin(angle) * hSpeed);

                    item.Launch(launchVel);
                }
            }
        }
    }
}
