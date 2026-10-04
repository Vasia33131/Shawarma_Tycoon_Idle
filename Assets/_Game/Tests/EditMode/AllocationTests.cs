using NUnit.Framework;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Stations;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace ShaurmaTycoon.Tests
{
    /// <summary>
    /// Проверки «без аллокаций» (только в Unity Test Runner: используют ограничения Unity Test Framework).
    /// </summary>
    public class AllocationTests
    {
        [Test]
        public void NumberText_AfterWarmUp_DoesNotAllocate()
        {
            var text = new NumberText();
            text.Clear().Append("+").AppendNumber(123456789).Append("/с");

            Assert.That(() =>
            {
                text.Clear().Append("+").AppendNumber(987654321).Append("/с");
                text.Clear().AppendNumber(1.5e40);
            }, Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void StationTick_DoesNotAllocate()
        {
            var wallet = new Wallet();
            var controller = new StationController(
                new IStationDefinition[] { new TestStationDefinition(), new TestStationDefinition { CycleTime = 0.01 } },
                new MilestoneTable(new[] { 25 }, 2), wallet);
            controller.Stations[0].IsAutomated = true;
            controller.Stations[1].IsAutomated = true;
            controller.Tick(0.1f);

            Assert.That(() =>
            {
                for (int i = 0; i < 100; i++)
                    controller.Tick(0.016f);
            }, Is.Not.AllocatingGCMemory());
        }
    }
}
