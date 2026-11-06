using UnityEngine;
using RelicTrail.Core;
using RelicTrail.Interactions;

namespace RelicTrail.Progression
{
    /// <summary>
    /// Collectible coin or magical token. Rotates, bobs, emits a golden glow,
    /// and prompts the player to press [E] to collect. Prevents duplicate collection.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [AddComponentMenu("Relic Trail/Progression/Collectible Item")]
    public class CollectibleItem : MonoBehaviour, IInteractable
    {
        [Header("Item Value & Settings")]
        [SerializeField] private int coinValue = 1;
        [SerializeField] private string itemName = "Coin";
        [SerializeField] private float rotationSpeed = 120f;
        [SerializeField] private float bobFrequency = 2.2f;
        [SerializeField] private float bobAmplitude = 0.15f;

        [Header("Effects & Audio")]
        [SerializeField] private GameObject pickupEffectPrefab;
        [SerializeField] private AudioClip pickupSound;

        private Vector3 startPosition;
        private bool isCollected;
        private Light coinLight;
        private bool isLaunched;
        private Rigidbody launchRb;
        private float launchTimer;

        public bool CanInteract => !isCollected;

        public string GetInteractionPrompt()
        {
            return $"Press [E] to Collect {itemName}";
        }

        public void Interact(GameObject interactor)
        {
            if (isCollected) return;
            Collect();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isCollected) return;
            if (IsPlayer(other.gameObject))
            {
                Collect();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (isCollected) return;
            if (IsPlayer(collision.gameObject))
            {
                Collect();
            }
        }

        private bool IsPlayer(GameObject obj)
        {
            if (obj == null) return false;
            if (obj.CompareTag("Player")) return true;
            if (obj.GetComponentInParent<RelicTrail.Player.PlayerMovement>() != null) return true;
            return false;
        }

        /// <summary>
        /// Launches the coin into the air with physics before it settles into its bobbing idle state.
        /// </summary>
        public void Launch(Vector3 velocity)
        {
            isLaunched = true;
            launchTimer = 1.0f;

            launchRb = GetComponent<Rigidbody>();
            if (launchRb == null) launchRb = gameObject.AddComponent<Rigidbody>();

            launchRb.isKinematic = false;
            launchRb.useGravity = true;
            launchRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            launchRb.linearVelocity = velocity;

            // Keep collider non-trigger briefly so it bounces on ground
            var col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = false;
            }
        }

        private void Awake()
        {
            SetupCollider();
        }

        private void Start()
        {
            startPosition = transform.position;
            SetupCollider();
            SetupCoinGlow();
        }

        private void SetupCollider()
        {
            var col = GetComponent<Collider>();
            if (col != null)
            {
                if (!isLaunched) col.isTrigger = true;
                if (col is SphereCollider sphere)
                {
                    sphere.radius = Mathf.Max(sphere.radius, 1.2f);
                }
                else if (col is BoxCollider box)
                {
                    box.size = Vector3.Max(box.size, Vector3.one * 1.2f);
                }
            }
            else
            {
                var sc = gameObject.AddComponent<SphereCollider>();
                sc.radius = 1.2f;
                sc.isTrigger = !isLaunched;
            }
        }

        private void SetupCoinGlow()
        {
            if (coinLight == null)
            {
                coinLight = GetComponentInChildren<Light>();
                if (coinLight == null)
                {
                    var lightObj = new GameObject("CoinGlowLight");
                    lightObj.transform.SetParent(transform);
                    lightObj.transform.localPosition = Vector3.up * 0.2f;
                    coinLight = lightObj.AddComponent<Light>();
                }
            }

            if (coinLight != null)
            {
                coinLight.range = 3.5f;
                coinLight.intensity = 2.0f;
                coinLight.color = new Color(1f, 0.85f, 0.25f); // Warm gold glow
                coinLight.enabled = true;
            }
        }

        private void Update()
        {
            if (isCollected) return;

            if (isLaunched)
            {
                launchTimer -= Time.deltaTime;
                // Once launch arc completes or coin comes to rest
                if (launchTimer <= 0f || (launchRb != null && launchRb.linearVelocity.sqrMagnitude < 0.08f && launchTimer < 0.6f))
                {
                    isLaunched = false;
                    if (launchRb != null)
                    {
                        Destroy(launchRb);
                        launchRb = null;
                    }
                    var col = GetComponent<Collider>();
                    if (col != null) col.isTrigger = true;
                    startPosition = transform.position;
                }
                return;
            }

            // Idle rotation
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

            // Floating bobbing
            float newY = startPosition.y + Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        public void Collect()
        {
            if (isCollected) return;
            isCollected = true;

            // Update GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddCoins(coinValue);
            }

            // Audio feedback
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCoinPickup(transform.position);
            }
            else if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position, 0.75f);
            }

            // Visual feedback
            if (pickupEffectPrefab != null)
            {
                Instantiate(pickupEffectPrefab, transform.position, Quaternion.identity);
            }

            Destroy(gameObject);
        }
    }
}
