using System;
using UnityEngine;

namespace RelicTrail.Player
{
    /// <summary>
    /// Manages player stamina for dodging and heavy combat attacks, inspired by Elden Ring.
    /// Provides smooth regeneration after a delay and notifies the UI.
    /// </summary>
    [AddComponentMenu("Relic Trail/Player/Player Stamina")]
    public class PlayerStamina : MonoBehaviour
    {
        [Header("Stamina Settings")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float currentStamina = 100f;
        [SerializeField] private float staminaRegenRate = 28f;
        [SerializeField] private float regenDelay = 0.85f;

        [Header("Action Costs")]
        [SerializeField] private float dodgeStaminaCost = 22f;
        [SerializeField] private float lightAttackStaminaCost = 14f;
        [SerializeField] private float heavyAttackStaminaCost = 35f;

        private float lastStaminaUsedTime = -999f;

        public event Action<float, float> OnStaminaChanged;

        public float MaxStamina => maxStamina;
        public float CurrentStamina => currentStamina;
        public float DodgeCost => dodgeStaminaCost;
        public float LightAttackCost => lightAttackStaminaCost;
        public float HeavyAttackCost => heavyAttackStaminaCost;

        private void Awake()
        {
            currentStamina = maxStamina;
        }

        private void Start()
        {
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }

        private void Update()
        {
            if (currentStamina < maxStamina && Time.time >= lastStaminaUsedTime + regenDelay)
            {
                currentStamina = Mathf.Min(maxStamina, currentStamina + (staminaRegenRate * Time.deltaTime));
                OnStaminaChanged?.Invoke(currentStamina, maxStamina);
            }
        }

        /// <summary>
        /// Checks if player has at least the required stamina.
        /// </summary>
        public bool HasStamina(float cost)
        {
            return currentStamina >= cost;
        }

        /// <summary>
        /// Attempts to consume stamina for an action.
        /// </summary>
        public bool TryConsumeStamina(float cost)
        {
            if (currentStamina < cost) return false;

            currentStamina = Mathf.Max(0f, currentStamina - cost);
            lastStaminaUsedTime = Time.time;
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
            return true;
        }

        /// <summary>
        /// Resets stamina to maximum (e.g. on respawn or scene transition).
        /// </summary>
        public void ResetStamina()
        {
            currentStamina = maxStamina;
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }
    }
}
