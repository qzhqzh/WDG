using System;
using System.Collections.Generic;
using UnityEngine;

namespace WDG
{
    [Serializable]
    public class RewardDefinition
    {
        public string rewardId;
        public RewardType type;
        public string displayName;
        public string description;
        public float weight;
        public BuildingConfig unlockBuilding;
        public int resourceAmount;
        public string targetBuildingId;
        public float upgradeMultiplier;
    }

    [CreateAssetMenu(menuName = "WDG/RewardPoolConfig")]
    public class RewardPoolConfig : ScriptableObject
    {
        public List<RewardDefinition> rewards = new List<RewardDefinition>();
    }
}
