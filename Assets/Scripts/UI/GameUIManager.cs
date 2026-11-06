#pragma warning disable CS0618
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using RelicTrail.Core;
using RelicTrail.Player;

namespace RelicTrail.UI
{
    /// <summary>
    /// Master UI manager controlling HUD, health bar, objective text, prompts,
    /// Main Menu, Pause Menu, Game Over Screen, and Victory Screen.
    /// All buttons are guaranteed functional based on what is written on them.
    /// </summary>
    [AddComponentMenu("Relic Trail/UI/Game UI Manager")]
    public class GameUIManager : MonoBehaviour
    {
        public static GameUIManager Instance { get; private set; }

        [Header("HUD Elements")]
        [SerializeField] private GameObject hudCanvas;
        [SerializeField] private Slider healthBar;
        [SerializeField] private Text healthText;
        [SerializeField] private Slider staminaBar;
        [SerializeField] private Text staminaText;
        [SerializeField] private Text biomeNameText;
        [SerializeField] private Text objectiveText;
        [SerializeField] private Text coinCountText;
        [SerializeField] private Text relicProgressText;
        [SerializeField] private GameObject promptPanel;
        [SerializeField] private Text promptText;

        [Header("Main Menu")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private Button playButton;
        [SerializeField] private Button instructionsButton;
        [SerializeField] private Button mainMenuQuitButton;
        [SerializeField] private GameObject instructionsPanel;
        [SerializeField] private Button closeInstructionsButton;

        [Header("Pause Menu")]
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button pauseRestartButton;
        [SerializeField] private Button pauseMainMenuButton;
        [SerializeField] private Button pauseQuitButton;

        [Header("Game Over Screen")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button gameOverMainMenuButton;
        [SerializeField] private Button gameOverQuitButton;

        [Header("Victory Screen")]
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private Text victoryStatsText;
        [SerializeField] private Button victoryRestartButton;
        [SerializeField] private Button victoryMainMenuButton;
        [SerializeField] private Button victoryQuitButton;

        private PlayerHealth playerHealth;
        private PlayerStamina playerStamina;
        private PlayerInteraction playerInteraction;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureEventSystem();
            EnsureMenuPanelsAndButtons();
            SetupButtonListeners();
            BindAllButtonsByText();
        }

        private void Start()
        {
            FindPlayerComponents();
            SubscribeEvents();

            // Refresh initial UI based on current GameState
            GameState startState = GameManager.Instance != null ? GameManager.Instance.CurrentGameState : GameState.MainMenu;
            HandleGameStateChanged(startState);
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        /// <summary>
        /// Guarantees that an EventSystem with InputSystemUIInputModule exists in the scene.
        /// Without this, Unity UI buttons cannot receive any click or hover events with the New Input System!
        /// </summary>
        public void EnsureEventSystem()
        {
            var es = FindObjectOfType<EventSystem>();
            if (es == null)
            {
                var esObj = new GameObject("EventSystem");
                es = esObj.AddComponent<EventSystem>();
                esObj.AddComponent<InputSystemUIInputModule>();
            }
            else
            {
                var module = es.GetComponent<InputSystemUIInputModule>();
                if (module == null)
                {
                    var oldModule = es.GetComponent<BaseInputModule>();
                    if (oldModule != null && !(oldModule is InputSystemUIInputModule))
                    {
                        if (Application.isPlaying) Destroy(oldModule);
                        else DestroyImmediate(oldModule);
                    }
                    es.gameObject.AddComponent<InputSystemUIInputModule>();
                }
            }
        }

        /// <summary>
        /// Ensures all menus (Main Menu, Instructions, Game Over, Victory, Pause) exist
        /// and contain fully functional buttons matching their written text.
        /// </summary>
        public void EnsureMenuPanelsAndButtons()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            Transform canvasTrans = canvas.transform;
            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // 1. Ensure Main Menu Panel
            if (mainMenuPanel == null)
            {
                Transform existing = canvasTrans.Find("MainMenuPanel");
                if (existing != null)
                {
                    mainMenuPanel = existing.gameObject;
                }
                else
                {
                    mainMenuPanel = CreateMenuPanel(canvasTrans, "MainMenuPanel", new Color(0.04f, 0.07f, 0.12f, 0.96f));
                    CreateTextObject(mainMenuPanel.transform, "THE RELIC TRAIL", defaultFont, 40, new Color(1f, 0.85f, 0.2f), new Vector2(0f, 130f), new Vector2(600f, 60f), TextAnchor.MiddleCenter);
                    CreateTextObject(mainMenuPanel.transform, "An Action-Adventure RPG", defaultFont, 18, new Color(0.6f, 0.9f, 1f), new Vector2(0f, 80f), new Vector2(600f, 30f), TextAnchor.MiddleCenter);

                    playButton = CreateButton(mainMenuPanel.transform, "PlayButton", "Play Game", defaultFont, new Vector2(0f, 15f), new Color(0.15f, 0.55f, 0.35f));
                    instructionsButton = CreateButton(mainMenuPanel.transform, "InstructionsButton", "Instructions", defaultFont, new Vector2(0f, -45f), new Color(0.2f, 0.4f, 0.65f));
                    mainMenuQuitButton = CreateButton(mainMenuPanel.transform, "QuitButton", "Quit Game", defaultFont, new Vector2(0f, -105f), new Color(0.65f, 0.2f, 0.2f));
                }
            }

            // Find buttons in Main Menu if not wired
            if (mainMenuPanel != null)
            {
                if (playButton == null) playButton = FindButtonWithTextOrName(mainMenuPanel.transform, "play");
                if (instructionsButton == null) instructionsButton = FindButtonWithTextOrName(mainMenuPanel.transform, "instruction");
                if (mainMenuQuitButton == null) mainMenuQuitButton = FindButtonWithTextOrName(mainMenuPanel.transform, "quit");
            }

            // 2. Ensure Instructions Modal
            if (instructionsPanel == null)
            {
                Transform existing = canvasTrans.Find("InstructionsPanel");
                if (existing == null && mainMenuPanel != null) existing = mainMenuPanel.transform.Find("InstructionsPanel");

                if (existing != null)
                {
                    instructionsPanel = existing.gameObject;
                }
                else
                {
                    Transform parent = mainMenuPanel != null ? mainMenuPanel.transform : canvasTrans;
                    instructionsPanel = CreateMenuPanel(parent, "InstructionsPanel", new Color(0.06f, 0.09f, 0.14f, 0.98f));
                    var rt = instructionsPanel.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.anchorMin = new Vector2(0.5f, 0.5f);
                        rt.anchorMax = new Vector2(0.5f, 0.5f);
                        rt.pivot = new Vector2(0.5f, 0.5f);
                        rt.sizeDelta = new Vector2(740f, 520f);
                        rt.anchoredPosition = Vector2.zero;
                    }

                    CreateTextObject(instructionsPanel.transform, "HOW TO PLAY & CONTROLS", defaultFont, 24, new Color(1f, 0.85f, 0.2f), new Vector2(0f, 215f), new Vector2(680f, 40f), TextAnchor.MiddleCenter);

                    string guideText =
                        "CONTROLS:\n" +
                        "• W / A / S / D : Move Character\n" +
                        "• Mouse Movement : Look & Aim Camera\n" +
                        "• Left Mouse Button (LMB) : Shoot / Light Attack\n" +
                        "• Right Mouse Button (RMB) : Aim Down Sights (ADS)\n" +
                        "• Q Key : Super Charged Attack\n" +
                        "• Left Ctrl : Lock-On / Unlock Enemy Target\n" +
                        "• E Key : Interact / Talk to NPCs / Purchase Relics\n" +
                        "• V Key : Toggle Shoulder Camera Side\n" +
                        "• Esc Key : Pause / Resume Game\n\n" +
                        "MISSION OBJECTIVE:\n" +
                        "Explore each biome, defeat hostile beasts to fulfill NPC missions,\n" +
                        "collect coins, restore ancient Relics, and unlock the portal to the Sanctuary!";

                    CreateTextObject(instructionsPanel.transform, guideText, defaultFont, 16, Color.white, new Vector2(0f, 20f), new Vector2(680f, 320f), TextAnchor.MiddleCenter);

                    closeInstructionsButton = CreateButton(instructionsPanel.transform, "CloseButton", "Close", defaultFont, new Vector2(0f, -210f), new Color(0.25f, 0.45f, 0.7f));
                }
            }

            if (instructionsPanel != null && closeInstructionsButton == null)
            {
                closeInstructionsButton = FindButtonWithTextOrName(instructionsPanel.transform, "close");
            }

            // 3. Ensure Game Over screen has "Main Menu" button if missing
            if (gameOverPanel != null)
            {
                if (retryButton == null) retryButton = FindButtonWithTextOrName(gameOverPanel.transform, "retry");
                if (gameOverQuitButton == null) gameOverQuitButton = FindButtonWithTextOrName(gameOverPanel.transform, "quit");
                if (gameOverMainMenuButton == null) gameOverMainMenuButton = FindButtonWithTextOrName(gameOverPanel.transform, "main menu");

                if (gameOverMainMenuButton == null && retryButton != null)
                {
                    // Reposition buttons cleanly to fit 3 options
                    var rtRetry = retryButton.GetComponent<RectTransform>();
                    if (rtRetry != null) rtRetry.anchoredPosition = new Vector2(0f, 25f);

                    gameOverMainMenuButton = CreateButton(gameOverPanel.transform, "GOMainMenuButton", "Main Menu", defaultFont, new Vector2(0f, -30f), new Color(0.2f, 0.4f, 0.65f));

                    if (gameOverQuitButton != null)
                    {
                        var rtQuit = gameOverQuitButton.GetComponent<RectTransform>();
                        if (rtQuit != null) rtQuit.anchoredPosition = new Vector2(0f, -85f);
                    }
                }
            }

            // 4. Ensure Victory screen has "Main Menu" button if missing
            if (victoryPanel != null)
            {
                if (victoryRestartButton == null) victoryRestartButton = FindButtonWithTextOrName(victoryPanel.transform, "play again") ?? FindButtonWithTextOrName(victoryPanel.transform, "restart");
                if (victoryQuitButton == null) victoryQuitButton = FindButtonWithTextOrName(victoryPanel.transform, "exit") ?? FindButtonWithTextOrName(victoryPanel.transform, "quit");
                if (victoryMainMenuButton == null) victoryMainMenuButton = FindButtonWithTextOrName(victoryPanel.transform, "main menu");

                if (victoryMainMenuButton == null && victoryRestartButton != null)
                {
                    var rtRestart = victoryRestartButton.GetComponent<RectTransform>();
                    if (rtRestart != null) rtRestart.anchoredPosition = new Vector2(0f, -10f);

                    victoryMainMenuButton = CreateButton(victoryPanel.transform, "VicMainMenuButton", "Main Menu", defaultFont, new Vector2(0f, -65f), new Color(0.2f, 0.4f, 0.65f));

                    if (victoryQuitButton != null)
                    {
                        var rtQuit = victoryQuitButton.GetComponent<RectTransform>();
                        if (rtQuit != null) rtQuit.anchoredPosition = new Vector2(0f, -120f);
                    }
                }
            }

            // 5. Ensure Pause menu has "Main Menu" button if missing
            if (pauseMenuPanel != null)
            {
                if (resumeButton == null) resumeButton = FindButtonWithTextOrName(pauseMenuPanel.transform, "resume");
                if (pauseRestartButton == null) pauseRestartButton = FindButtonWithTextOrName(pauseMenuPanel.transform, "restart");
                if (pauseQuitButton == null) pauseQuitButton = FindButtonWithTextOrName(pauseMenuPanel.transform, "quit");
                if (pauseMainMenuButton == null) pauseMainMenuButton = FindButtonWithTextOrName(pauseMenuPanel.transform, "main menu");

                if (pauseMainMenuButton == null && resumeButton != null)
                {
                    var rtResume = resumeButton.GetComponent<RectTransform>();
                    if (rtResume != null) rtResume.anchoredPosition = new Vector2(0f, 50f);

                    if (pauseRestartButton != null)
                    {
                        var rtRestart = pauseRestartButton.GetComponent<RectTransform>();
                        if (rtRestart != null) rtRestart.anchoredPosition = new Vector2(0f, -5f);
                    }

                    pauseMainMenuButton = CreateButton(pauseMenuPanel.transform, "PauseMainMenuButton", "Main Menu", defaultFont, new Vector2(0f, -60f), new Color(0.2f, 0.4f, 0.65f));

                    if (pauseQuitButton != null)
                    {
                        var rtQuit = pauseQuitButton.GetComponent<RectTransform>();
                        if (rtQuit != null) rtQuit.anchoredPosition = new Vector2(0f, -115f);
                    }
                }
            }
        }

