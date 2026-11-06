using System;
using UnityEngine;
using RelicTrail.Core;
using RelicTrail.Interactions;
using RelicTrail.Progression;

namespace RelicTrail.NPC
{
    /// <summary>
    /// The Explorer NPC in Biome 1 (The Forgotten Forest).
    /// Manages the combat mission to eliminate 10 hostile forest beasts,
    /// requests payment for the uncovered Forest Relic Fragment,
    /// grants the relic upon payment, and activates the portal to Biome 2.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [AddComponentMenu("Relic Trail/NPC/Explorer Mission NPC")]
    public class ExplorerMissionNPC : MonoBehaviour, IInteractable
    {
        [Header("NPC Identity")]
        [SerializeField] private string npcName = "The Explorer";

        [Header("Portal Link")]
        [Tooltip("Portal to Biome 2 (Scorched Canyon) that opens once the relic is purchased.")]
        [SerializeField] private TeleportPortal linkedPortal;

        [Header("Effects & Audio")]
        [SerializeField] private AudioClip purchaseSuccessSound;
        [SerializeField] private GameObject relicRewardEffectPrefab;

        [Header("Behavior")]
        [SerializeField] private float lookAtDistance = 6.0f;
        [SerializeField] private float rotationSpeed = 4.0f;

        [Header("Animation")]
        [Tooltip("Animator Controller with natural idle animation for the NPC.")]
        [SerializeField] private RuntimeAnimatorController npcAnimatorController;
        [Tooltip("Humanoid Avatar for the character rig.")]
        [SerializeField] private Avatar npcAvatar;

        private Transform playerTransform;

        public bool CanInteract => true;
        public TeleportPortal LinkedPortal => linkedPortal;

        public void SetLinkedPortal(TeleportPortal portal)
        {
            linkedPortal = portal;
        }

        private void Awake()
        {
            EnsureAnimator();
        }

        public void EnsureAnimator()
        {
            Animator anim = GetComponentInChildren<Animator>(true);
            if (anim == null)
            {
                Transform model = transform.Find("male01_1");
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
                    anim.avatar = UnityEditor.AssetDatabase.LoadAssetAtPath<Avatar>("Assets/ThirdParty/Floreswa/Models/male01_1.fbx");
                }
#endif
                anim.applyRootMotion = false;
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                anim.enabled = true;
            }
        }

        private void Start()
        {
            FindPlayer();

            // If game is loaded and relic was already purchased, ensure portal is active
            if (GameManager.Instance != null && GameManager.Instance.CurrentForestMissionState == ForestMissionState.RelicPurchased)
            {
                if (linkedPortal != null && !linkedPortal.IsPortalActive)
                {
                    linkedPortal.ActivatePortal();
                }
            }
        }

