
using FluentAssertions;
using NUnit.Framework;
using System;
using TargetPlanning.NINAPlugin.Astrometry;

namespace NINA.Plugin.TargetPlanning.Test.Astrometry {

    public class ImagingDayPlanTest {

        [Test]
        public void Test() {
            DateTime start = DateTime.Now;
            DateTime transit = start.AddHours(4);
            DateTime end = start.AddHours(8);

            ImagingDayPlan plan = new ImagingDayPlan(start, end, transit, ImagingLimit.Twilight, ImagingLimit.MinimumAltitude, .7, 10, double.MinValue);

            plan.StartImagingTime.Should().Be(start);
            plan.TransitTime.Should().Be(transit);
            plan.EndImagingTime.Should().Be(end);

            plan.StartLimitingFactor.Equals(ImagingLimit.Twilight).Should().BeTrue();
            plan.EndLimitingFactor.Equals(ImagingLimit.MinimumAltitude).Should().BeTrue();

            plan.MoonIllumination.Should().BeApproximately(0.7, 0.001);
            plan.MoonSeparation.Should().BeApproximately(10, 0.001);

            plan.NotVisible().Should().BeFalse();
            plan.NotAboveMinimumImagingTime().Should().BeFalse();

            plan.StartDay.Should().Be(start.Date);
            plan.EndDay.Should().Be(end.Date);

            plan.StartDay = start.AddDays(1);
            plan.EndDay = start.AddDays(2);
            plan.StartDay.Should().Be(start.AddDays(1).Date);
            plan.EndDay.Should().Be(end.AddDays(2).Date);
        }

        [Test]
        public void TestNotVisible() {
            DateTime start = DateTime.Now;
            DateTime transit = start.AddHours(4);
            DateTime end = start.AddHours(8);

            ImagingDayPlan plan = new ImagingDayPlan(start, end, transit, ImagingLimit.NotVisible, ImagingLimit.NotVisible, 0, 0, double.MinValue);

            plan.StartLimitingFactor.Equals(ImagingLimit.NotVisible).Should().BeTrue();
            plan.EndLimitingFactor.Equals(ImagingLimit.NotVisible).Should().BeTrue();

            plan.IsAccepted().Should().BeFalse();
            plan.NotVisible().Should().BeTrue();
        }

        [Test]
        public void TestNotAboveMinimumImagingTime() {
            DateTime start = DateTime.Now;
            DateTime transit = start.AddHours(4);
            DateTime end = start.AddHours(8);
            ImagingDayPlan plan = new ImagingDayPlan(start, end, transit, ImagingLimit.MinimumTime,
                                                     ImagingLimit.MinimumTime, 0, 0, double.MinValue);
            plan.IsAccepted().Should().BeFalse();
            plan.NotAboveMinimumImagingTime().Should().BeTrue();
        }

        [Test]
        public void TestGetImagingMinutes() {
            DateTime start = new DateTime(2022, 10, 31, 18, 0, 0);
            DateTime transit = start.AddHours(6);
            DateTime end = start.AddHours(12);

            ImagingDayPlan plan = new ImagingDayPlan(start, end, transit, ImagingLimit.Twilight, ImagingLimit.Twilight, 0, 0, double.MinValue);
            plan.GetImagingMinutes().Should().Be(12 * 60);
        }

        [Test]
        public void TestAboveMaximumMoonIllumination() {
            DateTime start = new DateTime(2022, 10, 31, 18, 0, 0);
            DateTime transit = start.AddHours(6);
            DateTime end = start.AddHours(12);

            ImagingDayPlan plan = new ImagingDayPlan(start, end, transit, ImagingLimit.MoonIllumination, ImagingLimit.MoonIllumination, 0, 0, double.MinValue);
            plan.IsAccepted().Should().BeFalse();
            plan.AboveMaximumMoonIllumination().Should().BeTrue();

            plan = new ImagingDayPlan(start, end, transit, ImagingLimit.Twilight, ImagingLimit.Twilight, 0, 0, double.MinValue);
            plan.IsAccepted().Should().BeTrue();
            plan.AboveMaximumMoonIllumination().Should().BeFalse();
        }

        [Test]
        public void TestBelowMinimumMoonSeparation() {
            DateTime start = new DateTime(2022, 10, 31, 18, 0, 0);
            DateTime transit = start.AddHours(6);
            DateTime end = start.AddHours(12);

            ImagingDayPlan plan = new ImagingDayPlan(start, end, transit, ImagingLimit.MoonSeparation, ImagingLimit.MoonSeparation, 0, 0, double.MinValue);
            plan.IsAccepted().Should().BeFalse();
            plan.BelowMinimumMoonSeparation().Should().BeTrue();

            plan = new ImagingDayPlan(start, end, transit, ImagingLimit.Twilight, ImagingLimit.Twilight, 0, 0, double.MinValue);
            plan.IsAccepted().Should().BeTrue();
            plan.BelowMinimumMoonSeparation().Should().BeFalse();
        }

        [Test]
        public void TestBad() {
            DateTime start = new DateTime(2022, 10, 31, 18, 0, 0);
            DateTime transit = start.AddHours(6);
            DateTime end = start.AddHours(12);

            var ex = Assert.Throws<ArgumentException>(() => new ImagingDayPlan(start, end, transit, null, null, 0, 0, double.MinValue));
            ex.Message.Should().Be("startLimitingFactor cannot be null");

            ex = Assert.Throws<ArgumentException>(() => new ImagingDayPlan(start, end, transit, ImagingLimit.Twilight, null, 0, 0, double.MinValue));
            ex.Message.Should().Be("endLimitingFactor cannot be null");
        }
    }

}
