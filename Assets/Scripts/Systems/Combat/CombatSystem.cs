using System.Collections.Generic;
using UnityEngine;

namespace WDG
{
    public class CombatSystem : IGameSystem
    {
        public void Initialize()
        {
            // No-op
        }

        public void Tick(float deltaTime)
        {
            var buildingSystem = ServiceLocator.Get<BuildingSystem>();
            var enemySystem = ServiceLocator.Get<EnemySystem>();
            var resourceSystem = ServiceLocator.Get<ResourceSystem>();

            var buildings = buildingSystem.GetAllBuildings();
            var enemies = enemySystem.GetActiveEnemies();

            foreach (var building in buildings)
            {
                building.AttackCooldownRemaining -= deltaTime;

                if (building.AttackCooldownRemaining > 0f)
                    continue;

                if (enemies.Count == 0)
                    continue;

                var target = FindTarget(building, enemies);
                if (target == null)
                    continue;

                // Attack the target
                target.CurrentHealth -= building.AttackDamage;
                building.AttackCooldownRemaining = building.AttackInterval;

                EventBus.Publish(new AttackEvent
                {
                    AttackerId = building.Id,
                    TargetInstanceId = target.InstanceId,
                    Damage = building.AttackDamage,
                    TargetPosition = target.WorldPosition
                });

                // Check for enemy death
                if (target.CurrentHealth <= 0f)
                {
                    EventBus.Publish(new EnemyKilledEvent
                    {
                        InstanceId = target.InstanceId,
                        EnemyId = target.ConfigId,
                        ResourceDrop = target.ResourceDrop
                    });

                    resourceSystem.Add(target.ResourceDrop);
                    enemySystem.RemoveEnemy(target.Id);

                    // Remove dead enemy from local list
                    enemies.Remove(target);
                }
            }
        }

        public void Dispose()
        {
            // No-op
        }

        private EnemyInstance FindTarget(BuildingInstance building, List<EnemyInstance> enemies)
        {
            Vector2 buildingCenter = new Vector2(building.GridPosition.x + 0.5f, building.GridPosition.y + 0.5f);

            EnemyInstance bestTarget = null;
            float bestValue = float.MaxValue;

            foreach (var enemy in enemies)
            {
                float distance = Vector2.Distance(buildingCenter, enemy.WorldPosition);

                if (distance > building.AttackRange)
                    continue;

                switch (building.Strategy)
                {
                    case TargetingStrategy.Nearest:
                        if (distance < bestValue)
                        {
                            bestValue = distance;
                            bestTarget = enemy;
                        }
                        break;

                    case TargetingStrategy.LowestHealth:
                        if (enemy.CurrentHealth < bestValue)
                        {
                            bestValue = enemy.CurrentHealth;
                            bestTarget = enemy;
                        }
                        break;
                }
            }

            return bestTarget;
        }
    }
}
