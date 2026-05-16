using System.Collections.Generic;
using UnityEngine;

namespace WDG
{
    public class EnemySystem : IGameSystem
    {
        private readonly List<EnemyInstance> _activeEnemies = new List<EnemyInstance>();
        private readonly Dictionary<Vector2Int, List<Vector2Int>> _pathCache = new Dictionary<Vector2Int, List<Vector2Int>>();

        public int ActiveEnemyCount => _activeEnemies.Count;

        public void Initialize()
        {
            EventBus.Subscribe<BuildingPlacedEvent>(OnBuildingPlaced);
            EventBus.Subscribe<BuildingRemovedEvent>(OnBuildingRemoved);
        }

        public void Tick(float deltaTime)
        {
            for (int i = _activeEnemies.Count - 1; i >= 0; i--)
            {
                var enemy = _activeEnemies[i];
                MoveEnemy(enemy, deltaTime);

                if (HasReachedCore(enemy))
                {
                    var gameCore = ServiceLocator.Get<GameCore>();
                    gameCore.ApplyCoreHealthDamage(enemy.CoreDamage);

                    EventBus.Publish(new EnemyReachedCoreEvent
                    {
                        InstanceId = enemy.InstanceId,
                        EnemyId = enemy.ConfigId,
                        Damage = enemy.CoreDamage
                    });

                    _activeEnemies.RemoveAt(i);
                }
            }
        }

        public void Dispose()
        {
            _activeEnemies.Clear();
            _pathCache.Clear();
            EventBus.Unsubscribe<BuildingPlacedEvent>(OnBuildingPlaced);
            EventBus.Unsubscribe<BuildingRemovedEvent>(OnBuildingRemoved);
            EnemyInstance.ResetIdCounter();
        }

        public EnemyInstance SpawnEnemy(EnemyConfig config, Vector2Int spawnPoint)
        {
            var grid = ServiceLocator.Get<GridMapSystem>();

            var instance = new EnemyInstance(
                config.enemyId,
                config.maxHealth,
                config.moveSpeed,
                config.coreDamage,
                config.resourceDrop
            );

            // Use cached path if available; otherwise compute and cache
            List<Vector2Int> path;
            if (_pathCache.ContainsKey(spawnPoint))
            {
                path = _pathCache[spawnPoint];
            }
            else
            {
                var pathfinding = ServiceLocator.Get<PathfindingModule>();
                var corePoint = grid.GetCorePoint();
                path = pathfinding.FindPath(spawnPoint, corePoint, grid);
                _pathCache[spawnPoint] = path;
            }

            instance.Path = new List<Vector2Int>(path);
            instance.PathIndex = 0;

            if (instance.Path.Count > 0)
            {
                instance.WorldPosition = new Vector2(instance.Path[0].x, instance.Path[0].y);
            }
            else
            {
                instance.WorldPosition = new Vector2(spawnPoint.x, spawnPoint.y);
            }

            _activeEnemies.Add(instance);

            EventBus.Publish(new EnemySpawnedEvent
            {
                EnemyId = config.enemyId,
                InstanceId = instance.InstanceId,
                SpawnPosition = instance.WorldPosition
            });

            return instance;
        }

        public void RemoveEnemy(string id)
        {
            for (int i = _activeEnemies.Count - 1; i >= 0; i--)
            {
                if (_activeEnemies[i].Id == id)
                {
                    _activeEnemies.RemoveAt(i);
                    return;
                }
            }
        }

        public List<EnemyInstance> GetActiveEnemies()
        {
            return new List<EnemyInstance>(_activeEnemies);
        }

        /// <summary>
        /// Clears the cached paths. Should be called when grid state changes
        /// (e.g., buildings placed or removed).
        /// </summary>
        public void ClearPathCache()
        {
            _pathCache.Clear();
        }

        private void OnBuildingPlaced(BuildingPlacedEvent evt)
        {
            ClearPathCache();
        }

        private void OnBuildingRemoved(BuildingRemovedEvent evt)
        {
            ClearPathCache();
        }

        private void MoveEnemy(EnemyInstance enemy, float deltaTime)
        {
            if (enemy.Path == null || enemy.Path.Count == 0)
                return;

            if (enemy.PathIndex >= enemy.Path.Count - 1)
                return;

            var currentTarget = enemy.Path[enemy.PathIndex + 1];
            var targetPos = new Vector2(currentTarget.x, currentTarget.y);
            var direction = targetPos - enemy.WorldPosition;
            float distance = direction.magnitude;

            float moveAmount = enemy.MoveSpeed * deltaTime;

            if (moveAmount >= distance)
            {
                enemy.WorldPosition = targetPos;
                enemy.PathIndex++;

                // Continue moving if there's remaining movement and more path
                float remaining = moveAmount - distance;
                while (remaining > 0 && enemy.PathIndex < enemy.Path.Count - 1)
                {
                    var nextTarget = enemy.Path[enemy.PathIndex + 1];
                    var nextPos = new Vector2(nextTarget.x, nextTarget.y);
                    var nextDir = nextPos - enemy.WorldPosition;
                    float nextDist = nextDir.magnitude;

                    if (remaining >= nextDist)
                    {
                        enemy.WorldPosition = nextPos;
                        enemy.PathIndex++;
                        remaining -= nextDist;
                    }
                    else
                    {
                        enemy.WorldPosition += nextDir.normalized * remaining;
                        remaining = 0;
                    }
                }
            }
            else
            {
                enemy.WorldPosition += direction.normalized * moveAmount;
            }
        }

        private bool HasReachedCore(EnemyInstance enemy)
        {
            if (enemy.Path == null || enemy.Path.Count == 0)
                return false;
            return enemy.PathIndex >= enemy.Path.Count - 1;
        }
    }
}