        private void SetupButtonListeners()
        {
            // Main Menu
            if (playButton != null) { playButton.onClick.RemoveListener(StartGame); playButton.onClick.AddListener(StartGame); }
            if (instructionsButton != null) { instructionsButton.onClick.RemoveListener(ShowInstructions); instructionsButton.onClick.AddListener(ShowInstructions); }
            if (closeInstructionsButton != null) { closeInstructionsButton.onClick.RemoveListener(HideInstructions); closeInstructionsButton.onClick.AddListener(HideInstructions); }
            if (mainMenuQuitButton != null) { mainMenuQuitButton.onClick.RemoveListener(QuitGame); mainMenuQuitButton.onClick.AddListener(QuitGame); }

            // Pause Menu
            if (resumeButton != null) { resumeButton.onClick.RemoveListener(ResumeGame); resumeButton.onClick.AddListener(ResumeGame); }
            if (pauseRestartButton != null) { pauseRestartButton.onClick.RemoveListener(RestartGame); pauseRestartButton.onClick.AddListener(RestartGame); }
            if (pauseMainMenuButton != null) { pauseMainMenuButton.onClick.RemoveListener(ReturnToMainMenu); pauseMainMenuButton.onClick.AddListener(ReturnToMainMenu); }
            if (pauseQuitButton != null) { pauseQuitButton.onClick.RemoveListener(QuitGame); pauseQuitButton.onClick.AddListener(QuitGame); }

            // Game Over
            if (retryButton != null) { retryButton.onClick.RemoveListener(RestartGame); retryButton.onClick.AddListener(RestartGame); }
            if (gameOverMainMenuButton != null) { gameOverMainMenuButton.onClick.RemoveListener(ReturnToMainMenu); gameOverMainMenuButton.onClick.AddListener(ReturnToMainMenu); }
            if (gameOverQuitButton != null) { gameOverQuitButton.onClick.RemoveListener(QuitGame); gameOverQuitButton.onClick.AddListener(QuitGame); }

            // Victory
            if (victoryRestartButton != null) { victoryRestartButton.onClick.RemoveListener(RestartGame); victoryRestartButton.onClick.AddListener(RestartGame); }
            if (victoryMainMenuButton != null) { victoryMainMenuButton.onClick.RemoveListener(ReturnToMainMenu); victoryMainMenuButton.onClick.AddListener(ReturnToMainMenu); }
            if (victoryQuitButton != null) { victoryQuitButton.onClick.RemoveListener(QuitGame); victoryQuitButton.onClick.AddListener(QuitGame); }
        }

