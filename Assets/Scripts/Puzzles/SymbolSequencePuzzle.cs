using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RelicTrail.Interactions;

namespace RelicTrail.Puzzles
{
    /// <summary>
    /// Biome 1 Puzzle: Ancient Forest Shrine.
    /// Simplified to a direct, satisfying interaction:
    /// Approaching the sacred shrine altar (or any of the rune pillars) and pressing [E]
    /// immediately awakens the ancient shrine, illuminating all runes and revealing
    /// the Forest Relic Fragment above the altar.
    /// </summary>
    [AddComponentMenu("Relic Trail/Puzzles/Symbol Sequence Puzzle")]
    public class SymbolSequencePuzzle : MonoBehaviour
    {
        [System.Serializable]
        public class SymbolRune : IInteractable
        {
            public string runeName = "Sun Rune";
            public GameObject runeObject;
            public Light runeLight;
            public Renderer runeRenderer;
            public Color activeColor = new Color(0.2f, 0.9f, 0.3f);
            public Color defaultColor = new Color(0.2f, 0.2f, 0.2f);

            [HideInInspector] public bool isActivated;
            [HideInInspector] public SymbolSequencePuzzle puzzle;
            [HideInInspector] public int runeIndex;

            public bool CanInteract => !isActivated && !puzzle.isSolved;

            public string GetInteractionPrompt()
            {
                if (isActivated)
                {
                    return $"{runeName} (Activated)";
                }
                return $"Press [E] to Awaken Shrine ({runeName})";
            }

            public void Interact(GameObject interactor)
            {
                if (CanInteract)
                {
                    puzzle.AwakenShrine();
                }
            }

            public void SetVisualActive(bool active)
            {
                isActivated = active;
                if (runeLight != null) runeLight.enabled = active;

                if (runeRenderer != null && runeRenderer.material != null)
                {
                    Color targetColor = active ? activeColor : defaultColor;
                    if (runeRenderer.material.HasProperty("_BaseColor"))
                        runeRenderer.material.SetColor("_BaseColor", targetColor);
                    else if (runeRenderer.material.HasProperty("_Color"))
                        runeRenderer.material.SetColor("_Color", targetColor);

                    if (runeRenderer.material.HasProperty("_EmissionColor"))
                    {
                        runeRenderer.material.SetColor("_EmissionColor", active ? activeColor * 2.5f : Color.black);
                        if (active) runeRenderer.material.EnableKeyword("_EMISSION");
                    }
                }
            }
        }

        [Header("Puzzle Runes")]
        [SerializeField] private List<SymbolRune> runes = new List<SymbolRune>();

        [Header("Shrine Altar")]
        [SerializeField] private GameObject shrineAltarObject;

        [Header("Reward & Compartment")]
        [SerializeField] private GameObject relicFragmentObject;
        [SerializeField] private GameObject shrineDoorOrLid;
        [SerializeField] private Vector3 openDoorOffset = new Vector3(0f, 2f, 0f);

        [Header("Audio & Effects")]
        [SerializeField] private AudioClip activateSound;
        [SerializeField] private AudioClip successSound;
        [SerializeField] private AudioClip failureSound;
        [SerializeField] private GameObject successVfx;

        private bool isSolved;

        public bool IsSolved => isSolved;

