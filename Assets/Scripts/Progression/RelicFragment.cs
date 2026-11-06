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

        [Header("3D Model Prefab")]
        [SerializeField] private GameObject gemPrefabOverride;

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
            SetupGemVisual();
        }

        private void OnEnable()
        {
            startPosition = transform.position;
            SetupCollider();
            SetupGlowLight();
            SetupGemVisual();
        }

        private void Start()
        {
            startPosition = transform.position;
            SetupCollider();
            SetupGlowLight();
            SetupGemVisual();
        }

        /// <summary>
        /// Replaces any primitive sphere placeholder with authentic 3D BTM_Assets gems.
        /// </summary>
        public void SetupGemVisual()
        {
            // Hide or disable any primitive sphere MeshRenderer on this object
            var selfRenderer = GetComponent<MeshRenderer>();
            if (selfRenderer != null)
            {
                selfRenderer.enabled = false;
            }

            // Check if gem model child already exists
            Transform existingGem = transform.Find("Gem_Model");
            if (existingGem != null)
            {
                existingGem.gameObject.SetActive(true);
                return;
            }

            GameObject prefabToUse = gemPrefabOverride;
#if UNITY_EDITOR
            if (prefabToUse == null)
            {
                string assetPath = GetGemPrefabPath(biome);
                prefabToUse = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            }
#endif

            if (prefabToUse != null)
            {
                GameObject gem = Instantiate(prefabToUse, transform);
                gem.name = "Gem_Model";
                gem.transform.localPosition = Vector3.zero;
                gem.transform.localRotation = Quaternion.identity;
                gem.transform.localScale = Vector3.one * 0.75f;

                // Strip colliders from child so root trigger manages interaction cleanly
                foreach (var col in gem.GetComponentsInChildren<Collider>())
                {
                    if (Application.isPlaying) Destroy(col);
                    else DestroyImmediate(col);
                }

                // Disable SimpleGemsAnim so RelicFragment's synchronized bob/spin is used
                foreach (var anim in gem.GetComponentsInChildren<MonoBehaviour>())
                {
                    if (anim != null && anim.GetType().Name == "SimpleGemsAnim")
                    {
                        anim.enabled = false;
                    }
                }

#if UNITY_EDITOR
                // Ensure biome themed URP material is assigned
                string matPath = GetGemMaterialPath(biome);
                if (!string.IsNullOrEmpty(matPath))
                {
                    Material mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (mat != null)
                    {
                        foreach (var rend in gem.GetComponentsInChildren<Renderer>())
                        {
                            rend.sharedMaterial = mat;
                        }
                    }
                }
#endif
            }
        }

        public static string GetGemPrefabPath(BiomeType biomeType)
        {
            switch (biomeType)
            {
                case BiomeType.ForgottenForest:
                    return "Assets/ThirdParty/BTM_Assets/BTM_Items_Gems/Prefabs/HeartGem.prefab";
                case BiomeType.ScorchedCanyon:
                    return "Assets/ThirdParty/BTM_Assets/BTM_Items_Gems/Prefabs/SphereGem.prefab";
                case BiomeType.FrozenRuins:
                    return "Assets/ThirdParty/BTM_Assets/BTM_Items_Gems/Prefabs/StarGem1.prefab";
                default:
                    return "Assets/ThirdParty/BTM_Assets/BTM_Items_Gems/Prefabs/HeartGem.prefab";
            }
        }

        public static string GetGemMaterialPath(BiomeType biomeType)
        {
            switch (biomeType)
            {
                case BiomeType.ForgottenForest:
                    return "Assets/ThirdParty/BTM_Assets/BTM_Items_Gems/Materials/BaseMaterial_Green.mat";
                case BiomeType.ScorchedCanyon:
                    return "Assets/ThirdParty/BTM_Assets/BTM_Items_Gems/Materials/BaseMaterial_Orange.mat";
                case BiomeType.FrozenRuins:
                    return "Assets/ThirdParty/BTM_Assets/BTM_Items_Gems/Materials/BaseMaterial_Teal.mat";
                default:
                    return "Assets/ThirdParty/BTM_Assets/BTM_Items_Gems/Materials/BaseMaterial_Red.mat";
            }
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
            else if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayRelicPickup(transform.position);
            }

            Destroy(gameObject, 0.1f);
        }
    }
}
