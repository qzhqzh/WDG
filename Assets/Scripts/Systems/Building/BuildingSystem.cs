using System.Collections.Generic;
using UnityEngine;

namespace WDG
{
    public class BuildingSystem : IGameSystem
    {
        private readonly List<BuildingInstance> _buildings = new List<BuildingInstance>();
        private readonly List<string> _unlockedBuildingIds = new List<string>();
        private readonly Dictionary<string, float> _damageMultipliers = new Dictionary<string, float>();

        public void Initialize()
        {
            var configSystem = ServiceLocator.Get<ConfigSystem>();
            foreach (var building in configSystem.BuildingConfigs)
            {
                if (!_unlockedBuildingIds.Contains(building.buildingId))
                {
                    _unlockedBuildingIds.Add(building.buildingId);
                }
            }
        }

        public void Tick(float deltaTime)
        {
            // No-op: combat handles attack timing
        }

        public void Dispose()
        {
            _buildings.Clear();
            _unlockedBuildingIds.Clear();
            _damageMultipliers.Clear();
        }

        public bool CanPlace(string configId, Vector2Int pos)
        {
            var grid = ServiceLocator.Get<GridMapSystem>();
            var pathfinding = ServiceLocator.Get<PathfindingModule>();
            var resourceSystem = ServiceLocator.Get<ResourceSystem>();
            var configSystem = ServiceLocator.Get<ConfigSystem>();

            if (!grid.IsInBounds(pos))
                return false;

            if (!grid.IsBuildable(pos))
                return false;

            BuildingConfig config = FindBuildingConfig(configId, configSystem);
            if (config == null)
                return false;

            if (!resourceSystem.CanAfford(config.cost))
                return false;

            // Temporarily mark occupied to check path validity
            grid.SetCellState(pos, CellState.Occupied);

            bool pathValid = true;
            var spawnPoints = grid.GetSpawnPoints();
            var corePoint = grid.GetCorePoint();

            foreach (var spawn in spawnPoints)
            {
                if (!pathfinding.HasValidPath(spawn, corePoint, grid))
                {
                    pathValid = false;
                    break;
                }
            }

            // Revert the cell state
            grid.SetCellState(pos, CellState.Empty);

            return pathValid;
        }

        public BuildingInstance Place(string configId, Vector2Int pos)
        {
            var grid = ServiceLocator.Get<GridMapSystem>();
            var resourceSystem = ServiceLocator.Get<ResourceSystem>();
            var configSystem = ServiceLocator.Get<ConfigSystem>();

            BuildingConfig config = FindBuildingConfig(configId, configSystem);
            if (config == null)
                return null;

            float damage = config.attackDamage;

            // Apply accumulated damage multiplier for this building type
            if (_damageMultipliers.ContainsKey(configId))
            {
                damage *= _damageMultipliers[configId];
            }

            var instance = new BuildingInstance(
                configId,
                pos,
                damage,
                config.attackRange,
                config.attackInterval,
                config.targetingStrategy
            );

            _buildings.Add(instance);
            grid.SetCellState(pos, CellState.Occupied, instance.Id);
            resourceSystem.TrySpend(config.cost);

            EventBus.Publish(new BuildingPlacedEvent
            {
                BuildingId = instance.Id,
                Position = pos
            });

            return instance;
        }

        public void Remove(string buildingId)
        {
            var grid = ServiceLocator.Get<GridMapSystem>();

            BuildingInstance target = null;
            foreach (var building in _buildings)
            {
                if (building.Id == buildingId)
                {
                    target = building;
                    break;
                }
            }

            if (target == null)
                return;

            _buildings.Remove(target);
            grid.SetCellState(target.GridPosition, CellState.Empty);

            EventBus.Publish(new BuildingRemovedEvent
            {
                BuildingId = buildingId,
                Position = target.GridPosition
            });
        }

        public List<BuildingInstance> GetAllBuildings()
        {
            return new List<BuildingInstance>(_buildings);
        }

        public List<BuildingConfig> GetAvailableBuildings()
        {
            var configSystem = ServiceLocator.Get<ConfigSystem>();
            var available = new List<BuildingConfig>();
            foreach (var config in configSystem.BuildingConfigs)
            {
                if (_unlockedBuildingIds.Contains(config.buildingId))
                {
                    available.Add(config);
                }
            }
            return available;
        }

        public void UnlockBuilding(string configId)
        {
            if (!_unlockedBuildingIds.Contains(configId))
            {
                _unlockedBuildingIds.Add(configId);
            }
        }

        /// <summary>
        /// Sets/accumulates a damage multiplier for a building type.
        /// Future placements of this configId will have their damage multiplied.
        /// </summary>
        public void SetDamageMultiplier(string configId, float multiplier)
        {
            if (_damageMultipliers.ContainsKey(configId))
            {
                _damageMultipliers[configId] *= multiplier;
            }
            else
            {
                _damageMultipliers[configId] = multiplier;
            }
        }

        public float GetDamageMultiplier(string configId)
        {
            if (_damageMultipliers.ContainsKey(configId))
                return _damageMultipliers[configId];
            return 1f;
        }

        private BuildingConfig FindBuildingConfig(string configId, ConfigSystem configSystem)
        {
            foreach (var config in configSystem.BuildingConfigs)
            {
                if (config.buildingId == configId)
                    return config;
            }
            return null;
        }
    }
}
