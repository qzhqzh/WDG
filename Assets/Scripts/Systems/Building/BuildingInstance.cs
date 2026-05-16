using UnityEngine;

namespace WDG
{
    public class BuildingInstance
    {
        public string Id { get; private set; }
        public string ConfigId { get; private set; }
        public Vector2Int GridPosition { get; private set; }
        public float AttackDamage { get; set; }
        public float AttackRange { get; set; }
        public float AttackInterval { get; set; }
        public TargetingStrategy Strategy { get; set; }
        public float AttackCooldownRemaining { get; set; }

        public BuildingInstance(string configId, Vector2Int gridPosition, float attackDamage, float attackRange, float attackInterval, TargetingStrategy strategy)
        {
            Id = System.Guid.NewGuid().ToString();
            ConfigId = configId;
            GridPosition = gridPosition;
            AttackDamage = attackDamage;
            AttackRange = attackRange;
            AttackInterval = attackInterval;
            Strategy = strategy;
            AttackCooldownRemaining = 0f;
        }
    }
}
