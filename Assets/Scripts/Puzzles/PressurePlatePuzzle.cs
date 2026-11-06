using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RelicTrail.Interactions;

namespace RelicTrail.Puzzles
{
    /// <summary>
    /// Biome 2 Puzzle: Ancient Pressure Plates.
    /// Stepping onto all 3 pressure plates (Left, Center, Right) activates the ancient mechanism,
    /// opening the sealed sandstone door to reveal the Canyon Relic Fragment.
    /// </summary>
    [AddComponentMenu("Relic Trail/Puzzles/Pressure Plate Puzzle")]
    public class PressurePlatePuzzle : MonoBehaviour
    {
        [System.Serializable]
        public class PressurePlate
        {
            public string plateName = "Left Plate";
            public GameObject plateObject;
            public float depressDepth = 0.08f;
            public Light plateLight;

            [HideInInspector] public Vector3 originalPosition;
            [HideInInspector] public bool isDepressed;
            [HideInInspector] public int plateIndex;
            [HideInInspector] public PressurePlatePuzzle puzzle;

            private Color originalColor = Color.white;
            private Material plateMaterial;

            public void InitializeVisuals()
            {
                if (plateObject != null)
                {
                    var renderer = plateObject.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        plateMaterial = renderer.material;
                        if (plateMaterial.HasProperty("_BaseColor")) originalColor = plateMaterial.GetColor("_BaseColor");
                        else if (plateMaterial.HasProperty("_Color")) originalColor = plateMaterial.GetColor("_Color");
                    }

                    if (plateLight == null)
                    {
                        var lightObj = new GameObject($"{plateName}_GlowLight");
                        lightObj.transform.SetParent(plateObject.transform, false);
                        lightObj.transform.localPosition = new Vector3(0f, 0.4f, 0f);
                        plateLight = lightObj.AddComponent<Light>();
                        plateLight.type = LightType.Point;
                        plateLight.range = 5f;
                        plateLight.intensity = 3f;
                        plateLight.color = new Color(0.2f, 1f, 0.5f);
                        plateLight.enabled = false;
                    }
                }
            }

            public void SetDepressed(bool depressed)
            {
                isDepressed = depressed;
                if (plateLight != null) plateLight.enabled = depressed;

                if (depressDepth <= 0.01f) depressDepth = 0.08f;

                Vector3 targetPos = depressed
                    ? originalPosition - Vector3.up * depressDepth
                    : originalPosition;

                if (plateObject != null)
                {
                    plateObject.transform.position = targetPos;

                    var renderer = plateObject.GetComponent<Renderer>();
                    if (renderer != null && renderer.material != null)
                    {
                        Color activeCol = new Color(0.2f, 1f, 0.6f);
                        Color col = depressed ? activeCol : originalColor;
                        if (renderer.material.HasProperty("_BaseColor")) renderer.material.SetColor("_BaseColor", col);
                        else if (renderer.material.HasProperty("_Color")) renderer.material.SetColor("_Color", col);

                        if (depressed)
                        {
                            renderer.material.EnableKeyword("_EMISSION");
                            if (renderer.material.HasProperty("_EmissionColor"))
                            {
                                renderer.material.SetColor("_EmissionColor", activeCol * 1.5f);
                            }
                        }
                        else
                        {
                            if (renderer.material.HasProperty("_EmissionColor"))
                            {
                                renderer.material.SetColor("_EmissionColor", Color.black);
                            }
                        }
                    }
                }
            }
        }

        [Header("Pressure Plates")]
        [SerializeField] private List<PressurePlate> plates = new List<PressurePlate>();

        [Header("Sequence Settings (Optional)")]
        [Tooltip("If false, depressing all plates in any order solves the puzzle (simple & intuitive).")]
        [SerializeField] private bool requireExactSequence = false;
        [Tooltip("Required order of plates if exact sequence is required (e.g. 0 = Left, 2 = Right, 1 = Center).")]
        [SerializeField] private int[] requiredSequence = new int[] { 0, 2, 1 };

        [Header("Chamber Door & Reward")]
        [SerializeField] private GameObject stoneDoor;
        [SerializeField] private Vector3 openDoorOffset = new Vector3(0f, 5.0f, 0f);
        [SerializeField] private GameObject relicFragmentObject;

        [Header("Audio & VFX")]
        [SerializeField] private AudioClip plateClickSound;
        [SerializeField] private AudioClip doorOpenSound;
        [SerializeField] private AudioClip resetSound;
        [SerializeField] private GameObject doorDustVfx;

        private List<int> currentSequence = new List<int>();
        private bool isSolved;
        private bool isResetting;
        private Transform playerTransform;

        public bool IsSolved => isSolved;

