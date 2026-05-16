using System;
using System.Collections.Generic;
using UnityEngine;

namespace WDG
{
    [Serializable]
    public class WaveSpawnGroup
    {
        public EnemyConfig enemyConfig;
        public int count;
        public float spawnInterval;
        public float groupDelay;
        public int spawnPointIndex;
    }

    [CreateAssetMenu(menuName = "WDG/WaveConfig")]
    public class WaveConfig : ScriptableObject
    {
        public int waveIndex;
        public bool isBossWave;
        public List<WaveSpawnGroup> spawnGroups = new List<WaveSpawnGroup>();
        public int completionBonus;
    }
}
