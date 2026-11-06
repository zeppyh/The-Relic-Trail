using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RelicTrail.Combat;

namespace RelicTrail.Player
{
    /// <summary>
    /// Manages player health, damage taking with invulnerability frames, visual flashing, and death.
    /// </summary>
    [AddComponentMenu("Relic Trail/Player/Player Health")]
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;
        [SerializeField] private float invulnerabilityDuration = 1.0f;
        [SerializeField] private float flashInterval = 0.1f;

        [Header("Damage Visuals")]
        [SerializeField] private GameObject hurtEffectPrefab;

        // State
        private bool isInvulnerable;
        private bool isDodgeInvulnerable;
        private bool isDead;
        private List<Renderer> renderers = new List<Renderer>();
        private MaterialPropertyBlock propBlock;

        // Events
        public event Action<float, float> OnHealthChanged;
        public event Action OnPlayerDied;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsDead => isDead;
        public bool IsInvulnerable => isInvulnerable || isDodgeInvulnerable;

        /// <summary>
        /// Enables or disables brief i-frames during dodge rolls (Elden Ring mechanics).
        /// </summary>
        public void SetDodgeInvulnerable(bool invulnerable)
        {
            isDodgeInvulnerable = invulnerable;
        }

        private void Awake()
        {
            currentHealth = maxHealth;
            CacheRenderers();
        }

        private void Start()
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void CacheRenderers()
        {
            renderers.Clear();
            if (propBlock == null) propBlock = new MaterialPropertyBlock();
            var found = GetComponentsInChildren<Renderer>(true);
            foreach (var r in found)
            {
                // Skip particle systems or trail renderers
                if (r is ParticleSystemRenderer || r is TrailRenderer) continue;
                renderers.Add(r);
            }
        }

        public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
        {
            if (isDead || isInvulnerable) return;

            currentHealth = Mathf.Max(0f, currentHealth - amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            // Spawn optional hurt VFX
            if (hurtEffectPrefab != null && hitPoint != Vector3.zero)
            {
                Instantiate(hurtEffectPrefab, hitPoint, Quaternion.LookRotation(hitDirection));
            }

            if (currentHealth <= 0f)
            {
                Die();
            }
            else
            {
                StartCoroutine(InvulnerabilityRoutine());
            }
        }

        public void Heal(float amount)
        {
            if (isDead) return;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void ResetHealth()
        {
            isDead = false;
            isInvulnerable = false;
            currentHealth = maxHealth;
            RestoreOriginalColors();
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;

            var anim = GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.SetTrigger("Die");
            }

            OnPlayerDied?.Invoke();
        }

        private IEnumerator InvulnerabilityRoutine()
        {
            isInvulnerable = true;
            float elapsed = 0f;
            bool isFlashRed = false;

            while (elapsed < invulnerabilityDuration)
            {
                isFlashRed = !isFlashRed;
                SetFlashColor(isFlashRed ? new Color(1f, 0.2f, 0.2f, 0.6f) : Color.clear);
                yield return new WaitForSeconds(flashInterval);
                elapsed += flashInterval;
            }

            RestoreOriginalColors();
            isInvulnerable = false;
        }

        private void SetFlashColor(Color flashColor)
        {
            if (propBlock == null) propBlock = new MaterialPropertyBlock();

            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] == null) continue;

                if (flashColor == Color.clear)
                {
                    renderers[i].SetPropertyBlock(null);
                }
                else
                {
                    renderers[i].GetPropertyBlock(propBlock);
                    propBlock.SetColor("_BaseColor", flashColor);
                    propBlock.SetColor("_Color", flashColor);
                    renderers[i].SetPropertyBlock(propBlock);
                }
            }
        }

        private void RestoreOriginalColors()
        {
            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].SetPropertyBlock(null);
                }
            }
        }
    }
}
