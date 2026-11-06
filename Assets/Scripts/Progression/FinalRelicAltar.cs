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

        private void Awake()
        {
            SetupAltarGemVisuals();
        }

        private void Start()
        {
            SetupAltarGemVisuals();
            if (completeLostRelic != null) completeLostRelic.SetActive(false);
            if (forestRelicSocket != null) forestRelicSocket.SetActive(false);
            if (canyonRelicSocket != null) canyonRelicSocket.SetActive(false);
            if (frozenRelicSocket != null) frozenRelicSocket.SetActive(false);
            if (altarDivineLight != null) altarDivineLight.enabled = false;
        }

        public void SetupAltarGemVisuals()
        {
            SetupSocketVisual(forestRelicSocket, "Assets/ThirdParty/BTM_Assets/BTM_Items_Gems/Prefabs/HeartGem.prefab", "Assets/ThirdParty/BTM_Assets/BTM_Items_Gems/Materials/BaseMaterial_Green.mat", 0.45f);
            SetupSocketVisual(canyonRelicSocket, "Assets/ThirdParty/BTM_Assets/BTM_Items_Gems/Prefabs/SphereGem.prefab", "Assets/ThirdParty/BTM_Assets/BTM_Items_Gems/Materials/BaseMaterial_Orange.mat", 0.45f);
            SetupSocketVisual(frozenRelicSocket, "Assets/ThirdParty/BTM_Assets/BTM_Items_Gems/Prefabs/StarGem1.prefab", "Assets/ThirdParty/BTM_Assets/BTM_Items_Gems/Materials/BaseMaterial_Teal.mat", 0.45f);
            SetupSocketVisual(completeLostRelic, "Assets/ThirdParty/BTM_Assets/BTM_Items_Gems/Prefabs/DoubleDiamond.prefab", "Assets/ThirdParty/BTM_Assets/BTM_Items_Gems/Materials/BaseMaterial_Yellow.mat", 0.9f);
        }

        private void SetupSocketVisual(GameObject targetObj, string prefabPath, string matPath, float scale)
        {
            if (targetObj == null) return;

            // Hide placeholder primitive sphere renderer
            var rend = targetObj.GetComponent<MeshRenderer>();
            if (rend != null) rend.enabled = false;

            if (targetObj.transform.Find("Gem_Model") != null) return;

#if UNITY_EDITOR
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThirdParty/BTM_Assets/BTM_Items_Gems/Prefabs/HeartGem.prefab");
            }
            if (prefab != null)
            {
                GameObject gem = Instantiate(prefab, targetObj.transform);
                gem.name = "Gem_Model";
                gem.transform.localPosition = Vector3.zero;
                gem.transform.localRotation = Quaternion.identity;
                gem.transform.localScale = Vector3.one * scale;

                foreach (var col in gem.GetComponentsInChildren<Collider>())
                {
                    if (Application.isPlaying) Destroy(col);
                    else DestroyImmediate(col);
                }

                foreach (var anim in gem.GetComponentsInChildren<MonoBehaviour>())
                {
                    if (anim != null && anim.GetType().Name == "SimpleGemsAnim")
                    {
                        anim.enabled = false;
                    }
                }

                if (!string.IsNullOrEmpty(matPath))
                {
                    Material mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (mat != null)
                    {
                        foreach (var r in gem.GetComponentsInChildren<Renderer>())
                        {
                            r.sharedMaterial = mat;
                        }
                    }
                }
            }
#endif
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
            else if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPuzzleChime(transform.position);
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
            else if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayRelicPickup(transform.position);
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
