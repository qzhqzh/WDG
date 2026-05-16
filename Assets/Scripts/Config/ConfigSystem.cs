using System.Collections.Generic;
using UnityEngine;

namespace WDG
{
    public class ConfigSystem
    {
        private readonly List<BuildingConfig> _buildingConfigs = new List<BuildingConfig>();
        private readonly List<EnemyConfig> _enemyConfigs = new List<EnemyConfig>();
        private readonly List<WaveConfig> _waveConfigs = new List<WaveConfig>();
        private MapConfig _mapConfig;
        private RewardPoolConfig _rewardPoolConfig;
        private GameBalanceConfig _gameBalanceConfig;

        public IReadOnlyList<BuildingConfig> BuildingConfigs => _buildingConfigs;
        public IReadOnlyList<EnemyConfig> EnemyConfigs => _enemyConfigs;
        public IReadOnlyList<WaveConfig> WaveConfigs => _waveConfigs;
        public MapConfig MapConfig => _mapConfig;
        public RewardPoolConfig RewardPoolConfig => _rewardPoolConfig;
        public GameBalanceConfig GameBalanceConfig => _gameBalanceConfig;

        public void RegisterBuildingConfig(BuildingConfig config)
        {
            _buildingConfigs.Add(config);
        }

        public void RegisterEnemyConfig(EnemyConfig config)
        {
            _enemyConfigs.Add(config);
        }

        public void RegisterWaveConfig(WaveConfig config)
        {
            _waveConfigs.Add(config);
        }

        public void SetMapConfig(MapConfig config)
        {
            _mapConfig = config;
        }

        public void SetRewardPoolConfig(RewardPoolConfig config)
        {
            _rewardPoolConfig = config;
        }

        public void SetGameBalanceConfig(GameBalanceConfig config)
        {
            _gameBalanceConfig = config;
        }

        public bool Validate()
        {
            bool isValid = true;

            if (_mapConfig == null)
            {
                Debug.LogError("[ConfigSystem] MapConfig is not set.");
                isValid = false;
            }
            else
            {
                if (_mapConfig.width <= 0 || _mapConfig.height <= 0)
                {
                    Debug.LogError("[ConfigSystem] MapConfig has invalid dimensions.");
                    isValid = false;
                }
                if (_mapConfig.spawnPoints == null || _mapConfig.spawnPoints.Count == 0)
                {
                    Debug.LogError("[ConfigSystem] MapConfig has no spawn points.");
                    isValid = false;
                }
                if (_mapConfig.coreMaxHealth <= 0)
                {
                    Debug.LogError("[ConfigSystem] MapConfig coreMaxHealth must be greater than 0.");
                    isValid = false;
                }
            }

            if (_gameBalanceConfig == null)
            {
                Debug.LogError("[ConfigSystem] GameBalanceConfig is not set.");
                isValid = false;
            }

            if (_rewardPoolConfig == null)
            {
                Debug.LogError("[ConfigSystem] RewardPoolConfig is not set.");
                isValid = false;
            }

            foreach (var building in _buildingConfigs)
            {
                if (string.IsNullOrEmpty(building.buildingId))
                {
                    Debug.LogError($"[ConfigSystem] BuildingConfig has empty buildingId: {building.name}");
                    isValid = false;
                }
                if (building.cost < 0)
                {
                    Debug.LogError($"[ConfigSystem] BuildingConfig '{building.buildingId}' has negative cost.");
                    isValid = false;
                }
            }

            foreach (var enemy in _enemyConfigs)
            {
                if (string.IsNullOrEmpty(enemy.enemyId))
                {
                    Debug.LogError($"[ConfigSystem] EnemyConfig has empty enemyId: {enemy.name}");
                    isValid = false;
                }
                if (enemy.maxHealth <= 0)
                {
                    Debug.LogError($"[ConfigSystem] EnemyConfig '{enemy.enemyId}' has invalid maxHealth.");
                    isValid = false;
                }
                if (enemy.moveSpeed <= 0)
                {
                    Debug.LogError($"[ConfigSystem] EnemyConfig '{enemy.enemyId}' has invalid moveSpeed.");
                    isValid = false;
                }
            }

            foreach (var wave in _waveConfigs)
            {
                if (wave.spawnGroups == null || wave.spawnGroups.Count == 0)
                {
                    Debug.LogError($"[ConfigSystem] WaveConfig index {wave.waveIndex} has no spawn groups.");
                    isValid = false;
                }
                else
                {
                    foreach (var group in wave.spawnGroups)
                    {
                        if (group.enemyConfig == null)
                        {
                            Debug.LogError($"[ConfigSystem] WaveConfig index {wave.waveIndex} has a spawn group with null enemyConfig.");
                            isValid = false;
                        }
                        if (group.count <= 0)
                        {
                            Debug.LogError($"[ConfigSystem] WaveConfig index {wave.waveIndex} has a spawn group with invalid count.");
                            isValid = false;
                        }
                    }
                }
            }

            return isValid;
        }
    }
}
