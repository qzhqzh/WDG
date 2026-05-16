using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace WDG.Tests
{
    [TestFixture]
    public class GridMapTests
    {
        private GridMapSystem _gridMap;
        private MapConfig _mapConfig;

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Clear();
            EventBus.Clear();

            _gridMap = new GridMapSystem();

            _mapConfig = ScriptableObject.CreateInstance<MapConfig>();
            _mapConfig.width = 10;
            _mapConfig.height = 8;
            _mapConfig.blockedCells = new List<Vector2Int>
            {
                new Vector2Int(3, 3),
                new Vector2Int(4, 3),
                new Vector2Int(5, 3)
            };
            _mapConfig.spawnPoints = new List<Vector2Int>
            {
                new Vector2Int(0, 0),
                new Vector2Int(0, 7)
            };
            _mapConfig.corePoint = new Vector2Int(9, 4);
            _mapConfig.startingGold = 100;
            _mapConfig.coreMaxHealth = 20;

            _gridMap.LoadFromConfig(_mapConfig);
        }

        [TearDown]
        public void TearDown()
        {
            ServiceLocator.Clear();
            EventBus.Clear();
        }

        [Test]
        public void LoadFromConfig_SetsCorrectDimensions()
        {
            Assert.AreEqual(10, _gridMap.Width);
            Assert.AreEqual(8, _gridMap.Height);
        }

        [Test]
        public void LoadFromConfig_SetsBlockedCells()
        {
            var cell1 = _gridMap.GetCell(new Vector2Int(3, 3));
            var cell2 = _gridMap.GetCell(new Vector2Int(4, 3));
            var cell3 = _gridMap.GetCell(new Vector2Int(5, 3));

            Assert.AreEqual(CellState.Blocked, cell1.State);
            Assert.AreEqual(CellState.Blocked, cell2.State);
            Assert.AreEqual(CellState.Blocked, cell3.State);
        }

        [Test]
        public void LoadFromConfig_SetsSpawnPoints()
        {
            var cell1 = _gridMap.GetCell(new Vector2Int(0, 0));
            var cell2 = _gridMap.GetCell(new Vector2Int(0, 7));

            Assert.AreEqual(CellState.SpawnPoint, cell1.State);
            Assert.AreEqual(CellState.SpawnPoint, cell2.State);
        }

        [Test]
        public void LoadFromConfig_SetsCorePoint()
        {
            var cell = _gridMap.GetCell(new Vector2Int(9, 4));

            Assert.AreEqual(CellState.CorePoint, cell.State);
        }

        [Test]
        public void IsBuildable_EmptyCell_ReturnsTrue()
        {
            // Cell (1, 1) should be empty by default
            Assert.IsTrue(_gridMap.IsBuildable(new Vector2Int(1, 1)));
        }

        [Test]
        public void IsBuildable_BlockedCell_ReturnsFalse()
        {
            Assert.IsFalse(_gridMap.IsBuildable(new Vector2Int(3, 3)));
        }

        [Test]
        public void IsBuildable_OccupiedCell_ReturnsFalse()
        {
            _gridMap.SetCellState(new Vector2Int(2, 2), CellState.Occupied);

            Assert.IsFalse(_gridMap.IsBuildable(new Vector2Int(2, 2)));
        }

        [Test]
        public void IsBuildable_SpawnPoint_ReturnsFalse()
        {
            Assert.IsFalse(_gridMap.IsBuildable(new Vector2Int(0, 0)));
        }

        [Test]
        public void SetCellState_UpdatesCorrectly()
        {
            var pos = new Vector2Int(5, 5);
            Assert.AreEqual(CellState.Empty, _gridMap.GetCell(pos).State);

            _gridMap.SetCellState(pos, CellState.Occupied, "building_1");

            var cell = _gridMap.GetCell(pos);
            Assert.AreEqual(CellState.Occupied, cell.State);
            Assert.AreEqual("building_1", cell.OccupantId);
        }

        [Test]
        public void IsInBounds_ValidPosition_ReturnsTrue()
        {
            Assert.IsTrue(_gridMap.IsInBounds(new Vector2Int(0, 0)));
            Assert.IsTrue(_gridMap.IsInBounds(new Vector2Int(9, 7)));
            Assert.IsTrue(_gridMap.IsInBounds(new Vector2Int(5, 4)));
        }

        [Test]
        public void IsInBounds_NegativePosition_ReturnsFalse()
        {
            Assert.IsFalse(_gridMap.IsInBounds(new Vector2Int(-1, 0)));
            Assert.IsFalse(_gridMap.IsInBounds(new Vector2Int(0, -1)));
            Assert.IsFalse(_gridMap.IsInBounds(new Vector2Int(-5, -3)));
        }

        [Test]
        public void IsInBounds_ExceedsDimensions_ReturnsFalse()
        {
            Assert.IsFalse(_gridMap.IsInBounds(new Vector2Int(10, 0)));
            Assert.IsFalse(_gridMap.IsInBounds(new Vector2Int(0, 8)));
            Assert.IsFalse(_gridMap.IsInBounds(new Vector2Int(15, 15)));
        }
    }
}
