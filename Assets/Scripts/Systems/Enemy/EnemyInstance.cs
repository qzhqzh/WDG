using System.Collections.Generic;
using UnityEngine;

namespace WDG
{
    public class EnemyInstance
    {
        private static int _nextInstanceId = 1;

        public string Id { get; private set; }
        public int InstanceId { get; private set; }
        public string ConfigId { get; private set; }
        public float CurrentHealth { get; set; }
        public float MaxHealth { get; private set; }
        public float MoveSpeed { get; private set; }
        public int CoreDamage { get; private set; }
        public int ResourceDrop { get; private set; }
        public int PathIndex { get; set; }
        public List<Vector2Int> Path { get; set; }
        public Vector2 WorldPosition { get; set; }

        public EnemyInstance(string configId, float maxHealth, float moveSpeed, int coreDamage, int resourceDrop)
        {
            Id = System.Guid.NewGuid().ToString();
            InstanceId = _nextInstanceId++;
            ConfigId = configId;
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
            MoveSpeed = moveSpeed;
            CoreDamage = coreDamage;
            ResourceDrop = resourceDrop;
            PathIndex = 0;
            Path = new List<Vector2Int>();
            WorldPosition = Vector2.zero;
        }

        public static void ResetIdCounter()
        {
            _nextInstanceId = 1;
        }
    }
}
