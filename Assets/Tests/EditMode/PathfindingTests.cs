using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace WDG.Tests
{
    [TestFixture]
    public class PathfindingTests
    {
        private PathfindingModule _pathfinding;
        private GridMapSystem _gridMap;

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Clear();
            EventBus.Clear();

            _pathfinding = new PathfindingModule();
            _gridMap = new GridMapSystem();
        }

        [TearDown]
        public void TearDown()
        {
            ServiceLocator.Clear();
            EventBus.Clear();
        }

        private MapConfig CreateMapConfig(int width, int height, List<Vector2Int> blocked = null)
        {
            var config = ScriptableObject.CreateInstance<MapConfig>();
            config.width = width;
            config.height = height;
            config.blockedCells = blocked ?? new List<Vector2Int>();
            config.spawnPoints = new List<Vector2Int> { new Vector2Int(0, 0) };
            config.corePoint = new Vector2Int(width - 1, height - 1);
            config.startingGold = 100;
            config.coreMaxHealth = 20;
            return config;
        }

        [Test]
        public void FindPath_EmptyGrid_ReturnsDirectPath()
        {
            var config = CreateMapConfig(5, 5);
            _gridMap.LoadFromConfig(config);

            var start = new Vector2Int(0, 0);
            var end = new Vector2Int(4, 4);

            var path = _pathfinding.FindPath(start, end, _gridMap);

            Assert.IsNotNull(path);
            Assert.IsTrue(path.Count > 0);
            Assert.AreEqual(start, path[0]);
            Assert.AreEqual(end, path[path.Count - 1]);
            // Manhattan distance on 5x5 grid from (0,0) to (4,4) = 8 steps + 1 for start = 9
            Assert.AreEqual(9, path.Count);
        }

        [Test]
        public void FindPath_WithObstacles_FindsDetour()
        {
            // Create a wall that blocks the direct path
            var blocked = new List<Vector2Int>
            {
                new Vector2Int(1, 0),
                new Vector2Int(1, 1),
                new Vector2Int(1, 2),
                new Vector2Int(1, 3)
            };
            var config = CreateMapConfig(5, 5, blocked);
            _gridMap.LoadFromConfig(config);

            var start = new Vector2Int(0, 0);
            var end = new Vector2Int(2, 0);

            var path = _pathfinding.FindPath(start, end, _gridMap);

            Assert.IsNotNull(path);
            Assert.IsTrue(path.Count > 0);
            Assert.AreEqual(start, path[0]);
            Assert.AreEqual(end, path[path.Count - 1]);
            // Path must be longer than direct Manhattan distance (2) because of wall
            Assert.IsTrue(path.Count > 3);
        }

        [Test]
        public void FindPath_NoValidPath_ReturnsEmpty()
        {
            // Completely surround the end point
            var blocked = new List<Vector2Int>
            {
                new Vector2Int(3, 3),
                new Vector2Int(3, 4),
                new Vector2Int(4, 3)
            };
            var config = CreateMapConfig(5, 5, blocked);
            _gridMap.LoadFromConfig(config);

            var start = new Vector2Int(0, 0);
            var end = new Vector2Int(4, 4);

            var path = _pathfinding.FindPath(start, end, _gridMap);

            Assert.IsNotNull(path);
            Assert.AreEqual(0, path.Count);
        }

        [Test]
        public void FindPath_StartEqualsEnd_ReturnsSingleNode()
        {
            var config = CreateMapConfig(5, 5);
            _gridMap.LoadFromConfig(config);

            var pos = new Vector2Int(2, 2);
            var path = _pathfinding.FindPath(pos, pos, _gridMap);

            Assert.IsNotNull(path);
            Assert.AreEqual(1, path.Count);
            Assert.AreEqual(pos, path[0]);
        }

        [Test]
        public void HasValidPath_OpenGrid_ReturnsTrue()
        {
            var config = CreateMapConfig(5, 5);
            _gridMap.LoadFromConfig(config);

            var start = new Vector2Int(0, 0);
            var end = new Vector2Int(4, 4);

            Assert.IsTrue(_pathfinding.HasValidPath(start, end, _gridMap));
        }

        [Test]
        public void HasValidPath_BlockedGrid_ReturnsFalse()
        {
            // Create a complete wall blocking access
            var blocked = new List<Vector2Int>
            {
                new Vector2Int(2, 0),
                new Vector2Int(2, 1),
                new Vector2Int(2, 2),
                new Vector2Int(2, 3),
                new Vector2Int(2, 4)
            };
            var config = CreateMapConfig(5, 5, blocked);
            _gridMap.LoadFromConfig(config);

            var start = new Vector2Int(0, 0);
            var end = new Vector2Int(4, 4);

            Assert.IsFalse(_pathfinding.HasValidPath(start, end, _gridMap));
        }

        [Test]
        public void FindPath_LargeGrid_CompletesReasonably()
        {
            // 20x15 grid with scattered obstacles
            var blocked = new List<Vector2Int>
            {
                new Vector2Int(5, 0), new Vector2Int(5, 1), new Vector2Int(5, 2),
                new Vector2Int(5, 3), new Vector2Int(5, 4), new Vector2Int(5, 5),
                new Vector2Int(10, 5), new Vector2Int(10, 6), new Vector2Int(10, 7),
                new Vector2Int(10, 8), new Vector2Int(10, 9), new Vector2Int(10, 10),
                new Vector2Int(15, 0), new Vector2Int(15, 1), new Vector2Int(15, 2),
                new Vector2Int(15, 3), new Vector2Int(15, 4)
            };
            var config = CreateMapConfig(20, 15, blocked);
            _gridMap.LoadFromConfig(config);

            var start = new Vector2Int(0, 0);
            var end = new Vector2Int(19, 14);

            var path = _pathfinding.FindPath(start, end, _gridMap);

            Assert.IsNotNull(path);
            Assert.IsTrue(path.Count > 0);
            Assert.AreEqual(start, path[0]);
            Assert.AreEqual(end, path[path.Count - 1]);
        }
    }
}
