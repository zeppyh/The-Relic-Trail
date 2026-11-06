using UnityEngine;
using RelicTrail.Core;
using RelicTrail.Interactions;

namespace RelicTrail.Progression
{
    /// <summary>
    /// Interactive Relic Fragment. When collected by the player, records relic progress
    /// in GameManager and activates the biome's exit teleportation portal.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [AddComponentMenu("Relic Trail/Progression/Relic Fragment")]
    public class RelicFragment : MonoBehaviour, IInteractable
    {
        [Header("Relic Information")]
        [SerializeField] private BiomeType biome = BiomeType.ForgottenForest;
        [SerializeField] private string relicName = "Forest Relic Fragment";

        [Header("Linked Exit Portal")]
        [Tooltip("The portal that activates once this relic fragment is secured.")]
        [SerializeField] private TeleportPortal linkedPortal;

        [Header("Visuals & Audio")]
        [SerializeField] private GameObject collectEffectPrefab;
        [SerializeField] private AudioClip collectSound;
        [SerializeField] private float rotationSpeed = 55f;
        [SerializeField] private float bobFrequency = 2.0f;
        [SerializeField] private float bobAmplitude = 0.22f;

        private Vector3 startPosition;
        private bool isCollected;
        private Light relicGlowLight;

        public bool CanInteract => !isCollected;

        private void Awake()
        {
            SetupCollider();
        }

        private void OnEnable()
        {
            startPosition = transform.position;
            SetupCollider();
            SetupGlowLight();
        }

        private void Start()
        {
            startPosition = transform.position;
            SetupCollider();
            SetupGlowLight();
        }

        private void SetupCollider()
        {
            var col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
                if (col is SphereCollider sc)
                {
                    sc.radius = Mathf.Max(sc.radius, 1.5f);
                }
                else if (col is BoxCollider bc)
                {
                    bc.size = Vector3.Max(bc.size, Vector3.one * 1.5f);
                }
            }
            else
            {
                var sc = gameObject.AddComponent<SphereCollider>();
                sc.radius = 1.5f;
                sc.isTrigger = true;
            }
        }

        private void SetupGlowLight()
        {
            if (relicGlowLight == null)
            {
                relicGlowLight = GetComponentInChildren<Light>();
                if (relicGlowLight == null)
                {
                    var lightObj = new GameObject("RelicGlowLight");
                    lightObj.transform.SetParent(transform);
                    lightObj.transform.localPosition = Vector3.zero;
                    relicGlowLight = lightObj.AddComponent<Light>();
                }
            }

            if (relicGlowLight != null)
            {
                relicGlowLight.range = 8.0f;
                relicGlowLight.intensity = 4.0f;
                switch (biome)
                {
                    case BiomeType.ForgottenForest:
                        relicGlowLight.color = new Color(0.2f, 1f, 0.4f); // Emerald
                        break;
                    case BiomeType.ScorchedCanyon:
                        relicGlowLight.color = new Color(1f, 0.6f, 0.1f); // Amber / Flame
                        break;
                    case BiomeType.FrozenRuins:
                        relicGlowLight.color = new Color(0.35f, 0.9f, 1f); // Frost Cyan
                        break;
                    default:
                        relicGlowLight.color = Color.yellow;
                        break;
                }
                relicGlowLight.enabled = true;
            }
        }

        private void Update()
        {
            if (isCollected) return;

            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            float newY = startPosition.y + Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        public string GetInteractionPrompt()
        {
            return $"Press [E] to Claim {relicName}";
        }

        public void Interact(GameObject interactor)
        {
            if (isCollected) return;
            isCollected = true;

            // Notify GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.CollectRelic(biome);
            }

            // Activate linked portal
            if (linkedPortal != null)
            {
                linkedPortal.ActivatePortal();
            }

            // Feedback
            if (collectEffectPrefab != null)
            {
                Instantiate(collectEffectPrefab, transform.position, Quaternion.identity);
            }

            if (collectSound != null)
            {
                AudioSource.PlayClipAtPoint(collectSound, transform.position, 1.0f);
            }

            Destroy(gameObject, 0.1f);
        }
    }
}
