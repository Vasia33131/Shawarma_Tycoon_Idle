using NUnit.Framework;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Managers;
using ShaurmaTycoon.Onboarding;
using ShaurmaTycoon.Save;
using ShaurmaTycoon.Stations;

namespace ShaurmaTycoon.Tests
{
    public class TutorialTests
    {
        private Wallet _wallet;
        private StationController _stations;
        private ManagerController _managers;
        private TutorialService _tutorial;
        private int _saves;

        [SetUp]
        public void SetUp()
        {
            _wallet = new Wallet();
            var definitions = new IStationDefinition[]
            {
                new TestStationDefinition { BaseCost = 4 },
                new TestStationDefinition { BaseCost = 60, BaseIncome = 60, CycleTime = 3, StartLevel = 0 },
            };
            _stations = new StationController(definitions, new MilestoneTable(new int[0], 2), _wallet);
            _managers = new ManagerController(_stations, _wallet, 1000);
            _saves = 0;
            _tutorial = new TutorialService(_stations, _managers, _wallet, () => _saves++);
            _tutorial.Apply(new SaveData());
        }

        [TearDown]
        public void TearDown()
        {
            _tutorial.Dispose();
        }

        private void CompleteTapCycle()
        {
            _stations.Tap(0);
            _stations.Tick(1f);
        }

        [Test]
        public void NewGame_StartsWithTap()
        {
            Assert.AreEqual(TutorialStep.Tap, _tutorial.Step);
            Assert.IsTrue(_tutorial.IsActive);
            Assert.IsTrue(_tutorial.CanDoCurrentStep);
        }

        [Test]
        public void FullFlow_TapBuyUnlockHire_ThenDoneAndSaved()
        {
            CompleteTapCycle();
            Assert.AreEqual(TutorialStep.BuyLevel, _tutorial.Step);

            _wallet.Add(10);
            Assert.IsTrue(_stations.TryBuy(0, 1));
            Assert.AreEqual(TutorialStep.UnlockStation, _tutorial.Step);

            _wallet.Add(60);
            Assert.IsTrue(_stations.TryBuy(1, 1));
            Assert.AreEqual(TutorialStep.HireManager, _tutorial.Step);
            Assert.AreEqual(0, _saves);

            _wallet.Add(4000);
            Assert.IsTrue(_managers.TryHire(0));
            Assert.AreEqual(TutorialStep.Done, _tutorial.Step);
            Assert.IsFalse(_tutorial.IsActive);
            Assert.AreEqual(1, _saves);
        }

        [Test]
        public void CanDoCurrentStep_FalseWithoutCoins()
        {
            CompleteTapCycle();
            _wallet.SetCoins(0);

            Assert.AreEqual(TutorialStep.BuyLevel, _tutorial.Step);
            Assert.IsFalse(_tutorial.CanDoCurrentStep);

            _wallet.Add(_stations.Stations[0].GetCost(1));
            Assert.IsTrue(_tutorial.CanDoCurrentStep);
        }

        [Test]
        public void ActionAhead_SkipsEarlierSteps()
        {
            _wallet.Add(60);
            Assert.IsTrue(_stations.TryBuy(1, 1));

            Assert.AreEqual(TutorialStep.HireManager, _tutorial.Step);
        }

        [Test]
        public void OldSaveWithProgress_IsDoneImmediately()
        {
            _stations.Stations[1].Restore(5, automated: true, progress: 0, running: false);
            _tutorial.Apply(new SaveData());

            Assert.AreEqual(TutorialStep.Done, _tutorial.Step);
        }

        [Test]
        public void SavedStep_IsRestoredAndCaptured()
        {
            _tutorial.Apply(new SaveData { TutorialStep = (int)TutorialStep.UnlockStation });
            Assert.AreEqual(TutorialStep.UnlockStation, _tutorial.Step);

            var data = new SaveData();
            _tutorial.Capture(data);
            Assert.AreEqual((int)TutorialStep.UnlockStation, data.TutorialStep);
        }

        [TestCase(-3, TutorialStep.Tap)]
        [TestCase(99, TutorialStep.Done)]
        public void BrokenSavedStep_IsClamped(int saved, TutorialStep expected)
        {
            _tutorial.Apply(new SaveData { TutorialStep = saved });

            Assert.AreEqual(expected, _tutorial.Step);
        }

        [Test]
        public void Skip_FinishesTutorial()
        {
            TutorialStep changed = TutorialStep.Tap;
            _tutorial.StepChanged += step => changed = step;

            _tutorial.Skip();

            Assert.AreEqual(TutorialStep.Done, _tutorial.Step);
            Assert.AreEqual(TutorialStep.Done, changed);
        }

        [Test]
        public void SingleStationCity_HasNoTutorial()
        {
            var stations = new StationController(new IStationDefinition[] { new TestStationDefinition() },
                new MilestoneTable(new int[0], 2), _wallet);
            var tutorial = new TutorialService(stations, new ManagerController(stations, _wallet, 1000), _wallet, null);

            Assert.IsFalse(tutorial.IsActive);
        }
    }
}
