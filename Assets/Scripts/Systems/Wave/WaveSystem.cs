using System.Collections.Generic;
using UnityEngine;

namespace WDG
{
    public class WaveSystem : IGameSystem
    {
        public int CurrentWaveIndex { get; private set; }
        public bool IsWaveActive { get; private set; }
        public int TotalWaves { get; set; }

        private WaveConfig _currentConfig;
        private List<SpawnGroupState> _groupStates;

        public void Initialize()
        {
            // No-op
        }

        public void Tick(float deltaTime)
        {
            if (!IsWaveActive || _groupStates == null)
                return;

            var enemySystem = ServiceLocator.Get<EnemySystem>();
            var grid = ServiceLocator.Get<GridMapSystem>();
            var spawnPoints = grid.GetSpawnPoints();

            bool allGroupsExhausted = true;

            for (int i = 0; i < _groupStates.Count; i++)
            {
                var state = _groupStates[i];
                var group = _currentConfig.spawnGroups[i];

                if (state.SpawnedCount >= group.count)
                    continue;

                allGroupsExhausted = false;

                // Wait for group delay
                if (state.GroupDelayElapsed < group.groupDelay)
                {
                    state.GroupDelayElapsed += deltaTime;
                    continue;
                }

                // Spawn timer
                state.ElapsedTime += deltaTime;

                if (state.ElapsedTime >= group.spawnInterval)
                {
                    state.ElapsedTime -= group.spawnInterval;
                    state.SpawnedCount++;

                    int spawnIndex = group.spawnPointIndex;
                    Vector2Int spawnPoint;
                    if (spawnIndex >= 0 && spawnIndex < spawnPoints.Count)
                    {
                        spawnPoint = spawnPoints[spawnIndex];
                    }
                    else
                    {
                        spawnPoint = spawnPoints[0];
                    }

                    enemySystem.SpawnEnemy(group.enemyConfig, spawnPoint);
                }
            }

            // Check if all groups are exhausted
            if (!allGroupsExhausted)
                return;

            // Verify all groups finished
            for (int i = 0; i < _groupStates.Count; i++)
            {
                if (_groupStates[i].SpawnedCount < _currentConfig.spawnGroups[i].count)
                    return;
            }

            // All spawned, check if all enemies are dead
            if (enemySystem.ActiveEnemyCount == 0)
            {
                IsWaveActive = false;
                EventBus.Publish(new WaveCompletedEvent
                {
                    WaveIndex = CurrentWaveIndex,
                    BonusReward = _currentConfig.completionBonus
                });
            }
        }

        public void Dispose()
        {
            _groupStates = null;
            _currentConfig = null;
        }

        public void StartWave(WaveConfig config)
        {
            _currentConfig = config;
            CurrentWaveIndex = config.waveIndex;
            IsWaveActive = true;

            _groupStates = new List<SpawnGroupState>();
            for (int i = 0; i < config.spawnGroups.Count; i++)
            {
                _groupStates.Add(new SpawnGroupState());
            }

            EventBus.Publish(new WaveStartedEvent
            {
                WaveIndex = CurrentWaveIndex
            });
        }

        private class SpawnGroupState
        {
            public int SpawnedCount;
            public float ElapsedTime;
            public float GroupDelayElapsed;
        }
    }
}
