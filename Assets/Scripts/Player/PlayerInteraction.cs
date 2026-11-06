using System;
using UnityEngine;
using RelicTrail.Interactions;

namespace RelicTrail.Player
{
    /// <summary>
    /// Detects nearby IInteractable objects in front of the player and handles interaction when [E] is pressed.
    /// </summary>
    [RequireComponent(typeof(PlayerInputHandler))]
    [AddComponentMenu("Relic Trail/Player/Player Interaction")]
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Detection Settings")]
        [SerializeField] private float interactionRadius = 3.5f;
        [SerializeField] private Vector3 interactionOffset = new Vector3(0f, 1.0f, 0.5f);
        [SerializeField] private LayerMask interactableLayers = ~0;

        // Events for UI
        public event Action<string> OnPromptChanged;
        public event Action OnPromptCleared;

        private PlayerInputHandler input;
        private IInteractable currentInteractable;

        public IInteractable CurrentInteractable => currentInteractable;

        private void Awake()
        {
            input = GetComponent<PlayerInputHandler>();
        }

        private void Update()
        {
            if (RelicTrail.Core.GameManager.Instance != null && RelicTrail.Core.GameManager.Instance.CurrentGameState != RelicTrail.Core.GameState.Playing)
            {
                return;
            }

            DetectInteractable();
            HandleInteractionInput();
        }

        private bool IsValidInteractable(IInteractable interactable)
        {
            if (interactable == null) return false;
            if (interactable is UnityEngine.Object unityObj && unityObj == null) return false;
            if (interactable is MonoBehaviour mb && (!mb.enabled || !mb.gameObject.activeInHierarchy)) return false;
            return interactable.CanInteract;
        }

        private void DetectInteractable()
        {
            Vector3 center = transform.TransformPoint(interactionOffset);
            Collider[] colliders = Physics.OverlapSphere(center, interactionRadius, interactableLayers, QueryTriggerInteraction.Collide);

            IInteractable bestCandidate = null;
            float closestDist = float.MaxValue;

            foreach (var col in colliders)
            {
                if (col == null || col.gameObject == gameObject) continue;

                var candidate = col.GetComponent<IInteractable>() ?? 
                                col.GetComponentInParent<IInteractable>() ?? 
                                col.GetComponentInChildren<IInteractable>();

                if (IsValidInteractable(candidate))
                {
                    float dist = Vector3.Distance(center, col.ClosestPoint(center));
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        bestCandidate = candidate;
                    }
                }
            }

            // If current interactable became destroyed or invalidated, clear prompt immediately
            if (!IsValidInteractable(currentInteractable) && currentInteractable != null)
            {
                currentInteractable = null;
                OnPromptCleared?.Invoke();
            }

            if (bestCandidate != currentInteractable)
            {
                currentInteractable = bestCandidate;
                if (currentInteractable != null)
                {
                    OnPromptChanged?.Invoke(currentInteractable.GetInteractionPrompt());
                }
                else
                {
                    OnPromptCleared?.Invoke();
                }
            }
        }

        private void HandleInteractionInput()
        {
            if (input.InteractTriggered)
            {
                if (IsValidInteractable(currentInteractable))
                {
                    currentInteractable.Interact(gameObject);
                }
                input.ConsumeInteract();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.TransformPoint(interactionOffset), interactionRadius);
        }
    }
}
