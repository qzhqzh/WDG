using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace WDG.Tests
{
    [TestFixture]
    public class RewardSystemTests
    {
        private RewardSystem _rewardSystem;
        private ResourceSystem _resourceSystem;
        private BuildingSystem _buildingSystem;
        private ConfigSystem _configSystem;
        private RewardPoolConfig _rewardPoolConfig;

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Clear();
            EventBus.Clear();

            _configSystem = new ConfigSystem();
            ServiceLocator.Register<ConfigSystem>(_configSystem);

            _resourceSystem = new ResourceSystem();
            ServiceLocator.Register<ResourceSystem>(_resourceSystem);
            _resourceSystem.Initialize(100);

            var gridMap = new GridMapSystem();
            ServiceLocator.Register<GridMapSystem>(gridMap);

            var pathfinding = new PathfindingModule();
            ServiceLocator.Register<PathfindingModule>(pathfinding);

            // Setup map for BuildingSystem
            var mapConfig = ScriptableObject.CreateInstance<MapConfig>();
            mapConfig.width = 10;
            mapConfig.height = 10;
            mapConfig.blockedCells = new List<Vector2Int>();
            mapConfig.spawnPoints = new List<Vector2Int> { new Vector2Int(0, 0) };
            mapConfig.corePoint = new Vector2Int(9, 9);
            mapConfig.startingGold = 100;
            mapConfig.coreMaxHealth = 20;
            _configSystem.SetMapConfig(mapConfig);
            gridMap.LoadFromConfig(mapConfig);

            _buildingSystem = new BuildingSystem();
            ServiceLocator.Register<BuildingSystem>(_buildingSystem);

            // Create reward pool with multiple rewards
            _rewardPoolConfig = ScriptableObject.CreateInstance<RewardPoolConfig>();
            _rewardPoolConfig.rewards = new List<RewardDefinition>
            {
                new RewardDefinition
                {
                    rewardId = "reward_gold_1",
                    type = RewardType.ResourceBonus,
                    displayName = "Gold Bonus",
                    description = "Gain 50 gold",
                    weight = 1f,
                    resourceAmount = 50
                },
                new RewardDefinition
                {
                    rewardId = "reward_gold_2",
                    type = RewardType.ResourceBonus,
                    displayName = "Large Gold",
                    description = "Gain 100 gold",
                    weight = 1f,
                    resourceAmount = 100
                },
                new RewardDefinition
                {
                    rewardId = "reward_upgrade_1",
                    type = RewardType.BuildingUpgrade,
                    displayName = "Tower Upgrade",
                    description = "Upgrade towers",
                    weight = 1f,
                    targetBuildingId = "basic_tower",
                    upgradeMultiplier = 1.5f
                },
                new RewardDefinition
                {
                    rewardId = "reward_building_1",
                    type = RewardType.NewBuilding,
                    displayName = "Unlock Sniper",
                    description = "Unlock sniper tower",
                    weight = 1f,
                    unlockBuilding = null // Would be a BuildingConfig in real usage
                },
                new RewardDefinition
                {
                    rewardId = "reward_gold_3",
                    type = RewardType.ResourceBonus,
                    displayName = "Small Gold",
                    description = "Gain 25 gold",
                    weight = 1f,
                    resourceAmount = 25
                }
            };
            _configSystem.SetRewardPoolConfig(_rewardPoolConfig);

            // Building config for upgrade tests
            var towerConfig = ScriptableObject.CreateInstance<BuildingConfig>();
            towerConfig.buildingId = "basic_tower";
            towerConfig.displayName = "Basic Tower";
            towerConfig.cost = 50;
            towerConfig.attackDamage = 10f;
            towerConfig.attackRange = 3f;
            towerConfig.attackInterval = 1f;
            towerConfig.targetingStrategy = TargetingStrategy.Nearest;
            _configSystem.RegisterBuildingConfig(towerConfig);

            _buildingSystem.Initialize();

            _rewardSystem = new RewardSystem();
            ServiceLocator.Register<RewardSystem>(_rewardSystem);
            _rewardSystem.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            ServiceLocator.Clear();
            EventBus.Clear();
        }

        [Test]
        public void DrawRewards_Returns3Cards()
        {
            var cards = _rewardSystem.DrawRewards(3);

            Assert.AreEqual(3, cards.Count);
        }

        [Test]
        public void DrawRewards_NoDuplicates()
        {
            var cards = _rewardSystem.DrawRewards(3);

            var ids = new HashSet<string>();
            foreach (var card in cards)
            {
                Assert.IsTrue(ids.Add(card.Id), "Duplicate card ID found: " + card.Id);
            }
        }

        [Test]
        public void DrawRewards_RespectsPool()
        {
            var cards = _rewardSystem.DrawRewards(3);

            var validIds = new HashSet<string>();
            foreach (var def in _rewardPoolConfig.rewards)
            {
                validIds.Add(def.rewardId);
            }

            foreach (var card in cards)
            {
                Assert.IsTrue(validIds.Contains(card.Id),
                    "Card ID " + card.Id + " is not from the configured pool");
            }
        }

        [Test]
        public void ApplyReward_ResourceBonus_AddsGold()
        {
            int goldBefore = _resourceSystem.CurrentGold;

            var card = new RewardCard
            {
                Id = "reward_gold_1",
                Type = RewardType.ResourceBonus,
                DisplayName = "Gold Bonus",
                Description = "Gain 50 gold",
                ResourceAmount = 50
            };

            _rewardSystem.ApplyReward(card);

            Assert.AreEqual(goldBefore + 50, _resourceSystem.CurrentGold);
        }

        [Test]
        public void ApplyReward_NewBuilding_UnlocksBuilding()
        {
            var unlockConfig = ScriptableObject.CreateInstance<BuildingConfig>();
            unlockConfig.buildingId = "sniper_tower";
            unlockConfig.displayName = "Sniper Tower";
            unlockConfig.cost = 100;
            unlockConfig.attackDamage = 25f;
            unlockConfig.attackRange = 6f;
            unlockConfig.attackInterval = 2f;
            unlockConfig.targetingStrategy = TargetingStrategy.LowestHealth;
            _configSystem.RegisterBuildingConfig(unlockConfig);

            var card = new RewardCard
            {
                Id = "reward_building_1",
                Type = RewardType.NewBuilding,
                DisplayName = "Unlock Sniper",
                Description = "Unlock sniper tower",
                UnlockBuilding = unlockConfig
            };

            _rewardSystem.ApplyReward(card);

            // Verify the building is now available
            var available = _buildingSystem.GetAvailableBuildings();
            bool found = false;
            foreach (var b in available)
            {
                if (b.buildingId == "sniper_tower")
                {
                    found = true;
                    break;
                }
            }
            Assert.IsTrue(found);
        }

        [Test]
        public void ApplyReward_BuildingUpgrade_IncreasesStats()
        {
            // Place a building first
            var instance = _buildingSystem.Place("basic_tower", new Vector2Int(5, 5));
            float originalDamage = instance.AttackDamage;

            var card = new RewardCard
            {
                Id = "reward_upgrade_1",
                Type = RewardType.BuildingUpgrade,
                DisplayName = "Tower Upgrade",
                Description = "Upgrade towers",
                TargetBuildingId = "basic_tower",
                UpgradeMultiplier = 1.5f
            };

            _rewardSystem.ApplyReward(card);

            // Get the building again to check updated stats
            var buildings = _buildingSystem.GetAllBuildings();
            BuildingInstance upgraded = null;
            foreach (var b in buildings)
            {
                if (b.Id == instance.Id)
                {
                    upgraded = b;
                    break;
                }
            }

            Assert.IsNotNull(upgraded);
            Assert.AreEqual(originalDamage * 1.5f, upgraded.AttackDamage, 0.01f);
        }
    }
}
