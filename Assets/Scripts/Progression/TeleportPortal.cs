using System.Collections;
using UnityEngine;
using RelicTrail.Core;
using RelicTrail.Interactions;

namespace RelicTrail.Progression
{
    /// <summary>
    /// Teleportation portal connecting biomes. Activates once the biome's puzzle and relic are completed.
    /// Safely repositions the player to the destination arrival point without collision glitches.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [AddComponentMenu("Relic Trail/Progression/Teleport Portal")]
    public class TeleportPortal : MonoBehaviour, IInteractable
    {
        [Header("Portal Destination")]
        [SerializeField] private Transform destinationSpawnPoint;
        [SerializeField] private BiomeType destinationBiome = BiomeType.ScorchedCanyon;
        [SerializeField] private string destinationName = "The Scorched Canyon";

        [Header("Portal State")]
        [SerializeField] private bool isPortalActive = false;

        [Header("Visual & Audio Elements")]
        [SerializeField] private GameObject activeVfx;
        [SerializeField] private Light portalLight;
        [SerializeField] private AudioSource portalAudioSource;
        [SerializeField] private AudioClip teleportSound;

        public bool IsPortalActive => isPortalActive;
        public bool CanInteract => true;

        private void Start()
        {
            UpdateVisualState();
        }

        public void ActivatePortal()
        {
            isPortalActive = true;
            UpdateVisualState();

            if (portalAudioSource != null && !portalAudioSource.isPlaying)
            {
                portalAudioSource.Play();
            }
        }

        private void UpdateVisualState()
        {
            if (activeVfx != null)
            {
                activeVfx.SetActive(isPortalActive);
            }

            if (portalLight != null)
            {
                portalLight.enabled = isPortalActive;
            }
        }

        public string GetInteractionPrompt()
        {
            if (!isPortalActive)
            {
                return "The portal is dormant. Secure the Relic Fragment to open it.";
            }
            return $"Press [E] to Enter Portal $\\rightarrow$ {destinationName}";
        }

        public void Interact(GameObject interactor)
        {
            if (!isPortalActive) return;

            if (destinationSpawnPoint == null)
            {
                Debug.LogWarning($"[TeleportPortal] {gameObject.name} has no destination spawn point configured!");
                return;
            }

            StartCoroutine(TeleportRoutine(interactor));
        }

        private IEnumerator TeleportRoutine(GameObject player)
        {
            // Play teleport sound
            if (teleportSound != null)
            {
                AudioSource.PlayClipAtPoint(teleportSound, transform.position, 0.9f);
            }

            // Disable CharacterController temporarily during transform update to avoid physics snapping
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            yield return new WaitForSeconds(0.1f);

            // Teleport player flush to destination spawn point
            player.transform.position = destinationSpawnPoint.position;
            player.transform.rotation = destinationSpawnPoint.rotation;

            yield return new WaitForSeconds(0.05f);

            if (cc != null) cc.enabled = true;

            var pm = player.GetComponent<Player.PlayerMovement>();
            if (pm != null) pm.SnapToGround();

            // Notify GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetBiome(destinationBiome);
            }

            if (teleportSound != null)
            {
                AudioSource.PlayClipAtPoint(teleportSound, destinationSpawnPoint.position, 0.9f);
            }
        }
    }
}
