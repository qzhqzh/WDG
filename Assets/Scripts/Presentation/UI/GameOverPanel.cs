using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace WDG
{
    public class GameOverPanel : MonoBehaviour
    {
        [SerializeField] private Text resultText;
        [SerializeField] private Text statsText;
        [SerializeField] private Button restartButton;
        [SerializeField] private GameObject panelRoot;

        private void OnEnable()
        {
            EventBus.Subscribe<GameOverEvent>(OnGameOver);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
        }

        private void Start()
        {
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartClicked);
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        private void OnGameOver(GameOverEvent evt)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            if (resultText != null)
            {
                resultText.text = evt.IsVictory ? "Victory!" : "Defeat";
            }

            if (statsText != null)
            {
                var gameCore = ServiceLocator.Get<GameCore>();
                string stats = "";

                if (gameCore != null)
                {
                    stats += $"Waves Survived: {gameCore.CurrentWaveIndex}\n";
                    stats += $"Core Health: {gameCore.CoreHealth}/{gameCore.MaxCoreHealth}";
                }

                statsText.text = stats;
            }
        }

        private void OnRestartClicked()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
