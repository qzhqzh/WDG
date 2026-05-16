using System.Collections.Generic;
using UnityEngine;

namespace WDG
{
    public class RewardSystem : IGameSystem
    {
        private RewardPoolConfig _poolConfig;

        public void Initialize()
        {
            var configSystem = ServiceLocator.Get<ConfigSystem>();
            _poolConfig = configSystem.RewardPoolConfig;
        }

        public void Tick(float deltaTime)
        {
            // No-op
        }

        public void Dispose()
        {
            _poolConfig = null;
        }

        public List<RewardCard> DrawRewards(int count = 3)
        {
            var result = new List<RewardCard>();

            if (_poolConfig == null || _poolConfig.rewards == null || _poolConfig.rewards.Count == 0)
                return result;

            var available = new List<RewardDefinition>(_poolConfig.rewards);

            for (int i = 0; i < count && available.Count > 0; i++)
            {
                float totalWeight = 0f;
                foreach (var reward in available)
                {
                    totalWeight += reward.weight;
                }

                float roll = Random.Range(0f, totalWeight);
                float cumulative = 0f;
                RewardDefinition selected = null;

                foreach (var reward in available)
                {
                    cumulative += reward.weight;
                    if (roll <= cumulative)
                    {
                        selected = reward;
                        break;
                    }
                }

                if (selected == null)
                    selected = available[available.Count - 1];

                var card = new RewardCard
                {
                    Id = selected.rewardId,
                    Type = selected.type,
                    DisplayName = selected.displayName,
                    Description = selected.description,
                    TargetBuildingId = selected.targetBuildingId,
                    UnlockBuilding = selected.unlockBuilding,
                    ResourceAmount = selected.resourceAmount,
                    UpgradeMultiplier = selected.upgradeMultiplier
                };

                result.Add(card);
                available.Remove(selected);
            }

            return result;
        }

        public void ApplyReward(RewardCard card)
        {
            switch (card.Type)
            {
                case RewardType.NewBuilding:
                    if (card.UnlockBuilding != null)
                    {
                        var buildingSystem = ServiceLocator.Get<BuildingSystem>();
                        buildingSystem.UnlockBuilding(card.UnlockBuilding.buildingId);
                    }
                    break;

                case RewardType.ResourceBonus:
                    var resourceSystem = ServiceLocator.Get<ResourceSystem>();
                    resourceSystem.Add(card.ResourceAmount);
                    break;

                case RewardType.BuildingUpgrade:
                    if (!string.IsNullOrEmpty(card.TargetBuildingId))
                    {
                        var buildingSystem = ServiceLocator.Get<BuildingSystem>();

                        // Upgrade existing placed buildings
                        var buildings = buildingSystem.GetAllBuildings();
                        foreach (var building in buildings)
                        {
                            if (building.ConfigId == card.TargetBuildingId)
                            {
                                building.AttackDamage *= card.UpgradeMultiplier;
                            }
                        }

                        // Store multiplier so future placements also benefit
                        buildingSystem.SetDamageMultiplier(card.TargetBuildingId, card.UpgradeMultiplier);
                    }
                    break;
            }

            EventBus.Publish(new RewardSelectedEvent
            {
                RewardId = card.Id,
                Type = card.Type
            });
        }
    }
}
