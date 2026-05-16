using System.Collections.Generic;
using UnityEngine;

namespace WDG
{
    public class EnemyView : MonoBehaviour
    {
        [SerializeField] private List<GameObject> enemyPrefabs;
        [SerializeField] private float interpolationSpeed = 10f;

        private const float CellSize = 1f;
        private Dictionary<int, GameObject> _enemyObjects = new Dictionary<int, GameObject>();
        private Dictionary<int, EnemyInstance> _trackedEnemies = new Dictionary<int, EnemyInstance>();

        private void OnEnable()
        {
            EventBus.Subscribe<EnemySpawnedEvent>(OnEnemySpawned);
            EventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Subscribe<EnemyReachedCoreEvent>(OnEnemyReachedCore);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemySpawnedEvent>(OnEnemySpawned);
            EventBus.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Unsubscribe<EnemyReachedCoreEvent>(OnEnemyReachedCore);
        }

        private void Update()
        {
            var enemySystem = ServiceLocator.Get<EnemySystem>();
            var activeEnemies = enemySystem.GetActiveEnemies();

            // Build a set of active instance IDs for quick lookup
            var activeIds = new HashSet<int>();
            foreach (var enemy in activeEnemies)
            {
                activeIds.Add(enemy.InstanceId);
            }

            // Update positions for tracked enemies
            var toRemove = new List<int>();
            foreach (var kvp in _trackedEnemies)
            {
                int instanceId = kvp.Key;

                if (!activeIds.Contains(instanceId))
                {
                    toRemove.Add(instanceId);
                    continue;
                }

                if (!_enemyObjects.ContainsKey(instanceId))
                    continue;

                var enemy = kvp.Value;
                var go = _enemyObjects[instanceId];
                if (go == null)
                    continue;

                Vector3 targetPos = EnemyWorldToScene(enemy.WorldPosition);
                go.transform.position = Vector3.Lerp(
                    go.transform.position,
                    targetPos,
                    Time.deltaTime * interpolationSpeed
                );
            }

            // Clean up enemies that are no longer active
            foreach (int id in toRemove)
            {
                RemoveEnemy(id);
            }
        }

        private void OnEnemySpawned(EnemySpawnedEvent evt)
        {
            var enemySystem = ServiceLocator.Get<EnemySystem>();
            var activeEnemies = enemySystem.GetActiveEnemies();

            EnemyInstance spawnedEnemy = null;
            foreach (var enemy in activeEnemies)
            {
                if (enemy.InstanceId == evt.InstanceId)
                {
                    spawnedEnemy = enemy;
                    break;
                }
            }

            if (spawnedEnemy == null)
                return;

            Vector3 worldPos = EnemyWorldToScene(evt.SpawnPosition);
            GameObject prefab = GetEnemyPrefab(evt.EnemyId);

            GameObject enemyGO;
            if (prefab != null)
            {
                enemyGO = Instantiate(prefab, worldPos, Quaternion.identity, transform);
            }
            else
            {
                enemyGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                enemyGO.transform.SetParent(transform);
                enemyGO.transform.position = worldPos;
                enemyGO.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            }

            enemyGO.name = $"Enemy_{evt.InstanceId}";
            _enemyObjects[evt.InstanceId] = enemyGO;
            _trackedEnemies[evt.InstanceId] = spawnedEnemy;
        }

        private void OnEnemyKilled(EnemyKilledEvent evt)
        {
            RemoveEnemy(evt.InstanceId);
        }

        private void OnEnemyReachedCore(EnemyReachedCoreEvent evt)
        {
            RemoveEnemy(evt.InstanceId);
        }

        private void RemoveEnemy(int instanceId)
        {
            if (_enemyObjects.TryGetValue(instanceId, out var go))
            {
                if (go != null)
                {
                    Destroy(go);
                }
                _enemyObjects.Remove(instanceId);
            }
            _trackedEnemies.Remove(instanceId);
        }

        private GameObject GetEnemyPrefab(string enemyId)
        {
            if (enemyPrefabs == null)
                return null;

            foreach (var prefab in enemyPrefabs)
            {
                if (prefab != null && prefab.name.Contains(enemyId))
                {
                    return prefab;
                }
            }

            return enemyPrefabs.Count > 0 ? enemyPrefabs[0] : null;
        }

        private Vector3 EnemyWorldToScene(Vector2 enemyWorldPos)
        {
            return new Vector3(
                enemyWorldPos.x * CellSize + CellSize / 2f,
                0.3f,
                enemyWorldPos.y * CellSize + CellSize / 2f
            );
        }
    }
}
