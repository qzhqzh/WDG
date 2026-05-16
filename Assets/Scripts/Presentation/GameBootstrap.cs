using System.Collections.Generic;
using UnityEngine;

namespace WDG
{
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Config Assets")]
        [SerializeField] private MapConfig mapConfig;
        [SerializeField] private List<BuildingConfig> buildingConfigs;
        [SerializeField] private List<EnemyConfig> enemyConfigs;
        [SerializeField] private List<WaveConfig> waveConfigs;
        [SerializeField] private RewardPoolConfig rewardPoolConfig;
        [SerializeField] private GameBalanceConfig gameBalanceConfig;

        [Header("Prefab References")]
        [SerializeField] private GameObject cellPrefab;
        [SerializeField] private List<GameObject> buildingPrefabs;
        [SerializeField] private List<GameObject> enemyPrefabs;

        private GameCore _gameCore;
        private string _selectedBuildingConfigId;
        private bool _isPlacementMode;

        public GameObject CellPrefab => cellPrefab;
        public List<GameObject> BuildingPrefabs => buildingPrefabs;
        public List<GameObject> EnemyPrefabs => enemyPrefabs;

        private void Awake()
        {
            // Create and register ConfigSystem
            var configSystem = new ConfigSystem();
            configSystem.SetMapConfig(mapConfig);
            configSystem.SetRewardPoolConfig(rewardPoolConfig);
            configSystem.SetGameBalanceConfig(gameBalanceConfig);

            if (buildingConfigs != null)
            {
                foreach (var config in buildingConfigs)
                {
                    configSystem.RegisterBuildingConfig(config);
                }
            }

            if (enemyConfigs != null)
            {
                foreach (var config in enemyConfigs)
                {
                    configSystem.RegisterEnemyConfig(config);
                }
            }

            if (waveConfigs != null)
            {
                foreach (var config in waveConfigs)
                {
                    configSystem.RegisterWaveConfig(config);
                }
            }

            ServiceLocator.Register<ConfigSystem>(configSystem);

            // Create and register GameCore
            _gameCore = new GameCore();
            ServiceLocator.Register<GameCore>(_gameCore);

            // Initialize GameCore (which creates and registers all sub-systems)
            _gameCore.Initialize();
        }

        private void Update()
        {
            if (_gameCore != null)
            {
                _gameCore.Tick(Time.deltaTime);
            }
        }

        private void OnDestroy()
        {
            if (_gameCore != null)
            {
                _gameCore.Dispose();
            }
            ServiceLocator.Clear();
            EventBus.Clear();
        }

        public void OnStartWaveClicked()
        {
            if (_gameCore != null && _gameCore.CurrentState == GameState.Preparation)
            {
                _gameCore.StartNextWave();
            }
        }

        public void OnBuildingSelected(string configId)
        {
            _selectedBuildingConfigId = configId;
            _isPlacementMode = true;
        }

        public void OnGridCellClicked(Vector2Int pos)
        {
            if (!_isPlacementMode || string.IsNullOrEmpty(_selectedBuildingConfigId))
                return;

            var buildingSystem = ServiceLocator.Get<BuildingSystem>();
            if (buildingSystem.CanPlace(_selectedBuildingConfigId, pos))
            {
                buildingSystem.Place(_selectedBuildingConfigId, pos);
                _isPlacementMode = false;
                _selectedBuildingConfigId = null;
            }
        }

        public void OnRewardSelected(RewardCard card)
        {
            if (_gameCore == null || _gameCore.CurrentState != GameState.RewardSelection)
                return;

            var rewardSystem = ServiceLocator.Get<RewardSystem>();
            rewardSystem.ApplyReward(card);
            _gameCore.TransitionTo(GameState.Preparation);
        }

        public void CancelPlacement()
        {
            _isPlacementMode = false;
            _selectedBuildingConfigId = null;
        }

        public bool IsPlacementMode => _isPlacementMode;
        public string SelectedBuildingConfigId => _selectedBuildingConfigId;
    }
}
