using UnityEngine;
using UnityEngine.UI;

namespace WDG
{
    public class WaveInfoPanel : MonoBehaviour
    {
        [SerializeField] private Text waveInfoText;
        [SerializeField] private Button startWaveButton;
        [SerializeField] private GameObject panelRoot;

        private GameBootstrap _bootstrap;

        private void OnEnable()
        {
            EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void Start()
        {
            _bootstrap = FindObjectOfType<GameBootstrap>();

            if (startWaveButton != null)
            {
                startWaveButton.onClick.AddListener(OnStartWaveClicked);
            }

            UpdateVisibility();
            UpdateWaveInfo();
        }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            UpdateVisibility(evt.NewState);
            if (evt.NewState == GameState.Preparation)
            {
                UpdateWaveInfo();
            }
        }

        private void OnStartWaveClicked()
        {
            if (_bootstrap != null)
            {
                _bootstrap.OnStartWaveClicked();
            }
        }

        private void UpdateVisibility(GameState state = GameState.Initializing)
        {
            if (panelRoot == null)
                return;

            if (state == GameState.Initializing)
            {
                var gameCore = ServiceLocator.Get<GameCore>();
                if (gameCore != null)
                {
                    state = gameCore.CurrentState;
                }
            }

            panelRoot.SetActive(state == GameState.Preparation);
        }

        private void UpdateWaveInfo()
        {
            if (waveInfoText == null)
                return;

            var gameCore = ServiceLocator.Get<GameCore>();
            var configSystem = ServiceLocator.Get<ConfigSystem>();

            if (gameCore == null || configSystem == null)
                return;

            int nextWaveIndex = gameCore.CurrentWaveIndex + 1;
            int totalWaves = configSystem.WaveConfigs.Count;

            WaveConfig nextWave = null;
            foreach (var config in configSystem.WaveConfigs)
            {
                if (config.waveIndex == nextWaveIndex)
                {
                    nextWave = config;
                    break;
                }
            }

            if (nextWave != null)
            {
                string info = $"Next Wave: {nextWaveIndex}/{totalWaves}";
                if (nextWave.isBossWave)
                {
                    info += " [BOSS]";
                }

                if (nextWave.spawnGroups != null)
                {
                    int totalEnemies = 0;
                    foreach (var group in nextWave.spawnGroups)
                    {
                        totalEnemies += group.count;
                    }
                    info += $"\nEnemies: {totalEnemies}";
                }

                waveInfoText.text = info;
            }
            else
            {
                waveInfoText.text = $"Next Wave: {nextWaveIndex}/{totalWaves}";
            }
        }
    }
}
