using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace WDG.Tests
{
    [TestFixture]
    public class CombatSystemTests
    {
        private CombatSystem _combatSystem;
        private BuildingSystem _buildingSystem;
        private EnemySystem _enemySystem;
        private ResourceSystem _resourceSystem;
        private GridMapSystem _gridMap;
        private PathfindingModule _pathfinding;
        private ConfigSystem _configSystem;
        private BuildingConfig _towerConfig;
        private EnemyConfig _enemyConfig;

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Clear();
            EventBus.Clear();
            EnemyInstance.ResetIdCounter();

            _configSystem = new ConfigSystem();
            ServiceLocator.Register<ConfigSystem>(_configSystem);

            _gridMap = new GridMapSystem();
            ServiceLocator.Register<GridMapSystem>(_gridMap);

            _pathfinding = new PathfindingModule();
            ServiceLocator.Register<PathfindingModule>(_pathfinding);

            _resourceSystem = new ResourceSystem();
            ServiceLocator.Register<ResourceSystem>(_resourceSystem);
            _resourceSystem.Initialize(500);

            _enemySystem = new EnemySystem();
            ServiceLocator.Register<EnemySystem>(_enemySystem);

            _buildingSystem = new BuildingSystem();
            ServiceLocator.Register<BuildingSystem>(_buildingSystem);

            _combatSystem = new CombatSystem();
            ServiceLocator.Register<CombatSystem>(_combatSystem);

            // Setup configs
            _towerConfig = ScriptableObject.CreateInstance<BuildingConfig>();
            _towerConfig.buildingId = "tower";
            _towerConfig.displayName = "Tower";
            _towerConfig.cost = 50;
            _towerConfig.attackDamage = 10f;
            _towerConfig.attackRange = 3f;
            _towerConfig.attackInterval = 1f;
            _towerConfig.targetingStrategy = TargetingStrategy.Nearest;
            _configSystem.RegisterBuildingConfig(_towerConfig);

            _enemyConfig = ScriptableObject.CreateInstance<EnemyConfig>();
            _enemyConfig.enemyId = "goblin";
            _enemyConfig.maxHealth = 30f;
            _enemyConfig.moveSpeed = 1f;
            _enemyConfig.coreDamage = 1;
            _enemyConfig.resourceDrop = 10;

            // Setup map
            var mapConfig = ScriptableObject.CreateInstance<MapConfig>();
            mapConfig.width = 10;
            mapConfig.height = 10;
            mapConfig.blockedCells = new List<Vector2Int>();
            mapConfig.spawnPoints = new List<Vector2Int> { new Vector2Int(0, 5) };
            mapConfig.corePoint = new Vector2Int(9, 5);
            mapConfig.startingGold = 500;
            mapConfig.coreMaxHealth = 20;
            _configSystem.SetMapConfig(mapConfig);
            _gridMap.LoadFromConfig(mapConfig);

            // GameCore needed for EnemySystem reaching core
            var gameCore = new GameCore();
            ServiceLocator.Register<GameCore>(gameCore);

            _buildingSystem.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            ServiceLocator.Clear();
            EventBus.Clear();
        }

        [Test]
        public void Tick_BuildingAttacksEnemyInRange()
        {
            // Place tower at (3, 5), spawn enemy near it
            _buildingSystem.Place("tower", new Vector2Int(3, 5));

            // Spawn enemy at (0, 5) - it will path through (3, 5) area
            var enemy = _enemySystem.SpawnEnemy(_enemyConfig, new Vector2Int(0, 5));

            // Move enemy close to the tower (within range 3)
            enemy.WorldPosition = new Vector2(3f, 5f);

            AttackEvent receivedAttack = default;
            bool attackFired = false;
            EventBus.Subscribe<AttackEvent>(e =>
            {
                receivedAttack = e;
                attackFired = true;
            });

            _combatSystem.Tick(0.1f);

            Assert.IsTrue(attackFired);
            Assert.AreEqual(10f, receivedAttack.Damage);
        }

        [Test]
        public void Tick_BuildingDoesNotAttackOutOfRange()
        {
            // Place tower at (3, 5) with range 3
            _buildingSystem.Place("tower", new Vector2Int(3, 5));

            // Spawn enemy far away
            var enemy = _enemySystem.SpawnEnemy(_enemyConfig, new Vector2Int(0, 5));
            enemy.WorldPosition = new Vector2(8f, 5f); // Distance > 3

            bool attackFired = false;
            EventBus.Subscribe<AttackEvent>(e => attackFired = true);

            _combatSystem.Tick(0.1f);

            Assert.IsFalse(attackFired);
        }

        [Test]
        public void Tick_CooldownPreventsAttack()
        {
            _buildingSystem.Place("tower", new Vector2Int(3, 5));

            var enemy = _enemySystem.SpawnEnemy(_enemyConfig, new Vector2Int(0, 5));
            enemy.WorldPosition = new Vector2(3f, 5f);

            // First tick fires
            _combatSystem.Tick(0.1f);

            int attackCount = 0;
            EventBus.Subscribe<AttackEvent>(e => attackCount++);

            // Second tick within cooldown (interval is 1s, only 0.1s passed)
            _combatSystem.Tick(0.1f);

            Assert.AreEqual(0, attackCount);
        }

        [Test]
        public void Tick_NearestStrategy_SelectsClosestEnemy()
        {
            // Place tower at (5, 5) with Nearest targeting
            _buildingSystem.Place("tower", new Vector2Int(5, 5));

            // Spawn two enemies, one closer than the other
            var enemyFar = _enemySystem.SpawnEnemy(_enemyConfig, new Vector2Int(0, 5));
            enemyFar.WorldPosition = new Vector2(4f, 5f); // distance ~ 1.5

            var enemyClose = _enemySystem.SpawnEnemy(_enemyConfig, new Vector2Int(0, 5));
            enemyClose.WorldPosition = new Vector2(5.5f, 5.5f); // distance ~ 0.7

            AttackEvent receivedAttack = default;
            EventBus.Subscribe<AttackEvent>(e => receivedAttack = e);

            _combatSystem.Tick(0.1f);

            Assert.AreEqual(enemyClose.InstanceId, receivedAttack.TargetInstanceId);
        }

        [Test]
        public void Tick_LowestHealthStrategy_SelectsWeakestEnemy()
        {
            // Create a tower config with LowestHealth strategy
            var sniperConfig = ScriptableObject.CreateInstance<BuildingConfig>();
            sniperConfig.buildingId = "sniper";
            sniperConfig.displayName = "Sniper";
            sniperConfig.cost = 50;
            sniperConfig.attackDamage = 15f;
            sniperConfig.attackRange = 5f;
            sniperConfig.attackInterval = 2f;
            sniperConfig.targetingStrategy = TargetingStrategy.LowestHealth;
            _configSystem.RegisterBuildingConfig(sniperConfig);

            _buildingSystem.Place("sniper", new Vector2Int(5, 5));

            // Spawn two enemies at similar distance but different health
            var enemyHealthy = _enemySystem.SpawnEnemy(_enemyConfig, new Vector2Int(0, 5));
            enemyHealthy.WorldPosition = new Vector2(5f, 4f);
            enemyHealthy.CurrentHealth = 30f;

            var enemyWeak = _enemySystem.SpawnEnemy(_enemyConfig, new Vector2Int(0, 5));
            enemyWeak.WorldPosition = new Vector2(5f, 6f);
            enemyWeak.CurrentHealth = 5f;

            AttackEvent receivedAttack = default;
            EventBus.Subscribe<AttackEvent>(e => receivedAttack = e);

            _combatSystem.Tick(0.1f);

            Assert.AreEqual(enemyWeak.InstanceId, receivedAttack.TargetInstanceId);
        }

        [Test]
        public void Tick_EnemyDies_TriggersResourceDrop()
        {
            _buildingSystem.Place("tower", new Vector2Int(3, 5));

            // Spawn enemy with low health so it dies in one hit
            var weakEnemy = ScriptableObject.CreateInstance<EnemyConfig>();
            weakEnemy.enemyId = "weak";
            weakEnemy.maxHealth = 5f;
            weakEnemy.moveSpeed = 1f;
            weakEnemy.coreDamage = 1;
            weakEnemy.resourceDrop = 25;

            var enemy = _enemySystem.SpawnEnemy(weakEnemy, new Vector2Int(0, 5));
            enemy.WorldPosition = new Vector2(3f, 5f);

            int goldBefore = _resourceSystem.CurrentGold;
            _combatSystem.Tick(0.1f);

            Assert.AreEqual(goldBefore + 25, _resourceSystem.CurrentGold);
        }

        [Test]
        public void Tick_EnemyDies_PublishesEnemyKilledEvent()
        {
            _buildingSystem.Place("tower", new Vector2Int(3, 5));

            var weakEnemy = ScriptableObject.CreateInstance<EnemyConfig>();
            weakEnemy.enemyId = "fragile";
            weakEnemy.maxHealth = 5f;
            weakEnemy.moveSpeed = 1f;
            weakEnemy.coreDamage = 1;
            weakEnemy.resourceDrop = 10;

            var enemy = _enemySystem.SpawnEnemy(weakEnemy, new Vector2Int(0, 5));
            enemy.WorldPosition = new Vector2(3f, 5f);

            EnemyKilledEvent killedEvent = default;
            bool eventFired = false;
            EventBus.Subscribe<EnemyKilledEvent>(e =>
            {
                killedEvent = e;
                eventFired = true;
            });

            _combatSystem.Tick(0.1f);

            Assert.IsTrue(eventFired);
            Assert.AreEqual(enemy.InstanceId, killedEvent.InstanceId);
            Assert.AreEqual("fragile", killedEvent.EnemyId);
            Assert.AreEqual(10, killedEvent.ResourceDrop);
        }
    }
}
