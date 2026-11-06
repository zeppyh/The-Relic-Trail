using UnityEngine;
using RelicTrail.Interactions;

namespace RelicTrail.NPC
{
    /// <summary>
    /// Reusable NPC component. Faces the player when approached and delivers
    /// puzzle clues and guidance via the DialogueManager.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [AddComponentMenu("Relic Trail/NPC/NPC Dialogue")]
    public class NPCDialogue : MonoBehaviour, IInteractable
    {
        [Header("NPC Identity")]
        [SerializeField] private string npcName = "The Forest Guardian";

        [Header("Dialogue Content")]
        [TextArea(2, 5)]
        [SerializeField] private string[] initialDialogue = new string[]
        {
            "Greetings, traveler. You seek the legendary Lost Relic.",
            "The first fragment rests within the sacred shrine beyond the Torii gate ahead.",
            "Simply approach the ancient altar and press [E] to awaken the forest shrine!",
            "The shrine will illuminate and release the Forest Relic for you to claim."
        };

        [TextArea(2, 4)]
        [SerializeField] private string[] repeatDialogue = new string[]
        {
            "Walk through the Torii gate to the ancient shrine.",
            "Press [E] on the altar to awaken the shrine and claim the Forest Relic!"
        };

        [Header("Behavior")]
        [SerializeField] private float lookAtDistance = 6.0f;
        [SerializeField] private float rotationSpeed = 4.0f;

        [Header("Animation")]
        [Tooltip("Animator Controller with natural idle animation for the NPC.")]
        [SerializeField] private RuntimeAnimatorController npcAnimatorController;
        [Tooltip("Humanoid Avatar for the character rig.")]
        [SerializeField] private Avatar npcAvatar;

        private Transform playerTransform;
        private bool hasSpokenBefore;

        public bool CanInteract => true;

        private void Awake()
        {
            EnsureAnimator();
        }

        public void EnsureAnimator()
        {
            Animator anim = GetComponentInChildren<Animator>(true);
            if (anim == null)
            {
                Transform model = transform.Find("male03_1") ?? transform.Find("male02_1") ?? transform.Find("male01_1");
                if (model == null && transform.childCount > 0)
                {
                    model = transform.GetChild(0);
                }

                GameObject targetObj = model != null ? model.gameObject : gameObject;
                anim = targetObj.AddComponent<Animator>();
            }

            if (anim != null)
            {
                if (anim.runtimeAnimatorController == null && npcAnimatorController != null)
                {
                    anim.runtimeAnimatorController = npcAnimatorController;
                }
#if UNITY_EDITOR
                if (anim.runtimeAnimatorController == null)
                {
                    anim.runtimeAnimatorController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/NPC_AnimatorController.controller");
                }
                if (anim.avatar == null)
                {
                    Avatar av = null;
                    if (name.Contains("Sage") || name.Contains("Frozen"))
                    {
                        av = UnityEditor.AssetDatabase.LoadAssetAtPath<Avatar>("Assets/ThirdParty/Floreswa/Models/male03_1.fbx");
                    }
                    else if (name.Contains("Canyon"))
                    {
                        av = UnityEditor.AssetDatabase.LoadAssetAtPath<Avatar>("Assets/ThirdParty/Floreswa/Models/male02_1.fbx");
                    }
                    if (av == null)
                    {
                        av = UnityEditor.AssetDatabase.LoadAssetAtPath<Avatar>("Assets/ThirdParty/Floreswa/Models/male01_1.fbx");
                    }
                    anim.avatar = av;
                }
#endif
                anim.applyRootMotion = false;
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                anim.enabled = true;
            }
        }

        private void Start()
        {
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }

        private void Update()
        {
            if (playerTransform == null)
            {
                var playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null) playerTransform = playerObj.transform;
                return;
            }

            // Smoothly face player when near
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist <= lookAtDistance)
            {
                Vector3 lookDir = (playerTransform.position - transform.position).normalized;
                lookDir.y = 0f;
                if (lookDir.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(lookDir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
                }
            }
        }

        public string GetInteractionPrompt()
        {
            return $"Press [E] to Speak to {npcName}";
        }

        public void Interact(GameObject interactor)
        {
            string[] linesToUse = hasSpokenBefore && repeatDialogue != null && repeatDialogue.Length > 0
                ? repeatDialogue
                : initialDialogue;

            hasSpokenBefore = true;

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartDialogue(npcName, linesToUse);
            }
        }
    }
}
