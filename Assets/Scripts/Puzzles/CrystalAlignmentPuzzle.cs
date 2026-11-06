using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RelicTrail.Interactions;

namespace RelicTrail.Puzzles
{
    /// <summary>
    /// Biome 3 Puzzle: Celestial Crystal Focusing.
    /// Player interacts with each of the 3 celestial crystals around the altar.
    /// Interacting with a crystal focuses its energy beam directly at the central altar.
    /// When all 3 beams converge, the ice barrier shatters to reveal the Frozen Relic Fragment.
    /// </summary>
    [AddComponentMenu("Relic Trail/Puzzles/Crystal Alignment Puzzle")]
    public class CrystalAlignmentPuzzle : MonoBehaviour
    {
        [System.Serializable]
        public class RotatableCrystal : IInteractable
        {
            public string crystalName = "North Crystal";
            public GameObject crystalObject;
            public LineRenderer beamRenderer;
            public Light crystalLight;
            public Transform centralReceiver;
            [Tooltip("Target rotation step (0 = 0 deg, 1 = 90 deg, 2 = 180 deg, 3 = 270 deg)")]
            public int targetStep = 2;

            [HideInInspector] public int currentStep;
            [HideInInspector] public CrystalAlignmentPuzzle puzzle;
            [HideInInspector] public bool isRotating;
            [HideInInspector] public bool isAligned;

            public bool IsAligned => isAligned;
            public bool CanInteract => !puzzle.IsSolved && !isRotating && !isAligned;

            public string GetInteractionPrompt()
            {
                if (isAligned)
                {
                    return $"{crystalName} (Focused to Altar)";
                }
                return $"Press [E] to Focus {crystalName} to Altar";
            }

            public void Interact(GameObject interactor)
            {
                if (CanInteract)
                {
                    puzzle.FocusCrystal(this);
                }
            }

            public void UpdateBeamVisuals()
            {
                if (crystalLight != null)
                {
                    crystalLight.color = isAligned ? new Color(0.4f, 0.95f, 1f) : new Color(0.2f, 0.4f, 0.8f);
                    crystalLight.intensity = isAligned ? 4.0f : 1.2f;
                }

                if (beamRenderer != null && centralReceiver != null)
                {
                    beamRenderer.enabled = isAligned;
                    if (isAligned && crystalObject != null)
                    {
                        beamRenderer.SetPosition(0, crystalObject.transform.position + Vector3.up * 1f);
                        beamRenderer.SetPosition(1, centralReceiver.position + Vector3.up * 1f);
                    }
                }
            }
        }

        [Header("Crystals")]
        [SerializeField] private List<RotatableCrystal> crystals = new List<RotatableCrystal>();

        [Header("Central Altar & Reward")]
        [SerializeField] private Transform centralAltar;
        [SerializeField] private GameObject iceBarrier;
        [SerializeField] private GameObject relicFragmentObject;

        [Header("Audio & Effects")]
        [SerializeField] private AudioClip rotateSound;
        [SerializeField] private AudioClip puzzleSolvedSound;
        [SerializeField] private GameObject shatterVfx;

        private bool isSolved;

        public bool IsSolved => isSolved;

        private void Awake()
        {
            foreach (var c in crystals)
            {
                c.puzzle = this;
                c.centralReceiver = centralAltar;
                c.isAligned = false;

                // Initial non-aligned visual
                if (c.crystalObject != null)
                {
                    if (c.crystalObject.GetComponent<CrystalProxy>() == null)
                    {
                        var proxy = c.crystalObject.AddComponent<CrystalProxy>();
                        proxy.crystal = c;
                    }

                    var col = c.crystalObject.GetComponent<Collider>();
                    if (col == null)
                    {
                        var sc = c.crystalObject.AddComponent<SphereCollider>();
                        sc.radius = 1.2f;
                        sc.isTrigger = true;
                    }
                }

                c.UpdateBeamVisuals();
            }

            if (relicFragmentObject != null)
            {
                relicFragmentObject.SetActive(false);
            }
        }

        public void FocusCrystal(RotatableCrystal crystal)
        {
            if (isSolved || crystal.isRotating || crystal.isAligned) return;
            StartCoroutine(FocusCrystalRoutine(crystal));
        }

        private IEnumerator FocusCrystalRoutine(RotatableCrystal crystal)
        {
            crystal.isRotating = true;

            if (rotateSound != null && crystal.crystalObject != null)
            {
                AudioSource.PlayClipAtPoint(rotateSound, crystal.crystalObject.transform.position, 0.9f);
            }

            if (crystal.crystalObject != null && centralAltar != null)
            {
                Quaternion startRot = crystal.crystalObject.transform.rotation;
                Vector3 toAltar = (centralAltar.position - crystal.crystalObject.transform.position);
                toAltar.y = 0f;
                Quaternion targetRot = toAltar.sqrMagnitude > 0.001f ? Quaternion.LookRotation(toAltar) : startRot;

                float elapsed = 0f;
                float duration = 0.5f;

                while (elapsed < duration)
                {
                    crystal.crystalObject.transform.rotation = Quaternion.Slerp(startRot, targetRot, elapsed / duration);
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                crystal.crystalObject.transform.rotation = targetRot;
            }

            crystal.isAligned = true;
            crystal.isRotating = false;
            crystal.UpdateBeamVisuals();

            CheckAllAligned();
        }

        private void CheckAllAligned()
        {
            foreach (var c in crystals)
            {
                if (!c.IsAligned) return;
            }

            SolvePuzzle();
        }

        private void SolvePuzzle()
        {
            if (isSolved) return;
            isSolved = true;

            Vector3 altarPos = centralAltar != null ? centralAltar.position : transform.position;

            if (puzzleSolvedSound != null)
            {
                AudioSource.PlayClipAtPoint(puzzleSolvedSound, altarPos, 1.0f);
            }

            if (shatterVfx != null && iceBarrier != null)
            {
                Instantiate(shatterVfx, iceBarrier.transform.position, Quaternion.identity);
            }

            if (iceBarrier != null)
            {
                iceBarrier.SetActive(false);
            }

            if (relicFragmentObject != null)
            {
                relicFragmentObject.SetActive(true);
            }
        }
    }

    /// <summary>
    /// Proxy component attached to crystal GameObjects to expose IInteractable.
    /// </summary>
    public class CrystalProxy : MonoBehaviour, IInteractable
    {
        public CrystalAlignmentPuzzle.RotatableCrystal crystal;

        public bool CanInteract => crystal != null && crystal.CanInteract;
        public string GetInteractionPrompt() => crystal != null ? crystal.GetInteractionPrompt() : "Focus Crystal";
        public void Interact(GameObject interactor) => crystal?.Interact(interactor);
    }
}
