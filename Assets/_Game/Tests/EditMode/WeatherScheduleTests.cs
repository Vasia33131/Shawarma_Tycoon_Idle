using System;
using NUnit.Framework;
using ShaurmaTycoon.Ambient;

namespace ShaurmaTycoon.Tests
{
    public class WeatherScheduleTests
    {
        private const int SlotMinutes = 10;

        private static WeatherSchedule CreateSchedule()
        {
            return new WeatherSchedule(SlotMinutes, 2, 4);
        }

        // Каждую минуту за сутки, начиная с from.
        private static int Count(WeatherSchedule schedule, DateTime from, string city, float rainChance, bool snowAllowed, WeatherKind kind)
        {
            int count = 0;
            for (int minute = 0; minute < 24 * 60; minute++)
            {
                if (schedule.Get(from.AddMinutes(minute), city, rainChance, snowAllowed) == kind)
                    count++;
            }
            return count;
        }

        [Test]
        public void SameSlot_SameWeather()
        {
            var first = CreateSchedule();
            var second = CreateSchedule();
            var start = new DateTime(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);

            for (int minute = 0; minute < 24 * 60; minute++)
            {
                DateTime time = start.AddMinutes(minute).AddSeconds(17);
                Assert.AreEqual(first.Get(time, "home", 0.5f, true), second.Get(time, "home", 0.5f, true), time.ToString("u"));
                Assert.AreEqual(first.Get(time, "home", 0.5f, true), first.Get(time, "home", 0.5f, true), time.ToString("u"));
            }
        }

        [Test]
        public void Precipitation_LastsTwoToFourMinutes_InsideOneSlot()
        {
            var schedule = CreateSchedule();
            var start = new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc);

            for (int slot = 0; slot < 144; slot++)
            {
                int rainySeconds = 0;
                for (int second = 0; second < SlotMinutes * 60; second += 5)
                {
                    if (schedule.Get(start.AddMinutes(slot * SlotMinutes).AddSeconds(second), "kazan", 1f, false) == WeatherKind.Rain)
                        rainySeconds += 5;
                }
                Assert.That(rainySeconds, Is.GreaterThanOrEqualTo(115).And.LessThanOrEqualTo(245), "slot " + slot);
            }
        }

        [Test]
        public void NoSnow_InSummer()
        {
            var schedule = CreateSchedule();
            var july = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);

            Assert.AreEqual(0, Count(schedule, july, "moscow", 1f, true, WeatherKind.Snow));
            Assert.Greater(Count(schedule, july, "moscow", 1f, true, WeatherKind.Rain), 0);
        }

        [Test]
        public void Snow_InWinter_WhereAllowed()
        {
            var schedule = CreateSchedule();
            var january = new DateTime(2027, 1, 15, 0, 0, 0, DateTimeKind.Utc);

            Assert.Greater(Count(schedule, january, "moscow", 1f, true, WeatherKind.Snow), 0);
            Assert.AreEqual(0, Count(schedule, january, "moscow", 1f, true, WeatherKind.Rain));
        }

        [Test]
        public void NoSnow_InSochi_EvenInWinter()
        {
            var schedule = CreateSchedule();
            var december = new DateTime(2026, 12, 20, 0, 0, 0, DateTimeKind.Utc);

            Assert.AreEqual(0, Count(schedule, december, "sochi", 1f, false, WeatherKind.Snow));
            Assert.Greater(Count(schedule, december, "sochi", 1f, false, WeatherKind.Rain), 0);
        }

        [Test]
        public void ZeroRainChance_AlwaysClear()
        {
            var schedule = CreateSchedule();
            var january = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var august = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

            Assert.AreEqual(24 * 60, Count(schedule, january, "piter", 0f, true, WeatherKind.Clear));
            Assert.AreEqual(24 * 60, Count(schedule, august, "piter", 0f, true, WeatherKind.Clear));
        }

        [Test]
        public void RainChance_ControlsShareOfRainySlots()
        {
            var schedule = CreateSchedule();
            var start = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
            int rainy = 0;
            const int slots = 6 * 24 * 30;

            for (int slot = 0; slot < slots; slot++)
            {
                for (int minute = 0; minute < SlotMinutes; minute++)
                {
                    if (schedule.Get(start.AddMinutes(slot * SlotMinutes + minute), "home", 0.25f, true) != WeatherKind.Clear)
                    {
                        rainy++;
                        break;
                    }
                }
            }

            Assert.That(rainy / (double)slots, Is.EqualTo(0.25).Within(0.03));
        }

        [Test]
        public void Service_Force_OverridesScheduleAndRaisesEvent()
        {
            var now = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
            var service = new WeatherService(CreateSchedule(), () => now, "home", 0f, true, 1f);
            WeatherKind? changed = null;
            service.WeatherChanged += kind => changed = kind;

            Assert.AreEqual(WeatherKind.Clear, service.Current);
            service.Force(WeatherKind.Snow);

            Assert.AreEqual(WeatherKind.Snow, service.Current);
            Assert.AreEqual(WeatherKind.Snow, changed);
            service.Tick(5f);
            Assert.AreEqual(WeatherKind.Snow, service.Current);
        }
    }
}
