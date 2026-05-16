using UnityEngine;

namespace WDG
{
    [CreateAssetMenu(menuName = "WDG/EnemyConfig")]
    public class EnemyConfig : ScriptableObject
    {
        public string enemyId;
        public string displayName;
        public float maxHealth;
        public float moveSpeed;
        public int coreDamage;
        public int resourceDrop;
        public bool isBoss;
        public GameObject prefab;
    }
}
