using System.Collections.Generic;
using NUnit.Framework;
using ShaurmaTycoon.Customers;

namespace ShaurmaTycoon.Tests
{
    public class CatStateTests
    {
        private const float Sleep = 10f;
        private const float Sit = 5f;

        private CatBrain _cat;
        private List<CatState> _changes;

        [SetUp]
        public void SetUp()
        {
            // Интервалы без разброса — переходы по времени точные.
            _cat = new CatBrain(new System.Random(1), Sleep, Sleep, Sit, Sit);
            _changes = new List<CatState>();
            _cat.StateChanged += _changes.Add;
        }

        [Test]
        public void StartsAsleep()
        {
            Assert.AreEqual(CatState.Sleep, _cat.State);
            Assert.AreEqual(Sleep, _cat.TimeLeft, 1e-4f);
        }

        [Test]
        public void Sleep_StretchesAfterSleepTime()
        {
            _cat.Tick(Sleep - 0.1f);
            Assert.AreEqual(CatState.Sleep, _cat.State);

            _cat.Tick(0.2f);
            Assert.AreEqual(CatState.Stretch, _cat.State);
            Assert.AreEqual(CatBrain.StretchDuration, _cat.TimeLeft, 1e-4f);
        }

        [Test]
        public void FullCycle_SleepStretchSitSleep()
        {
            _cat.Tick(Sleep);
            _cat.Tick(CatBrain.StretchDuration);
            Assert.AreEqual(CatState.Sit, _cat.State);
            Assert.AreEqual(Sit, _cat.TimeLeft, 1e-4f);

            _cat.Tick(Sit);
            Assert.AreEqual(CatState.Sleep, _cat.State);
            CollectionAssert.AreEqual(new[] { CatState.Stretch, CatState.Sit, CatState.Sleep }, _changes);
        }

        [Test]
        public void TapWhileAsleep_MeowsAndSits()
        {
            Assert.AreEqual(CatReaction.Meow, _cat.Tap());
            Assert.AreEqual(CatState.Sit, _cat.State);
            Assert.AreEqual(Sit, _cat.TimeLeft, 1e-4f);
        }

        [Test]
        public void TapWhileSitting_Purrs_ThenSitsAgain()
        {
            _cat.Tap();

            Assert.AreEqual(CatReaction.Purr, _cat.Tap());
            Assert.AreEqual(CatState.Purr, _cat.State);
            Assert.AreEqual(CatBrain.PurrDuration, _cat.TimeLeft, 1e-4f);

            _cat.Tick(CatBrain.PurrDuration);
            Assert.AreEqual(CatState.Sit, _cat.State);
        }

        [Test]
        public void TapWhileStretching_Purrs()
        {
            _cat.Tick(Sleep);
            Assert.AreEqual(CatState.Stretch, _cat.State);

            Assert.AreEqual(CatReaction.Purr, _cat.Tap());
            Assert.AreEqual(CatState.Purr, _cat.State);
        }

        [Test]
        public void TapDuringPurr_IgnoredWithinCooldown()
        {
            _cat.Tap();
            _cat.Tap();
            _cat.Tick(CatBrain.TapCooldown * 0.5f);

            Assert.AreEqual(CatReaction.None, _cat.Tap());
            Assert.AreEqual(CatBrain.PurrDuration - CatBrain.TapCooldown * 0.5f, _cat.TimeLeft, 1e-4f);
        }

        [Test]
        public void TapDuringPurr_AfterCooldown_ExtendsPurr()
        {
            _cat.Tap();
            _cat.Tap();
            _cat.Tick(CatBrain.PurrDuration - 0.1f);
            int changes = _changes.Count;

            Assert.AreEqual(CatReaction.Purr, _cat.Tap());
            Assert.AreEqual(CatState.Purr, _cat.State);
            Assert.AreEqual(CatBrain.PurrDuration, _cat.TimeLeft, 1e-4f);
            Assert.AreEqual(changes, _changes.Count, "Продление мурчания — не смена позы.");

            _cat.Tick(CatBrain.PurrDuration - 0.1f);
            Assert.AreEqual(CatState.Purr, _cat.State);
        }

        [Test]
        public void RandomDurations_StayInRange()
        {
            var random = new System.Random(42);
            for (int i = 0; i < 50; i++)
            {
                var cat = new CatBrain(random, 40f, 90f, 10f, 20f);
                Assert.That(cat.TimeLeft, Is.InRange(40f, 90f));

                cat.Tap();
                Assert.That(cat.TimeLeft, Is.InRange(10f, 20f));
            }
        }
    }
}
