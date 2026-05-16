namespace WDG
{
    public class RewardCard
    {
        public string Id { get; set; }
        public RewardType Type { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string TargetBuildingId { get; set; }
        public BuildingConfig UnlockBuilding { get; set; }
        public int ResourceAmount { get; set; }
        public float UpgradeMultiplier { get; set; }
    }
}