        /// <summary>
        /// Universal button auto-binder that dynamically connects every Button in the UI hierarchy
        /// according to what is written on its label.
        /// </summary>
        public void BindAllButtonsByText()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            Button[] allButtons = canvas.GetComponentsInChildren<Button>(true);
            foreach (var btn in allButtons)
            {
                if (btn == null) continue;

                string text = "";
                var txt = btn.GetComponentInChildren<Text>(true);
                if (txt != null && !string.IsNullOrWhiteSpace(txt.text))
                {
                    text = txt.text.Trim().ToLowerInvariant();
                }
                else
                {
                    text = btn.gameObject.name.ToLowerInvariant();
                }

                if (text.Contains("main menu") || text.Contains("to menu") || text.Contains("title screen"))
                {
                    btn.onClick.RemoveListener(ReturnToMainMenu);
                    btn.onClick.AddListener(ReturnToMainMenu);
                }
                else if (text.Contains("play again") || text.Contains("retry") || text.Contains("restart"))
                {
                    btn.onClick.RemoveListener(RestartGame);
                    btn.onClick.AddListener(RestartGame);
                }
                else if (text.Contains("resume") || text.Contains("continue"))
                {
                    btn.onClick.RemoveListener(ResumeGame);
                    btn.onClick.AddListener(ResumeGame);
                }
                else if (text.Contains("play") || text.Contains("start"))
                {
                    btn.onClick.RemoveListener(StartGame);
                    btn.onClick.AddListener(StartGame);
                }
                else if (text.Contains("instruction") || text.Contains("control") || text.Contains("how to play"))
                {
                    btn.onClick.RemoveListener(ShowInstructions);
                    btn.onClick.AddListener(ShowInstructions);
                }
                else if (text.Contains("close") || text.Contains("back"))
                {
                    btn.onClick.RemoveListener(HideInstructions);
                    btn.onClick.AddListener(HideInstructions);
                }
                else if (text.Contains("quit") || text.Contains("exit"))
                {
                    btn.onClick.RemoveListener(QuitGame);
                    btn.onClick.AddListener(QuitGame);
                }
            }
        }

