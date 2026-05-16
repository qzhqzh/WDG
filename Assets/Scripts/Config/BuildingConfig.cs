using UnityEngine;

namespace WDG
{
    [CreateAssetMenu(menuName = "WDG/BuildingConfig")]
    public class BuildingConfig : ScriptableObject
    {
        public string buildingId;
        public string displayName;
        public string description;
        public int cost;
        public float attackDamage;
        public float attackRange;
        public float attackInterval;
        public TargetingStrategy targetingStrategy;
        public Vector2Int gridSize = Vector2Int.one;
        public GameObject prefab;
    }
}