        private void Awake()
        {
            FindMissingReferences();

            for (int i = 0; i < plates.Count; i++)
            {
                plates[i].puzzle = this;
                plates[i].plateIndex = i;
                if (plates[i].plateObject != null)
                {
                    plates[i].originalPosition = plates[i].plateObject.transform.position;
                    plates[i].InitializeVisuals();

                    // Ensure kinematic Rigidbody exists so CharacterController trigger collisions work in Unity PhysX
                    var rb = plates[i].plateObject.GetComponent<Rigidbody>();
                    if (rb == null) rb = plates[i].plateObject.AddComponent<Rigidbody>();
                    rb.isKinematic = true;
                    rb.useGravity = false;

                    // Ensure trigger handler is present and correctly linked
                    var proxy = plates[i].plateObject.GetComponent<PlateTriggerProxy>();
                    if (proxy == null) proxy = plates[i].plateObject.AddComponent<PlateTriggerProxy>();
                    proxy.plate = plates[i];

                    // Ensure collider exists, is a trigger, and tall enough to catch player feet
                    var col = plates[i].plateObject.GetComponent<Collider>();
                    if (col == null) col = plates[i].plateObject.AddComponent<BoxCollider>();
                    col.isTrigger = true;
                }
            }

            if (relicFragmentObject != null)
            {
                relicFragmentObject.SetActive(false);
            }

            if (stoneDoor != null)
            {
                // Ensure stone door has kinematic Rigidbody for smooth physics movement
                var doorRb = stoneDoor.GetComponent<Rigidbody>();
                if (doorRb == null) doorRb = stoneDoor.AddComponent<Rigidbody>();
                doorRb.isKinematic = true;
                doorRb.useGravity = false;

                // Add interaction proxy on door so inspecting it explains the puzzle
                if (stoneDoor.GetComponent<SandstoneDoorProxy>() == null)
                {
                    var doorProxy = stoneDoor.AddComponent<SandstoneDoorProxy>();
                    doorProxy.puzzle = this;
                }
            }
        }

        private void FindMissingReferences()
        {
            if (stoneDoor == null)
            {
                var d = transform.Find("Sandstone_Door");
                if (d != null) stoneDoor = d.gameObject;
                else
                {
                    var found = GameObject.Find("Sandstone_Door");
                    if (found != null) stoneDoor = found;
                }
            }

            if (relicFragmentObject == null)
            {
                var r = transform.Find("Canyon_Relic_Fragment");
                if (r != null) relicFragmentObject = r.gameObject;
                else
                {
                    var found = GameObject.Find("Canyon_Relic_Fragment");
                    if (found != null) relicFragmentObject = found;
                }
            }
        }

        private void Update()
        {
            if (isSolved || isResetting) return;

            // Direct proximity detection: guaranteed to trigger even if physics triggers glitch
            if (playerTransform == null)
            {
                var p = GameObject.FindWithTag("Player");
                if (p != null) playerTransform = p.transform;
            }

            if (playerTransform != null)
            {
                Vector3 playerPos = playerTransform.position;
                for (int i = 0; i < plates.Count; i++)
                {
                    if (plates[i].isDepressed || plates[i].plateObject == null) continue;

                    Vector3 platePos = plates[i].plateObject.transform.position;
                    float dx = playerPos.x - platePos.x;
                    float dz = playerPos.z - platePos.z;
                    float dy = Mathf.Abs(playerPos.y - platePos.y);

                    // Radius of 1.4m covers the plate's physical footprint
                    if (dx * dx + dz * dz <= 1.4f * 1.4f && dy <= 2.0f)
                    {
                        TriggerPlate(i);
                    }
                }
            }
        }

        public void TriggerPlate(int plateIndex)
        {
            if (isSolved || isResetting) return;
            if (plateIndex < 0 || plateIndex >= plates.Count) return;
            if (plates[plateIndex].isDepressed) return;

            plates[plateIndex].SetDepressed(true);
            currentSequence.Add(plateIndex);

            // Audio feedback
            if (plateClickSound != null && plates[plateIndex].plateObject != null)
            {
                AudioSource.PlayClipAtPoint(plateClickSound, plates[plateIndex].plateObject.transform.position, 1.0f);
            }
            else if (RelicTrail.Core.AudioManager.Instance != null)
            {
                RelicTrail.Core.AudioManager.Instance.PlayButtonClick();
            }

            // Count activated plates
            int count = 0;
            for (int i = 0; i < plates.Count; i++)
            {
                if (plates[i].isDepressed) count++;
            }

            // Show progress prompt
            if (RelicTrail.UI.GameUIManager.Instance != null)
            {
                if (count < plates.Count)
                {
                    RelicTrail.UI.GameUIManager.Instance.ShowPrompt($"Pressure Plate Activated! ({count}/{plates.Count})");
                }
            }

            if (!requireExactSequence)
            {
                // Simple mode: solve when all plates are depressed
                if (count >= plates.Count)
                {
                    SolvePuzzle();
                }
            }
            else
            {
                if (currentSequence.Count == requiredSequence.Length)
                {
                    CheckSequence();
                }
            }
        }