        private Button FindButtonWithTextOrName(Transform root, string keyword)
        {
            if (root == null) return null;
            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            keyword = keyword.ToLowerInvariant();

            foreach (var b in buttons)
            {
                var txt = b.GetComponentInChildren<Text>(true);
                if (txt != null && txt.text.ToLowerInvariant().Contains(keyword))
                {
                    return b;
                }
                if (b.gameObject.name.ToLowerInvariant().Contains(keyword))
                {
                    return b;
                }
            }
            return null;
        }

        private void FindPlayerComponents()
        {
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                playerHealth = playerObj.GetComponent<PlayerHealth>();
                playerStamina = playerObj.GetComponent<PlayerStamina>();
                playerInteraction = playerObj.GetComponent<PlayerInteraction>();

                if (playerHealth != null)
                {
                    playerHealth.OnHealthChanged += UpdateHealthBar;
                    playerHealth.OnPlayerDied += HandlePlayerDeath;
                    UpdateHealthBar(playerHealth.CurrentHealth, playerHealth.MaxHealth);
                }

                if (playerStamina != null)
                {
                    playerStamina.OnStaminaChanged += UpdateStaminaBar;
                    UpdateStaminaBar(playerStamina.CurrentStamina, playerStamina.MaxStamina);
                }

                if (playerInteraction != null)
                {
                    playerInteraction.OnPromptChanged += ShowPrompt;
                    playerInteraction.OnPromptCleared += HidePrompt;
                }
            }
        }

        private void SubscribeEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnCoinsChanged += UpdateCoins;
                GameManager.Instance.OnRelicsChanged += UpdateRelics;
                GameManager.Instance.OnBiomeChanged += UpdateBiome;
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
                GameManager.Instance.OnForestMissionStateChanged += UpdateForestMissionUI;
                GameManager.Instance.OnForestKillProgressChanged += UpdateForestKillProgressUI;

                UpdateCoins(GameManager.Instance.CoinsCollected);
                UpdateRelics(GameManager.Instance.TotalRelicsCollected, 3);
                UpdateBiome(GameManager.Instance.CurrentBiome);
            }
        }

        private void UnsubscribeEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnCoinsChanged -= UpdateCoins;
                GameManager.Instance.OnRelicsChanged -= UpdateRelics;
                GameManager.Instance.OnBiomeChanged -= UpdateBiome;
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
                GameManager.Instance.OnForestMissionStateChanged -= UpdateForestMissionUI;
                GameManager.Instance.OnForestKillProgressChanged -= UpdateForestKillProgressUI;
            }

            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= UpdateHealthBar;
                playerHealth.OnPlayerDied -= HandlePlayerDeath;
            }

            if (playerStamina != null)
            {
                playerStamina.OnStaminaChanged -= UpdateStaminaBar;
            }

            if (playerInteraction != null)
            {
                playerInteraction.OnPromptChanged -= ShowPrompt;
                playerInteraction.OnPromptCleared -= HidePrompt;
            }
        }

        private void Update()
        {
            // Toggle Pause menu or close Instructions with Escape key
            bool escapePressed = UnityEngine.InputSystem.Keyboard.current != null &&
                                 UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame;

            if (escapePressed)
            {
                if (instructionsPanel != null && instructionsPanel.activeSelf)
                {
                    HideInstructions();
                    return;
                }

                if (GameManager.Instance != null)
                {
                    if (GameManager.Instance.CurrentGameState == GameState.Playing)
                    {
                        GameManager.Instance.SetGameState(GameState.Paused);
                    }
                    else if (GameManager.Instance.CurrentGameState == GameState.Paused)
                    {
                        GameManager.Instance.SetGameState(GameState.Playing);
                    }
                }
            }
        }

        private void UpdateHealthBar(float current, float max)
        {
            if (healthBar != null)
            {
                healthBar.maxValue = max;
                healthBar.value = current;
            }
            if (healthText != null)
            {
                healthText.text = $"HP: {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
            }
        }

        private void UpdateStaminaBar(float current, float max)
        {
            if (staminaBar != null)
            {
                staminaBar.maxValue = max;
                staminaBar.value = current;
            }
            if (staminaText != null)
            {
                staminaText.text = $"SP: {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
            }
        }

        private void UpdateCoins(int count)
        {
            if (coinCountText != null)
            {
                coinCountText.text = $"Coins: {count}";
            }

            if (GameManager.Instance != null && GameManager.Instance.CurrentBiome == BiomeType.ForgottenForest)
            {
                if (GameManager.Instance.CurrentForestMissionState == ForestMissionState.PaymentRequested)
                {
                    UpdateForestMissionUI(ForestMissionState.PaymentRequested);
                }
            }
        }

        private void UpdateRelics(int current, int total)
        {
            if (relicProgressText != null)
            {
                relicProgressText.text = $"Relic Fragments: {current} / {total}";
            }
        }

        private void UpdateBiome(BiomeType biome)
        {
            string biomeName = "Forgotten Forest";
            string objective = "Explore the woods and speak with the Forest Guardian.";

            switch (biome)
            {
                case BiomeType.ForgottenForest:
                    biomeName = "The Forgotten Forest";
                    if (GameManager.Instance != null)
                    {
                        if (biomeNameText != null) biomeNameText.text = biomeName;
                        UpdateForestMissionUI(GameManager.Instance.CurrentForestMissionState);
                        return;
                    }
                    objective = "Talk to The Explorer at the camp to receive your mission.";
                    break;
                case BiomeType.ScorchedCanyon:
                    biomeName = "The Scorched Canyon";
                    objective = "Locate the Canyon Explorer, activate the pressure plates, and retrieve the Canyon Relic.";
                    break;
                case BiomeType.FrozenRuins:
                    biomeName = "The Frozen Ruins";
                    objective = "Seek the Frozen Sage, align the ancient crystals, and free the Frozen Relic.";
                    break;
                case BiomeType.FinalSanctuary:
                    biomeName = "The Final Sanctuary";
                    objective = "Approach the central altar and restore the Lost Relic to achieve victory!";
                    break;
            }

            if (biomeNameText != null) biomeNameText.text = biomeName;
            if (objectiveText != null) objectiveText.text = $"Objective: {objective}";
        }

        private void UpdateForestMissionUI(ForestMissionState state)
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentBiome != BiomeType.ForgottenForest)
                return;

            switch (state)
            {
                case ForestMissionState.NotStarted:
                    SetCustomObjective("Talk to The Explorer at the camp to receive your mission.");
                    break;
                case ForestMissionState.HuntingEnemies:
                    SetCustomObjective($"Defeat hostile forest beasts ({GameManager.Instance.ForestEnemiesDefeated}/{GameManager.Instance.RequiredForestKills})");
                    break;
                case ForestMissionState.KillsCompleted:
                    SetCustomObjective("Return to The Explorer at the camp.");
                    break;
                case ForestMissionState.PaymentRequested:
                    int cost = GameManager.Instance.RelicCostCoins;
                    int coins = GameManager.Instance.CoinsCollected;
                    if (coins >= cost)
                    {
                        SetCustomObjective($"Talk to The Explorer to purchase the Forest Relic ({cost} Coins)");
                    }
                    else
                    {
                        SetCustomObjective($"Collect {cost} coins to buy the Forest Relic ({coins}/{cost})");
                    }
                    break;
                case ForestMissionState.RelicPurchased:
                    SetCustomObjective("Step through the awakened portal to The Scorched Canyon!");
                    break;
            }
        }

        private void UpdateForestKillProgressUI(int current, int required)
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentBiome != BiomeType.ForgottenForest)
                return;

            if (GameManager.Instance.CurrentForestMissionState == ForestMissionState.HuntingEnemies)
            {
                SetCustomObjective($"Defeat hostile forest beasts ({current}/{required})");
            }
        }

        public void SetCustomObjective(string objective)
        {
            if (objectiveText != null)
            {
                objectiveText.text = $"Objective: {objective}";
            }
        }

        public void ShowPrompt(string prompt)
        {
            if (promptPanel != null) promptPanel.SetActive(true);
            if (promptText != null) promptText.text = prompt;
        }

        public void HidePrompt()
        {
            if (promptPanel != null) promptPanel.SetActive(false);
        }

        private void HandlePlayerDeath()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerGameOver();
            }
        }

        public void HandleGameStateChanged(GameState state)
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(state == GameState.MainMenu);
            if (instructionsPanel != null && state != GameState.MainMenu) instructionsPanel.SetActive(false);
            if (hudCanvas != null) hudCanvas.SetActive(state == GameState.Playing || state == GameState.Dialogue);
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(state == GameState.Paused);
            if (gameOverPanel != null) gameOverPanel.SetActive(state == GameState.GameOver);
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(state == GameState.Victory);
                if (state == GameState.Victory && victoryStatsText != null && GameManager.Instance != null)
                {
                    victoryStatsText.text = $"Total Coins Collected: {GameManager.Instance.CoinsCollected}\nAll 3 Relic Fragments Restored!";
                }
            }
        }

        // ==========================================
        // Button Functional Actions
        // ==========================================

        public void StartGame()
        {
            PlayButtonSound();
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (instructionsPanel != null) instructionsPanel.SetActive(false);
            if (hudCanvas != null) hudCanvas.SetActive(true);

            if (GameManager.Instance != null)
            {
                GameManager.StartInMainMenu = false;
                GameManager.Instance.SetGameState(GameState.Playing);
            }
        }

        public void ShowInstructions()
        {
            PlayButtonSound();
            if (instructionsPanel != null) instructionsPanel.SetActive(true);
        }

        public void HideInstructions()
        {
            PlayButtonSound();
            if (instructionsPanel != null) instructionsPanel.SetActive(false);
        }

        public void ResumeGame()
        {
            PlayButtonSound();
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetGameState(GameState.Playing);
            }
        }

        public void RestartGame()
        {
            PlayButtonSound();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartGame(toMainMenu: false);
            }
            else
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }

        public void ReturnToMainMenu()
        {
            PlayButtonSound();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartGame(toMainMenu: true);
            }
            else
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }

        public void QuitGame()
        {
            PlayButtonSound();
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private void PlayButtonSound()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayButtonClick();
            }
        }

        // Helper UI creation methods
        private static GameObject CreateMenuPanel(Transform parent, string name, Color bgColor)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            var img = panel.AddComponent<Image>();
            img.color = bgColor;
            return panel;
        }

        private static GameObject CreateTextObject(
            Transform parent,
            string text,
            Font font,
            int size,
            Color col,
            Vector2 pos,
            Vector2 sizeDelta,
            TextAnchor align)
        {
            GameObject obj = new GameObject("Text");
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = sizeDelta;
            var t = obj.AddComponent<Text>();
            t.text = text;
            t.font = font;
            t.fontSize = size;
            t.color = col;
            t.alignment = align;
            return obj;
        }

        private static Button CreateButton(Transform parent, string name, string label, Font font, Vector2 pos, Color btnColor)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            var rect = btnObj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(200f, 44f);
            var img = btnObj.AddComponent<Image>();
            img.color = btnColor;
            var btn = btnObj.AddComponent<Button>();

            CreateTextObject(btnObj.transform, label, font, 18, Color.white, Vector2.zero, new Vector2(200f, 44f), TextAnchor.MiddleCenter);
            return btn;
        }
    }
}
