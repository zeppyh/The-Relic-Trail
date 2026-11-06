using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using RelicTrail.Core;

namespace RelicTrail.UI
{
    /// <summary>
    /// Controls the Main Menu UI, level loading, instruction panel, and quit action.
    /// </summary>
    [AddComponentMenu("Relic Trail/UI/Main Menu Controller")]
    public class MainMenuController : MonoBehaviour
    {
        [Header("Menu Panels")]
        [SerializeField] private GameObject menuRootPanel;
        [SerializeField] private GameObject instructionsPanel;

        [Header("Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button instructionsButton;
        [SerializeField] private Button closeInstructionsButton;
        [SerializeField] private Button exitButton;

        [Header("Scene to Load")]
        [SerializeField] private string gameSceneName = "SampleScene";

        private void Awake()
        {
            if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);
            if (instructionsButton != null) instructionsButton.onClick.AddListener(OnInstructionsClicked);
            if (closeInstructionsButton != null) closeInstructionsButton.onClick.AddListener(OnCloseInstructionsClicked);
            if (exitButton != null) exitButton.onClick.AddListener(OnExitClicked);

            if (instructionsPanel != null) instructionsPanel.SetActive(false);
            if (menuRootPanel != null) menuRootPanel.SetActive(true);
        }

        public void OnPlayClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();

            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.StartGame();
            }
            else if (GameManager.Instance != null)
            {
                GameManager.StartInMainMenu = false;
                if (menuRootPanel != null) menuRootPanel.SetActive(false);
                GameManager.Instance.SetGameState(GameState.Playing);
            }
            else
            {
                SceneManager.LoadScene(gameSceneName);
            }
        }

        public void OnInstructionsClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
            if (instructionsPanel != null) instructionsPanel.SetActive(true);
        }

        public void OnCloseInstructionsClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
            if (instructionsPanel != null) instructionsPanel.SetActive(false);
        }

        public void OnExitClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
