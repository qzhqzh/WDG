namespace WDG
{
    public class ResourceSystem : IGameSystem
    {
        public int CurrentGold { get; private set; }

        public void Initialize()
        {
            // No-op: Initialize(int startAmount) is called separately
        }

        public void Initialize(int startAmount)
        {
            CurrentGold = startAmount;
        }

        public void Tick(float deltaTime)
        {
            // No-op
        }

        public void Dispose()
        {
            CurrentGold = 0;
        }

        public void Add(int amount)
        {
            int previous = CurrentGold;
            CurrentGold += amount;
            EventBus.Publish(new ResourceChangedEvent
            {
                PreviousAmount = previous,
                NewAmount = CurrentGold
            });
        }

        public bool TrySpend(int amount)
        {
            if (!CanAfford(amount))
                return false;

            int previous = CurrentGold;
            CurrentGold -= amount;
            EventBus.Publish(new ResourceChangedEvent
            {
                PreviousAmount = previous,
                NewAmount = CurrentGold
            });
            return true;
        }

        public bool CanAfford(int amount)
        {
            return CurrentGold >= amount;
        }

        public void AddWaveBonus(int amount)
        {
            Add(amount);
        }
    }
}
