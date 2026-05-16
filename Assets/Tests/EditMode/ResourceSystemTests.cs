using NUnit.Framework;

namespace WDG.Tests
{
    [TestFixture]
    public class ResourceSystemTests
    {
        private ResourceSystem _resourceSystem;

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Clear();
            EventBus.Clear();

            _resourceSystem = new ResourceSystem();
        }

        [TearDown]
        public void TearDown()
        {
            ServiceLocator.Clear();
            EventBus.Clear();
        }

        [Test]
        public void Initialize_SetsCorrectBalance()
        {
            _resourceSystem.Initialize(100);

            Assert.AreEqual(100, _resourceSystem.CurrentGold);
        }

        [Test]
        public void Add_IncreasesBalance()
        {
            _resourceSystem.Initialize(50);

            _resourceSystem.Add(30);

            Assert.AreEqual(80, _resourceSystem.CurrentGold);
        }

        [Test]
        public void TrySpend_SufficientFunds_DecreasesBalance_ReturnsTrue()
        {
            _resourceSystem.Initialize(100);

            bool result = _resourceSystem.TrySpend(40);

            Assert.IsTrue(result);
            Assert.AreEqual(60, _resourceSystem.CurrentGold);
        }

        [Test]
        public void TrySpend_InsufficientFunds_NoChange_ReturnsFalse()
        {
            _resourceSystem.Initialize(30);

            bool result = _resourceSystem.TrySpend(50);

            Assert.IsFalse(result);
            Assert.AreEqual(30, _resourceSystem.CurrentGold);
        }

        [Test]
        public void CanAfford_SufficientFunds_ReturnsTrue()
        {
            _resourceSystem.Initialize(100);

            Assert.IsTrue(_resourceSystem.CanAfford(100));
            Assert.IsTrue(_resourceSystem.CanAfford(50));
        }

        [Test]
        public void CanAfford_InsufficientFunds_ReturnsFalse()
        {
            _resourceSystem.Initialize(20);

            Assert.IsFalse(_resourceSystem.CanAfford(21));
            Assert.IsFalse(_resourceSystem.CanAfford(100));
        }

        [Test]
        public void Add_PublishesResourceChangedEvent()
        {
            _resourceSystem.Initialize(50);

            ResourceChangedEvent receivedEvent = default;
            bool eventReceived = false;
            EventBus.Subscribe<ResourceChangedEvent>(e =>
            {
                receivedEvent = e;
                eventReceived = true;
            });

            _resourceSystem.Add(25);

            Assert.IsTrue(eventReceived);
            Assert.AreEqual(50, receivedEvent.PreviousAmount);
            Assert.AreEqual(75, receivedEvent.NewAmount);
        }

        [Test]
        public void TrySpend_PublishesResourceChangedEvent()
        {
            _resourceSystem.Initialize(100);

            ResourceChangedEvent receivedEvent = default;
            bool eventReceived = false;
            EventBus.Subscribe<ResourceChangedEvent>(e =>
            {
                receivedEvent = e;
                eventReceived = true;
            });

            _resourceSystem.TrySpend(30);

            Assert.IsTrue(eventReceived);
            Assert.AreEqual(100, receivedEvent.PreviousAmount);
            Assert.AreEqual(70, receivedEvent.NewAmount);
        }
    }
}
