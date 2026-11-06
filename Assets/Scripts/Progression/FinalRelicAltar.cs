using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RelicTrail.Core;
using RelicTrail.Interactions;

namespace RelicTrail.Progression
{
    /// <summary>
    /// Stage 5: The Final Sanctuary Altar.
    /// Requires all three relic fragments to initiate the relic restoration sequence and achieve victory.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [AddComponentMenu("Relic Trail/Progression/Final Relic Altar")]
    public class FinalRelicAltar : MonoBehaviour, IInteractable
    {
        [Header("Altar Sockets")]
        [SerializeField] private GameObject forestRelicSocket;
        [SerializeField] private GameObject canyonRelicSocket;
        [SerializeField] private GameObject frozenRelicSocket;

        [Header("Restored Lost Relic")]
        [SerializeField] private GameObject completeLostRelic;
        [SerializeField] private Transform floatingCenterPoint;

        [Header("Effects & Fanfare")]
        [SerializeField] private GameObject restorationVfx;
        [SerializeField] private Light altarDivineLight;
        [SerializeField] private AudioClip restorationSound;
        [SerializeField] private AudioClip victoryFanfareSound;

        private bool isRestored;

        public bool CanInteract => !isRestored;

        private void Start()
        {
            if (completeLostRelic != null) completeLostRelic.SetActive(false);
            if (forestRelicSocket != null) forestRelicSocket.SetActive(false);
            if (canyonRelicSocket != null) canyonRelicSocket.SetActive(false);
            if (frozenRelicSocket != null) frozenRelicSocket.SetActive(false);
            if (altarDivineLight != null) altarDivineLight.enabled = false;
        }

        public string GetInteractionPrompt()
        {
            if (GameManager.Instance != null && GameManager.Instance.HasAllRelics())
            {
                return "Press [E] to Restore the Lost Relic!";
            }
            int collected = GameManager.Instance != null ? GameManager.Instance.TotalRelicsCollected : 0;
            return $"The Altar is silent. 3 Relic Fragments required ({collected}/3 collected).";
        }

        public void Interact(GameObject interactor)
        {
            if (isRestored) return;

            if (GameManager.Instance != null && GameManager.Instance.HasAllRelics())
            {
                StartCoroutine(RestorationSequenceRoutine());
            }
        }

        private IEnumerator RestorationSequenceRoutine()
        {
            isRestored = true;

            // Activate sockets
            if (forestRelicSocket != null) forestRelicSocket.SetActive(true);
            if (canyonRelicSocket != null) canyonRelicSocket.SetActive(true);
            if (frozenRelicSocket != null) frozenRelicSocket.SetActive(true);

            if (restorationSound != null)
            {
                AudioSource.PlayClipAtPoint(restorationSound, transform.position, 1.0f);
            }

            yield return new WaitForSeconds(1.0f);

            // Turn on divine light
            if (altarDivineLight != null) altarDivineLight.enabled = true;

            if (restorationVfx != null && floatingCenterPoint != null)
            {
                Instantiate(restorationVfx, floatingCenterPoint.position, Quaternion.identity);
            }

            yield return new WaitForSeconds(1.5f);

            // Display completed unified relic
            if (completeLostRelic != null)
            {
                completeLostRelic.SetActive(true);
            }

            if (victoryFanfareSound != null)
            {
                AudioSource.PlayClipAtPoint(victoryFanfareSound, transform.position, 1.0f);
            }

            yield return new WaitForSeconds(2.0f);

            // Trigger Victory state
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerVictory();
            }
        }
    }
}
