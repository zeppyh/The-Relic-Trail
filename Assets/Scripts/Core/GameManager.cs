using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RelicTrail.Core
{
    public enum BiomeType
    {
        ForgottenForest,
        ScorchedCanyon,
        FrozenRuins,
        FinalSanctuary
    }

    public enum GameState
    {
        MainMenu,
        Playing,
        Dialogue,
        Paused,
        GameOver,
        Victory
    }

    public enum ForestMissionState
    {
        NotStarted,
        HuntingEnemies,
        KillsCompleted,
        PaymentRequested,
        RelicPurchased
    }

    /// <summary>
    /// Central game manager maintaining biome progression, relics, coins, game states, and respawns.
    /// </summary>
    [AddComponentMenu("Relic Trail/Core/Game Manager")]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Progression State")]
        [SerializeField] private BiomeType currentBiome = BiomeType.ForgottenForest;
        [SerializeField] private int coinsCollected = 0;
        [SerializeField] private bool hasForestRelic;
        [SerializeField] private bool hasCanyonRelic;
        [SerializeField] private bool hasFrozenRelic;

        [Header("Forest Combat Mission")]
        [SerializeField] private ForestMissionState forestMissionState = ForestMissionState.NotStarted;
        [SerializeField] private int forestEnemiesDefeated = 0;
        [SerializeField] private int requiredForestKills = 10;
        [SerializeField] private int relicCostCoins = 5;

        [Header("Active State")]
        [SerializeField] private GameState currentGameState = GameState.Playing;
        [SerializeField] private bool defaultStartInMainMenu = true;
        private static bool? startInMainMenuOverride = null;

        public static bool StartInMainMenu
        {
            get => startInMainMenuOverride ?? true;
            set => startInMainMenuOverride = value;
        }

        // Events
        public event Action<int> OnCoinsChanged;
        public event Action<int, int> OnRelicsChanged; // current, max
        public event Action<BiomeType> OnBiomeChanged;
        public event Action<GameState> OnGameStateChanged;
        public event Action<ForestMissionState> OnForestMissionStateChanged;
        public event Action<int, int> OnForestKillProgressChanged;

        public BiomeType CurrentBiome => currentBiome;
        public int CoinsCollected => coinsCollected;
        public bool HasForestRelic => hasForestRelic;
        public bool HasCanyonRelic => hasCanyonRelic;
        public bool HasFrozenRelic => hasFrozenRelic;
        public int TotalRelicsCollected => (hasForestRelic ? 1 : 0) + (hasCanyonRelic ? 1 : 0) + (hasFrozenRelic ? 1 : 0);
        public GameState CurrentGameState => currentGameState;
        public ForestMissionState CurrentForestMissionState => forestMissionState;
        public int ForestEnemiesDefeated => forestEnemiesDefeated;
        public int RequiredForestKills => requiredForestKills;
        public int RelicCostCoins => relicCostCoins;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            OnCoinsChanged?.Invoke(coinsCollected);
            OnRelicsChanged?.Invoke(TotalRelicsCollected, 3);
            OnBiomeChanged?.Invoke(currentBiome);
            OnForestMissionStateChanged?.Invoke(forestMissionState);
            OnForestKillProgressChanged?.Invoke(forestEnemiesDefeated, requiredForestKills);

            bool shouldStartInMainMenu = startInMainMenuOverride.HasValue ? startInMainMenuOverride.Value : defaultStartInMainMenu;
            if (shouldStartInMainMenu)
            {
                SetGameState(GameState.MainMenu);
            }
            else
            {
                SetGameState(GameState.Playing);
            }
        }

        public void SetGameState(GameState newState)
        {
            currentGameState = newState;

            switch (newState)
            {
                case GameState.Playing:
                    Time.timeScale = 1f;
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    break;
                case GameState.Dialogue:
                    Time.timeScale = 1f;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    break;
                case GameState.Paused:
                case GameState.GameOver:
                case GameState.Victory:
                case GameState.MainMenu:
                    Time.timeScale = 0f;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    break;
            }

            OnGameStateChanged?.Invoke(newState);
        }

        public void AddCoins(int amount)
        {
            coinsCollected += amount;
            OnCoinsChanged?.Invoke(coinsCollected);
        }

        public bool SpendCoins(int amount)
        {
            if (coinsCollected < amount) return false;
            coinsCollected -= amount;
            OnCoinsChanged?.Invoke(coinsCollected);
            return true;
        }

        public void StartForestMission()
        {
            if (forestMissionState == ForestMissionState.NotStarted)
            {
                forestMissionState = ForestMissionState.HuntingEnemies;
                OnForestMissionStateChanged?.Invoke(forestMissionState);
                OnForestKillProgressChanged?.Invoke(forestEnemiesDefeated, requiredForestKills);
            }
        }

        public void RecordForestEnemyKill()
        {
            // Only count kills if the mission has been activated by speaking to The Explorer!
            if (forestMissionState != ForestMissionState.HuntingEnemies) return;

            forestEnemiesDefeated++;
            OnForestKillProgressChanged?.Invoke(forestEnemiesDefeated, requiredForestKills);

            if (forestEnemiesDefeated >= requiredForestKills)
            {
                forestMissionState = ForestMissionState.KillsCompleted;
                OnForestMissionStateChanged?.Invoke(forestMissionState);
            }
        }

        public void SetForestPaymentRequested()
        {
            if (forestMissionState == ForestMissionState.KillsCompleted)
            {
                forestMissionState = ForestMissionState.PaymentRequested;
                OnForestMissionStateChanged?.Invoke(forestMissionState);
            }
        }

        public bool PurchaseForestRelic()
        {
            if (forestMissionState != ForestMissionState.KillsCompleted && forestMissionState != ForestMissionState.PaymentRequested)
            {
                return false;
            }

            if (!SpendCoins(relicCostCoins))
            {
                return false;
            }

            forestMissionState = ForestMissionState.RelicPurchased;
            CollectRelic(BiomeType.ForgottenForest);
            OnForestMissionStateChanged?.Invoke(forestMissionState);
            return true;
        }

        public void CollectRelic(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.ForgottenForest:
                    hasForestRelic = true;
                    break;
                case BiomeType.ScorchedCanyon:
                    hasCanyonRelic = true;
                    break;
                case BiomeType.FrozenRuins:
                    hasFrozenRelic = true;
                    break;
            }

            OnRelicsChanged?.Invoke(TotalRelicsCollected, 3);
        }

        public bool HasAllRelics()
        {
            return hasForestRelic && hasCanyonRelic && hasFrozenRelic;
        }

        public void SetBiome(BiomeType biome)
        {
            currentBiome = biome;
            OnBiomeChanged?.Invoke(currentBiome);
        }

        public void TriggerGameOver()
        {
            SetGameState(GameState.GameOver);
        }

        public void TriggerVictory()
        {
            SetGameState(GameState.Victory);
        }

        public void RestartGame(bool toMainMenu = false)
        {
            StartInMainMenu = toMainMenu;
            Time.timeScale = 1f;
            coinsCollected = 0;
            hasForestRelic = false;
            hasCanyonRelic = false;
            hasFrozenRelic = false;
            currentBiome = BiomeType.ForgottenForest;
            forestMissionState = ForestMissionState.NotStarted;
            forestEnemiesDefeated = 0;

            var activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.buildIndex);
        }
    }
}