        private void CheckSequence()
        {
            bool correct = true;
            for (int i = 0; i < requiredSequence.Length; i++)
            {
                if (currentSequence[i] != requiredSequence[i])
                {
                    correct = false;
                    break;
                }
            }

            if (correct)
            {
                SolvePuzzle();
            }
            else
            {
                StartCoroutine(ResetPlatesRoutine());
            }
        }

        public void SolvePuzzle()
        {
            if (isSolved) return;
            isSolved = true;

            Debug.Log("[PressurePlatePuzzle] All pressure plates activated! Opening Sandstone Door...");

            if (RelicTrail.UI.GameUIManager.Instance != null)
            {
                RelicTrail.UI.GameUIManager.Instance.ShowPrompt("All Plates Activated! The Sandstone Door is Opening!");
            }

            if (doorOpenSound != null)
            {
                AudioSource.PlayClipAtPoint(doorOpenSound, transform.position, 1.0f);
            }

            if (doorDustVfx != null && stoneDoor != null)
            {
                Instantiate(doorDustVfx, stoneDoor.transform.position, Quaternion.identity);
            }

            if (stoneDoor != null)
            {
                StartCoroutine(OpenDoorRoutine());
            }

            if (relicFragmentObject != null)
            {
                relicFragmentObject.SetActive(true);
            }
        }

        private IEnumerator OpenDoorRoutine()
        {
            Vector3 startPos = stoneDoor.transform.position;
            Vector3 offset = openDoorOffset.y < 4.0f ? new Vector3(0f, 5.0f, 0f) : openDoorOffset;
            Vector3 targetPos = startPos + offset;
            float elapsed = 0f;
            float duration = 2.5f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                t = Mathf.SmoothStep(0f, 1f, t);
                stoneDoor.transform.position = Vector3.Lerp(startPos, targetPos, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            stoneDoor.transform.position = targetPos;
        }

        private IEnumerator ResetPlatesRoutine()
        {
            isResetting = true;

            if (resetSound != null)
            {
                AudioSource.PlayClipAtPoint(resetSound, transform.position, 0.75f);
            }

            if (RelicTrail.UI.GameUIManager.Instance != null)
            {
                RelicTrail.UI.GameUIManager.Instance.ShowPrompt("Incorrect sequence! Pressure plates reset.");
            }

            yield return new WaitForSeconds(1.2f);

            foreach (var p in plates)
            {
                p.SetDepressed(false);
            }

            currentSequence.Clear();
            isResetting = false;
        }
    }

    /// <summary>
    /// Proxy component attached to plate triggers to detect player stepping on it or pressing E.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PlateTriggerProxy : MonoBehaviour, IInteractable
    {
        public PressurePlatePuzzle.PressurePlate plate;

        public bool CanInteract => plate != null && !plate.isDepressed && !plate.puzzle.IsSolved;
        public string GetInteractionPrompt() => plate != null && plate.isDepressed ? $"{plate.plateName} (Activated)" : $"Press [E] to Step on {plate?.plateName}";
        public void Interact(GameObject interactor) => plate?.puzzle.TriggerPlate(plate.plateIndex);

        private void OnTriggerEnter(Collider other)
        {
            HandleTrigger(other);
        }

        private void OnTriggerStay(Collider other)
        {
            HandleTrigger(other);
        }

        private void HandleTrigger(Collider other)
        {
            if (plate == null || plate.isDepressed || plate.puzzle == null || plate.puzzle.IsSolved) return;

            if (other.CompareTag("Player") || 
                other.GetComponentInParent<Player.PlayerMovement>() != null ||
                other.GetComponent<CharacterController>() != null)
            {
                plate.puzzle.TriggerPlate(plate.plateIndex);
            }
        }
    }

    /// <summary>
    /// Proxy component attached to Sandstone Door to provide interaction feedback.
    /// </summary>
    public class SandstoneDoorProxy : MonoBehaviour, IInteractable
    {
        public PressurePlatePuzzle puzzle;

        public bool CanInteract => puzzle != null && !puzzle.IsSolved;
        public string GetInteractionPrompt() => "The Ancient Sandstone Door is sealed. Step on all 3 pressure plates to open it.";
        public void Interact(GameObject interactor)
        {
            if (RelicTrail.UI.GameUIManager.Instance != null)
            {
                RelicTrail.UI.GameUIManager.Instance.ShowPrompt("The Ancient Sandstone Door is sealed. Step on all 3 pressure plates to open it.");
            }
        }
    }
}
