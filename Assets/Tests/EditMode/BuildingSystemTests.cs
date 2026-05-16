using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace WDG.Tests
{
    [TestFixture]
    public class BuildingSystemTests
    {
        private BuildingSystem _buildingSystem;
        private GridMapSystem _gridMap;
        private PathfindingModule _pathfinding;
        private ResourceSystem _resourceSystem;
        private ConfigSystem _configSystem;
        private BuildingConfig _towerConfig;

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Clear();
            EventBus.Clear();

            _configSystem = new ConfigSystem();
            ServiceLocator.Register<ConfigSystem>(_configSystem);

            _gridMap = new GridMapSystem();
            ServiceLocator.Register<GridMapSystem>(_gridMap);

            _pathfinding = new PathfindingModule();
            ServiceLocator.Register<PathfindingModule>(_pathfinding);

            _resourceSystem = new ResourceSystem();
            ServiceLocator.Register<ResourceSystem>(_resourceSystem);
            _resourceSystem.Initialize(200);

            // Create a building config
            _towerConfig = ScriptableObject.CreateInstance<BuildingConfig>();
            _towerConfig.buildingId = "basic_tower";
            _towerConfig.displayName = "Basic Tower";
            _towerConfig.cost = 50;
            _towerConfig.attackDamage = 10f;
            _towerConfig.attackRange = 3f;
            _towerConfig.attackInterval = 1f;
            _towerConfig.targetingStrategy = TargetingStrategy.Nearest;
            _configSystem.RegisterBuildingConfig(_towerConfig);

            // Create a map with spawn and core
            var mapConfig = ScriptableObject.CreateInstance<MapConfig>();
            mapConfig.width = 10;
            mapConfig.height = 10;
            mapConfig.blockedCells = new List<Vector2Int>();
            mapConfig.spawnPoints = new List<Vector2Int> { new Vector2Int(0, 0) };
            mapConfig.corePoint = new Vector2Int(9, 9);
            mapConfig.startingGold = 200;
            mapConfig.coreMaxHealth = 20;
            _configSystem.SetMapConfig(mapConfig);
            _gridMap.LoadFromConfig(mapConfig);

            _buildingSystem = new BuildingSystem();
            ServiceLocator.Register<BuildingSystem>(_buildingSystem);
            _buildingSystem.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            ServiceLocator.Clear();
            EventBus.Clear();
        }

        [Test]
        public void CanPlace_EmptyBuildableCell_ReturnsTrue()
        {
            // (5, 5) is empty and placing there won't block the path
            Assert.IsTrue(_buildingSystem.CanPlace("basic_tower", new Vector2Int(5, 5)));
        }

        [Test]
        public void CanPlace_BlockedCell_ReturnsFalse()
        {
            _gridMap.SetCellState(new Vector2Int(3, 3), CellState.Blocked);

            Assert.IsFalse(_buildingSystem.CanPlace("basic_tower", new Vector2Int(3, 3)));
        }

        [Test]
        public void CanPlace_OccupiedCell_ReturnsFalse()
        {
            _gridMap.SetCellState(new Vector2Int(4, 4), CellState.Occupied, "existing_building");

            Assert.IsFalse(_buildingSystem.CanPlace("basic_tower", new Vector2Int(4, 4)));
        }

        [Test]
        public void CanPlace_OutOfBounds_ReturnsFalse()
        {
            Assert.IsFalse(_buildingSystem.CanPlace("basic_tower", new Vector2Int(-1, 0)));
            Assert.IsFalse(_buildingSystem.CanPlace("basic_tower", new Vector2Int(10, 10)));
        }

        [Test]
        public void CanPlace_InsufficientResources_ReturnsFalse()
        {
            // Spend most resources so we can't afford the tower
            _resourceSystem.TrySpend(180); // Left with 20, tower costs 50

            Assert.IsFalse(_buildingSystem.CanPlace("basic_tower", new Vector2Int(5, 5)));
        }

        [Test]
        public void CanPlace_WouldBlockPath_ReturnsFalse()
        {
            // Block cells to create a narrow corridor that the building would block
            // Create a wall leaving only one path through (1, y) for y = 0..9
            for (int y = 0; y < 10; y++)
            {
                if (y != 5) // Leave only (1, 5) open
                {
                    _gridMap.SetCellState(new Vector2Int(1, y), CellState.Blocked);
                }
            }

            // Placing at (1, 5) would block the only path from spawn (0,0) to core (9,9)
            Assert.IsFalse(_buildingSystem.CanPlace("basic_tower", new Vector2Int(1, 5)));
        }

        [Test]
        public void Place_Success_UpdatesGrid()
        {
            var pos = new Vector2Int(5, 5);
            _buildingSystem.Place("basic_tower", pos);

            var cell = _gridMap.GetCell(pos);
            Assert.AreEqual(CellState.Occupied, cell.State);
            Assert.IsNotNull(cell.OccupantId);
        }

        [Test]
        public void Place_Success_SpendsResources()
        {
            int initialGold = _resourceSystem.CurrentGold;
            _buildingSystem.Place("basic_tower", new Vector2Int(5, 5));

            Assert.AreEqual(initialGold - _towerConfig.cost, _resourceSystem.CurrentGold);
        }

        [Test]
        public void Place_PublishesBuildingPlacedEvent()
        {
            BuildingPlacedEvent receivedEvent = default;
            bool eventReceived = false;
            EventBus.Subscribe<BuildingPlacedEvent>(e =>
            {
                receivedEvent = e;
                eventReceived = true;
            });

            var pos = new Vector2Int(5, 5);
            _buildingSystem.Place("basic_tower", pos);

            Assert.IsTrue(eventReceived);
            Assert.AreEqual(pos, receivedEvent.Position);
            Assert.IsNotNull(receivedEvent.BuildingId);
        }

        [Test]
        public void Remove_ClearsCell()
        {
            var pos = new Vector2Int(5, 5);
            var instance = _buildingSystem.Place("basic_tower", pos);

            _buildingSystem.Remove(instance.Id);

            var cell = _gridMap.GetCell(pos);
            Assert.AreEqual(CellState.Empty, cell.State);
        }

        [Test]
        public void Remove_PublishesBuildingRemovedEvent()
        {
            var pos = new Vector2Int(5, 5);
            var instance = _buildingSystem.Place("basic_tower", pos);

            BuildingRemovedEvent receivedEvent = default;
            bool eventReceived = false;
            EventBus.Subscribe<BuildingRemovedEvent>(e =>
            {
                receivedEvent = e;
                eventReceived = true;
            });

            _buildingSystem.Remove(instance.Id);

            Assert.IsTrue(eventReceived);
            Assert.AreEqual(instance.Id, receivedEvent.BuildingId);
            Assert.AreEqual(pos, receivedEvent.Position);
        }
    }
}
