using UnityEngine;

namespace WDG
{
    [CreateAssetMenu(menuName = "WDG/GameBalanceConfig")]
    public class GameBalanceConfig : ScriptableObject
    {
        public int initialGold = 100;
        public int coreMaxHealth = 20;
        public int baseWaveReward = 50;
        public int rewardCardsPerWave = 3;
    }
}
