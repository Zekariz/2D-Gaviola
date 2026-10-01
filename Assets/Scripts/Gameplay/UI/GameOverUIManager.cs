using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace YourGame.Gameplay.UI
{
    public class GameOverUIManager : MonoBehaviour
    {
        public static GameOverUIManager Instance { get; private set; }

        [Header("UI References")]
        public GameObject gameOverPanel;
        public Button retryButton;
        public Button exitButton;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            // Hide by default
            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);

            // Hook up buttons
            if (retryButton != null)
                retryButton.onClick.AddListener(OnRetryClicked);
                
            if (exitButton != null)
                exitButton.onClick.AddListener(OnExitClicked);
        }

        public void Show()
        {
            if (gameOverPanel != null)
                gameOverPanel.SetActive(true);
                
            // Pause the game
            Time.timeScale = 0f;
        }

        private void OnRetryClicked()
        {
            // Unpause the game before reloading
            Time.timeScale = 1f;
            
            // Reload the current scene from the very beginning
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void OnExitClicked()
        {
            // Quit the game
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