        private void Awake()
        {
            // Setup Rune Pillars
            for (int i = 0; i < runes.Count; i++)
            {
                runes[i].puzzle = this;
                runes[i].runeIndex = i;
                runes[i].SetVisualActive(false);

                if (runes[i].runeObject != null)
                {
                    var proxy = runes[i].runeObject.GetComponent<RuneProxy>();
                    if (proxy == null) proxy = runes[i].runeObject.AddComponent<RuneProxy>();
                    proxy.puzzle = this;
                    proxy.rune = runes[i];

                    // Set all existing colliders to trigger
                    var existingCols = runes[i].runeObject.GetComponents<Collider>();
                    foreach (var c in existingCols)
                    {
                        c.isTrigger = true;
                    }

                    // Add a generous sphere trigger for instant, smooth detection
                    var sc = runes[i].runeObject.GetComponent<SphereCollider>();
                    if (sc == null) sc = runes[i].runeObject.AddComponent<SphereCollider>();
                    sc.radius = 2.5f;
                    sc.isTrigger = true;
                }
            }

            // Setup Central Shrine Altar
            if (shrineAltarObject == null)
            {
                var altarTrans = transform.Find("Shrine_Altar");
                if (altarTrans != null) shrineAltarObject = altarTrans.gameObject;
            }

            if (shrineAltarObject != null)
            {
                var altarProxy = shrineAltarObject.GetComponent<ShrineAltarProxy>();
                if (altarProxy == null) altarProxy = shrineAltarObject.AddComponent<ShrineAltarProxy>();
                altarProxy.puzzle = this;

                var existingCols = shrineAltarObject.GetComponents<Collider>();
                foreach (var c in existingCols)
                {
                    c.isTrigger = true;
                }

                var sc = shrineAltarObject.GetComponent<SphereCollider>();
                if (sc == null) sc = shrineAltarObject.AddComponent<SphereCollider>();
                sc.radius = 3.5f;
                sc.isTrigger = true;

                // Add soft ambient beacon glow to altar
                var light = shrineAltarObject.GetComponentInChildren<Light>();
                if (light == null)
                {
                    var lightObj = new GameObject("AltarBeaconLight");
                    lightObj.transform.SetParent(shrineAltarObject.transform);
                    lightObj.transform.localPosition = Vector3.up * 1.5f;
                    light = lightObj.AddComponent<Light>();
                    light.range = 7.0f;
                    light.intensity = 2.5f;
                    light.color = new Color(0.2f, 0.9f, 0.4f); // Sacred emerald
                }
            }

            if (relicFragmentObject != null)
            {
                relicFragmentObject.SetActive(false);
            }
        }

        /// <summary>
        /// Instantly awakens the sacred forest shrine, ignites all runes, and reveals the Forest Relic.
        /// </summary>
        public void AwakenShrine()
        {
            if (isSolved) return;

            // Ignite all runes with brilliant sacred light
            foreach (var rune in runes)
            {
                rune.SetVisualActive(true);
            }

            if (activateSound != null)
            {
                AudioSource.PlayClipAtPoint(activateSound, transform.position, 1.0f);
            }

            SolvePuzzle();
        }

        public void ActivateRune(int runeIndex)
        {
            AwakenShrine();
        }

        private void SolvePuzzle()
        {
            if (isSolved) return;
            isSolved = true;

            if (successSound != null)
            {
                AudioSource.PlayClipAtPoint(successSound, transform.position, 1.0f);
            }

            if (successVfx != null)
            {
                Instantiate(successVfx, transform.position + Vector3.up * 1f, Quaternion.identity);
            }

            // Open shrine door/lid
            if (shrineDoorOrLid != null)
            {
                StartCoroutine(OpenDoorRoutine());
            }

            // Reveal the Forest Relic Fragment
            if (relicFragmentObject != null)
            {
                relicFragmentObject.SetActive(true);
            }
        }

        private IEnumerator OpenDoorRoutine()
        {
            Vector3 startPos = shrineDoorOrLid.transform.position;
            Vector3 targetPos = startPos + openDoorOffset;
            float elapsed = 0f;
            float duration = 2.0f;

            while (elapsed < duration)
            {
                shrineDoorOrLid.transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            shrineDoorOrLid.transform.position = targetPos;
        }
    }

    /// <summary>
    /// Proxy component attached to the central Shrine Altar to allow direct awakening with [E].
    /// </summary>
    public class ShrineAltarProxy : MonoBehaviour, IInteractable
    {
        public SymbolSequencePuzzle puzzle;

        public bool CanInteract => puzzle != null && !puzzle.IsSolved;
        public string GetInteractionPrompt() => "Press [E] to Awaken the Forest Shrine";
        public void Interact(GameObject interactor) => puzzle?.AwakenShrine();
    }

    /// <summary>
    /// Helper component attached to rune GameObjects to expose IInteractable.
    /// </summary>
    public class RuneProxy : MonoBehaviour, IInteractable
    {
        public SymbolSequencePuzzle puzzle;
        public SymbolSequencePuzzle.SymbolRune rune;

        public bool CanInteract => (puzzle == null || !puzzle.IsSolved) && (rune == null || !rune.isActivated);
        public string GetInteractionPrompt() => rune != null ? $"Press [E] to Awaken Shrine ({rune.runeName})" : "Press [E] to Awaken the Forest Shrine";
        public void Interact(GameObject interactor)
        {
            if (puzzle != null)
            {
                puzzle.AwakenShrine();
            }
            else
            {
                rune?.Interact(interactor);
            }
        }
    }
}