        private void FindPlayer()
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
                FindPlayer();
                return;
            }

            // Smoothly rotate to face player when in range
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
            if (GameManager.Instance == null)
            {
                return $"Press [E] to Speak to {npcName}";
            }

            ForestMissionState state = GameManager.Instance.CurrentForestMissionState;
            int kills = GameManager.Instance.ForestEnemiesDefeated;
            int reqKills = GameManager.Instance.RequiredForestKills;
            int coins = GameManager.Instance.CoinsCollected;
            int cost = GameManager.Instance.RelicCostCoins;

            switch (state)
            {
                case ForestMissionState.NotStarted:
                    return $"Press [E] to Speak to {npcName}";

                case ForestMissionState.HuntingEnemies:
                    return $"Press [E] to Speak to {npcName} ({kills}/{reqKills} Beasts Defeated)";

                case ForestMissionState.KillsCompleted:
                    return $"Press [E] to Report to {npcName} (Mission Ready)";

                case ForestMissionState.PaymentRequested:
                    if (coins >= cost)
                    {
                        return $"Press [E] to Purchase Forest Relic ({cost} Coins)";
                    }
                    else
                    {
                        return $"Press [E] to Speak to {npcName} (Need {cost} Coins - Have {coins})";
                    }

                case ForestMissionState.RelicPurchased:
                    return $"Press [E] to Speak to {npcName}";

                default:
                    return $"Press [E] to Speak to {npcName}";
            }
        }

        public void Interact(GameObject interactor)
        {
            if (DialogueManager.Instance == null || GameManager.Instance == null) return;

            ForestMissionState state = GameManager.Instance.CurrentForestMissionState;
            int kills = GameManager.Instance.ForestEnemiesDefeated;
            int reqKills = GameManager.Instance.RequiredForestKills;
            int coins = GameManager.Instance.CoinsCollected;
            int cost = GameManager.Instance.RelicCostCoins;

            switch (state)
            {
                case ForestMissionState.NotStarted:
                    StartMissionIntroDialogue();
                    break;

                case ForestMissionState.HuntingEnemies:
                    StartHuntingReminderDialogue(kills, reqKills);
                    break;

                case ForestMissionState.KillsCompleted:
                    StartKillsCompletedDialogue(cost, coins);
                    break;

                case ForestMissionState.PaymentRequested:
                    if (coins >= cost)
                    {
                        ExecuteRelicPurchase();
                    }
                    else
                    {
                        StartNeedMoreCoinsDialogue(cost, coins);
                    }
                    break;

                case ForestMissionState.RelicPurchased:
                    StartRelicPurchasedDialogue();
                    break;
            }
        }

        private void StartMissionIntroDialogue()
        {
            string[] introLines = new string[]
            {
                "Greetings, traveler! I am the Explorer of these ancient, uncharted woods.",
                "A vicious pack of hostile crimson beasts has invaded the forest, threatening everything in sight.",
                "The northern portal to The Scorched Canyon is locked tight, and I cannot safely explore further.",
                "If you can defeat at least 10 of these savage beasts, I have an ancient relic fragment that will awaken the portal!",
                "Take up your weapon and clear the woods. Return to me once you have defeated 10 beasts!"
            };

            DialogueManager.Instance.StartDialogue(npcName, introLines, () =>
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.StartForestMission();
                }
            });
        }

        private void StartHuntingReminderDialogue(int kills, int reqKills)
        {
            string[] reminderLines = new string[]
            {
                $"The forest beasts are still lurking out there! You have defeated {kills} of {reqKills} beasts.",
                $"Defeat {Mathf.Max(0, reqKills - kills)} more beasts to clear the threat, then return to me!"
            };

            DialogueManager.Instance.StartDialogue(npcName, reminderLines);
        }

        private void StartKillsCompletedDialogue(int cost, int coins)
        {
            string[] completedLines;

            if (coins >= cost)
            {
                completedLines = new string[]
                {
                    "Tremendous courage, traveler! You have slain 10 of the beasts and pacified the forest!",
                    "As promised, I have the ancient Forest Relic Fragment. Its magical glow holds the power to open the northern portal.",
                    $"However, exploring is costly work. I am offering the relic in exchange for {cost} gold coins.",
                    $"I see you already have {coins} coins on hand! Speak with me again to purchase the relic and open the way forward."
                };
            }
            else
            {
                completedLines = new string[]
                {
                    "Tremendous courage, traveler! You have slain 10 of the beasts and pacified the forest!",
                    "As promised, I have the ancient Forest Relic Fragment. Its magical glow holds the power to open the northern portal.",
                    $"However, exploring is costly work. I am offering the relic in exchange for {cost} gold coins.",
                    $"You currently possess {coins} coins. Search around the forest or defeat remaining beasts to collect {cost} coins, then return to me!"
                };
            }

            DialogueManager.Instance.StartDialogue(npcName, completedLines, () =>
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SetForestPaymentRequested();
                }
            });
        }

        private void StartNeedMoreCoinsDialogue(int cost, int coins)
        {
            string[] needCoinsLines = new string[]
            {
                $"I cannot surrender the Forest Relic just yet. The price is {cost} gold coins.",
                $"You currently have {coins} coins. You need {cost - coins} more coins.",
                "Look for golden coins scattered among the forest trees and campsites, or defeat additional beasts to earn coins!"
            };

            DialogueManager.Instance.StartDialogue(npcName, needCoinsLines);
        }

        private void ExecuteRelicPurchase()
        {
            if (GameManager.Instance == null) return;

            bool success = GameManager.Instance.PurchaseForestRelic();
            if (!success) return;

            // Trigger portal unlock
            if (linkedPortal != null)
            {
                linkedPortal.ActivatePortal();
            }

            // Audio & visual feedback
            if (purchaseSuccessSound != null)
            {
                AudioSource.PlayClipAtPoint(purchaseSuccessSound, transform.position, 1.0f);
            }

            if (relicRewardEffectPrefab != null)
            {
                Instantiate(relicRewardEffectPrefab, transform.position + Vector3.up * 1.2f, Quaternion.identity);
            }

            string[] purchasedLines = new string[]
            {
                "A fair bargain! Here is the ancient Forest Relic Fragment, secured from the depths of these woods.",
                "Look to the north! The dormant portal to The Scorched Canyon has burst into life with radiant light!",
                "Step through the portal whenever you are prepared. Safe travels, fellow adventurer!"
            };

            DialogueManager.Instance.StartDialogue(npcName, purchasedLines);
        }

        private void StartRelicPurchasedDialogue()
        {
            string[] standingByLines = new string[]
            {
                "The portal to The Scorched Canyon remains fully activated to the north.",
                "Step through the portal when you are ready to continue your search for the remaining relic fragments!"
            };

            DialogueManager.Instance.StartDialogue(npcName, standingByLines);
        }
    }
}
