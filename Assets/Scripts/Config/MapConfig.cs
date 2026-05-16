using System.Collections.Generic;
using UnityEngine;

namespace WDG
{
    [CreateAssetMenu(menuName = "WDG/MapConfig")]
    public class MapConfig : ScriptableObject
    {
        public int width;
        public int height;
        public List<Vector2Int> blockedCells = new List<Vector2Int>();
        public List<Vector2Int> spawnPoints = new List<Vector2Int>();
        public Vector2Int corePoint;
        public int startingGold;
        public int coreMaxHealth;
    }
}
