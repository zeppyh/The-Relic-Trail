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
        [SerializeField] private AudioClip activationSound;
        [SerializeField] private AudioClip ambientHumSound;
        [SerializeField] private Transform vortexTransform;
        [SerializeField] private ParticleSystem portalParticles;

        [Header("Animation Settings")]
        [SerializeField] private float spinSpeed = 25f;
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float pulseAmount = 0.04f;

        private Vector3 baseVortexScale = Vector3.one;
        private float baseLightIntensity = 3f;
        private bool isTeleporting = false;

        public bool IsPortalActive => isPortalActive;
        public bool CanInteract => true;

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            UpdateVisualState(instant: true);
        }

        private void Update()
        {
            if (!isPortalActive) return;

            // Continuous vortex spin animation
            if (vortexTransform != null)
            {
                vortexTransform.Rotate(Vector3.forward, spinSpeed * Time.deltaTime, Space.Self);

                // Subtle breathing scale animation
                float scaleMod = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
                vortexTransform.localScale = baseVortexScale * scaleMod;
            }

            // Subtle pulsing light animation
            if (portalLight != null && portalLight.enabled)
            {
                portalLight.intensity = baseLightIntensity * (1f + Mathf.Sin(Time.time * (pulseSpeed * 1.3f)) * 0.15f);
            }
        }

        private void InitializeComponents()
        {
            // Auto-resolve vortex transform if not explicitly wired
            if (vortexTransform == null && activeVfx != null)
            {
                vortexTransform = activeVfx.transform;
            }

            if (vortexTransform != null)
            {
                baseVortexScale = vortexTransform.localScale;
            }

            if (portalLight != null)
            {
                baseLightIntensity = portalLight.intensity > 0 ? portalLight.intensity : 3f;
            }

            // Setup or ensure spatial AudioSource
            if (portalAudioSource == null)
            {
                portalAudioSource = GetComponent<AudioSource>();
                if (portalAudioSource == null)
                {
                    portalAudioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            if (portalAudioSource != null)
            {
                portalAudioSource.playOnAwake = false;
                portalAudioSource.loop = true;
                portalAudioSource.spatialBlend = 1.0f; // Full 3D spatial audio
                portalAudioSource.minDistance = 2.5f;
                portalAudioSource.maxDistance = 25.0f;
                portalAudioSource.rolloffMode = AudioRolloffMode.Linear;
                portalAudioSource.volume = 0.7f;

                if (ambientHumSound != null && portalAudioSource.clip == null)
                {
                    portalAudioSource.clip = ambientHumSound;
                }
            }

            if (portalParticles == null)
            {
                portalParticles = GetComponentInChildren<ParticleSystem>();
            }
        }

        public void ActivatePortal()
        {
            if (isPortalActive) return;

            isPortalActive = true;
            StartCoroutine(ActivationSequenceRoutine());
        }

        private IEnumerator ActivationSequenceRoutine()
        {
            // Play activation sound surge
            if (activationSound != null)
            {
                AudioSource.PlayClipAtPoint(activationSound, transform.position, 1.0f);
            }

            if (activeVfx != null)
            {
                activeVfx.SetActive(true);
            }

            if (portalParticles != null && !portalParticles.isPlaying)
            {
                portalParticles.Play();
            }

            // Animate smooth vortex expansion from zero to full scale
            if (vortexTransform != null)
            {
                float duration = 1.2f;
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    // Smooth elastic / overshoot pop-in curve
                    float curvedT = Mathf.Sin(t * Mathf.PI * 0.5f);
                    vortexTransform.localScale = baseVortexScale * curvedT;

                    if (portalLight != null)
                    {
                        portalLight.enabled = true;
                        // Light flare during opening
                        portalLight.intensity = Mathf.Lerp(0f, baseLightIntensity * 1.8f, curvedT);
                    }

                    yield return null;
                }
            }

            UpdateVisualState(instant: false);

            if (portalAudioSource != null && portalAudioSource.clip != null && !portalAudioSource.isPlaying)
            {
                portalAudioSource.Play();
            }
        }

        private void UpdateVisualState(bool instant = false)
        {
            if (activeVfx != null)
            {
                activeVfx.SetActive(isPortalActive);
            }

            if (vortexTransform != null)
            {
                vortexTransform.localScale = isPortalActive ? baseVortexScale : Vector3.zero;
            }

            if (portalLight != null)
            {
                portalLight.enabled = isPortalActive;
                portalLight.intensity = baseLightIntensity;
            }

            if (portalParticles != null)
            {
                if (isPortalActive && !portalParticles.isPlaying) portalParticles.Play();
                else if (!isPortalActive && portalParticles.isPlaying) portalParticles.Stop();
            }

            if (portalAudioSource != null)
            {
                if (isPortalActive && portalAudioSource.clip != null && !portalAudioSource.isPlaying)
                {
                    portalAudioSource.Play();
                }
                else if (!isPortalActive && portalAudioSource.isPlaying)
                {
                    portalAudioSource.Stop();
                }
            }
        }

        public string GetInteractionPrompt()
        {
            if (!isPortalActive)
            {
                return "The portal is dormant. Secure the Relic Fragment to open it.";
            }
            return $"Press [E] to Enter Portal \u2192 {destinationName}";
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isPortalActive || isTeleporting) return;

            // Direct walk-in triggering
            if (other.CompareTag("Player") || other.GetComponent<CharacterController>() != null || other.GetComponent<Player.PlayerMovement>() != null)
            {
                Interact(other.gameObject);
            }
        }

        public void Interact(GameObject interactor)
        {
            if (!isPortalActive || isTeleporting) return;

            if (destinationSpawnPoint == null)
            {
                Debug.LogWarning($"[TeleportPortal] {gameObject.name} has no destination spawn point configured!");
                return;
            }

            StartCoroutine(TeleportRoutine(interactor));
        }

        private IEnumerator TeleportRoutine(GameObject player)
        {
            isTeleporting = true;

            // Play teleport sound at departure
            if (teleportSound != null)
            {
                AudioSource.PlayClipAtPoint(teleportSound, transform.position, 1.0f);
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

            // Play arrival teleport whoosh sound at arrival
            if (teleportSound != null)
            {
                AudioSource.PlayClipAtPoint(teleportSound, destinationSpawnPoint.position, 1.0f);
            }

            yield return new WaitForSeconds(0.3f);
            isTeleporting = false;
        }
    }
}
