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

            public void SetDepressed(bool depressed)
            {
                isDepressed = depressed;
                if (plateLight != null) plateLight.enabled = depressed;

                Vector3 targetPos = depressed
                    ? originalPosition - Vector3.up * depressDepth
                    : originalPosition;

                if (plateObject != null)
                {
                    plateObject.transform.position = targetPos;
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
        [SerializeField] private Vector3 openDoorOffset = new Vector3(0f, 3.5f, 0f);
        [SerializeField] private GameObject relicFragmentObject;

        [Header("Audio & VFX")]
        [SerializeField] private AudioClip plateClickSound;
        [SerializeField] private AudioClip doorOpenSound;
        [SerializeField] private AudioClip resetSound;
        [SerializeField] private GameObject doorDustVfx;

        private List<int> currentSequence = new List<int>();
        private bool isSolved;
        private bool isResetting;

        public bool IsSolved => isSolved;

        private void Awake()
        {
            for (int i = 0; i < plates.Count; i++)
            {
                plates[i].puzzle = this;
                plates[i].plateIndex = i;
                if (plates[i].plateObject != null)
                {
                    plates[i].originalPosition = plates[i].plateObject.transform.position;

                    // Add trigger handler
                    if (plates[i].plateObject.GetComponent<PlateTriggerProxy>() == null)
                    {
                        var proxy = plates[i].plateObject.AddComponent<PlateTriggerProxy>();
                        proxy.plate = plates[i];
                    }

                    // Ensure collider exists
                    var col = plates[i].plateObject.GetComponent<Collider>();
                    if (col != null)
                    {
                        col.isTrigger = true;
                    }
                }
            }

            if (relicFragmentObject != null)
            {
                relicFragmentObject.SetActive(false);
            }
        }

        public void TriggerPlate(int plateIndex)
        {
            if (isSolved || isResetting) return;
            if (plates[plateIndex].isDepressed) return;

            plates[plateIndex].SetDepressed(true);
            currentSequence.Add(plateIndex);

            if (plateClickSound != null && plates[plateIndex].plateObject != null)
            {
                AudioSource.PlayClipAtPoint(plateClickSound, plates[plateIndex].plateObject.transform.position, 0.9f);
            }

            if (!requireExactSequence)
            {
                // Simple mode: solve when all plates are depressed
                int count = 0;
                for (int i = 0; i < plates.Count; i++)
                {
                    if (plates[i].isDepressed) count++;
                }

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

        private void SolvePuzzle()
        {
            if (isSolved) return;
            isSolved = true;

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
            Vector3 targetPos = startPos + openDoorOffset;
            float elapsed = 0f;
            float duration = 2.5f;

            while (elapsed < duration)
            {
                stoneDoor.transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
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
        public string GetInteractionPrompt() => plate != null && plate.isDepressed ? $"{plate.plateName} (Pressed)" : $"Press [E] to Step on {plate?.plateName}";
        public void Interact(GameObject interactor) => plate?.puzzle.TriggerPlate(plate.plateIndex);

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") || other.GetComponentInParent<Player.PlayerMovement>() != null)
            {
                plate?.puzzle.TriggerPlate(plate.plateIndex);
            }
        }
    }
}
