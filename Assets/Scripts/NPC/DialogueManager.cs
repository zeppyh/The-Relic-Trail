using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RelicTrail.Core;

namespace RelicTrail.NPC
{
    /// <summary>
    /// Central manager controlling the Dialogue UI, line advancement, and player state management during conversations.
    /// </summary>
    [AddComponentMenu("Relic Trail/NPC/Dialogue Manager")]
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private Text npcNameText;
        [SerializeField] private Text dialogueText;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button closeButton;

        [Header("Audio")]
        [SerializeField] private AudioClip voiceBlipSound;
        [SerializeField] private AudioSource audioSource;

        private Queue<string> currentSentences = new Queue<string>();
        private bool isDialogueActive;
        private Action onDialogueCompleted;

        public bool IsDialogueActive => isDialogueActive;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(DisplayNextSentence);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(EndDialogue);
            }

            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }
        }

        private void Update()
        {
            if (!isDialogueActive) return;

            // Advance dialogue with Space, E, or Enter key using Input System
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb.spaceKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)
                {
                    DisplayNextSentence();
                }
                else if (kb.escapeKey.wasPressedThisFrame)
                {
                    EndDialogue();
                }
            }
        }

        public void StartDialogue(string speakerName, string[] sentences, Action onComplete = null)
        {
            if (sentences == null || sentences.Length == 0) return;

            isDialogueActive = true;
            onDialogueCompleted = onComplete;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetGameState(GameState.Dialogue);
            }

            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(true);
            }

            if (npcNameText != null)
            {
                npcNameText.text = speakerName;
            }

            currentSentences.Clear();
            foreach (string sentence in sentences)
            {
                currentSentences.Enqueue(sentence);
            }

            DisplayNextSentence();
        }

        public void DisplayNextSentence()
        {
            if (currentSentences.Count == 0)
            {
                EndDialogue();
                return;
            }

            string sentence = currentSentences.Dequeue();
            StopAllCoroutines();
            StartCoroutine(TypeSentence(sentence));

            if (voiceBlipSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(voiceBlipSound, 0.6f);
            }
        }

        private IEnumerator TypeSentence(string sentence)
        {
            if (dialogueText != null)
            {
                dialogueText.text = "";
                foreach (char letter in sentence.ToCharArray())
                {
                    dialogueText.text += letter;
                    yield return new WaitForSeconds(0.015f);
                }
            }
        }

        public void EndDialogue()
        {
            isDialogueActive = false;

            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }

            if (GameManager.Instance != null && GameManager.Instance.CurrentGameState == GameState.Dialogue)
            {
                GameManager.Instance.SetGameState(GameState.Playing);
            }

            onDialogueCompleted?.Invoke();
            onDialogueCompleted = null;
        }
    }
}
