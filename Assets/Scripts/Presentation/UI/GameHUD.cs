using UnityEngine;
using UnityEngine.UI;

namespace WDG
{
    public class GameHUD : MonoBehaviour
    {
        [SerializeField] private Text healthText;
        [SerializeField] private Text waveText;
        [SerializeField] private Text resourceText;
        [SerializeField] private Text stateText;

        private GameCore _gameCore;

        private void OnEnable()
        {
            EventBus.Subscribe<ResourceChangedEvent>(OnResourceChanged);
            EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
            EventBus.Subscribe<WaveStartedEvent>(OnWaveStarted);
            EventBus.Subscribe<EnemyReachedCoreEvent>(OnEnemyReachedCore);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ResourceChangedEvent>(OnResourceChanged);
            EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
            EventBus.Unsubscribe<WaveStartedEvent>(OnWaveStarted);
            EventBus.Unsubscribe<EnemyReachedCoreEvent>(OnEnemyReachedCore);
        }

        private void Start()
        {
            _gameCore = ServiceLocator.Get<GameCore>();
            UpdateHealth();
            UpdateWave();
            UpdateResource();
            UpdateState();
        }

        private void OnResourceChanged(ResourceChangedEvent evt)
        {
            UpdateResource(evt.NewAmount);
        }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            UpdateState(evt.NewState);
        }

        private void OnWaveStarted(WaveStartedEvent evt)
        {
            UpdateWave(evt.WaveIndex);
        }

        private void OnEnemyReachedCore(EnemyReachedCoreEvent evt)
        {
            UpdateHealth();
        }

        private void UpdateHealth()
        {
            if (healthText == null || _gameCore == null)
                return;

            healthText.text = $"HP: {_gameCore.CoreHealth}/{_gameCore.MaxCoreHealth}";
        }

        private void UpdateWave(int currentWave = 0)
        {
            if (waveText == null)
                return;

            if (_gameCore == null)
            {
                waveText.text = "Wave: 0/0";
                return;
            }

            int total = 0;
            var configSystem = ServiceLocator.Get<ConfigSystem>();
            if (configSystem != null && configSystem.WaveConfigs != null)
            {
                total = configSystem.WaveConfigs.Count;
            }

            int wave = currentWave > 0 ? currentWave : _gameCore.CurrentWaveIndex;
            waveText.text = $"Wave: {wave}/{total}";
        }

        private void UpdateResource(int amount = -1)
        {
            if (resourceText == null)
                return;

            if (amount < 0)
            {
                var resourceSystem = ServiceLocator.Get<ResourceSystem>();
                if (resourceSystem != null)
                {
                    amount = resourceSystem.CurrentGold;
                }
                else
                {
                    amount = 0;
                }
            }

            resourceText.text = $"Gold: {amount}";
        }

        private void UpdateState(GameState state = GameState.Initializing)
        {
            if (stateText == null)
                return;

            if (_gameCore != null && state == GameState.Initializing)
            {
                state = _gameCore.CurrentState;
            }

            stateText.text = state.ToString();
        }
    }
}
