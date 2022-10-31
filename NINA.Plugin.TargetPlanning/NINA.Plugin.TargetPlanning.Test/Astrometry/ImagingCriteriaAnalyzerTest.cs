using FluentAssertions;
using NUnit.Framework;
using System;
using System.Security.Policy;
using TargetPlanning.NINAPlugin.Astrometry;

namespace NINA.Plugin.TargetPlanning.Test.Astrometry {

    public class ImagingCriteriaAnalyzerTest {

        private readonly static DateTime start = DateTime.Now.Date.AddHours(-6); // 6pm
        private readonly static DateTime end = start.AddHours(12); // 6am

        [Test]
        public void TestCtor() {
            ImagingCriteriaAnalyzer analyzer = new ImagingCriteriaAnalyzer(start, end);

            analyzer.StartImagingTime.Should().Be(start);
            analyzer.EndImagingTime.Should().Be(end);
            analyzer.StartLimitingFactor.Equals(ImagingLimit.MinimumAltitude).Should().BeTrue();
            analyzer.EndLimitingFactor.Equals(ImagingLimit.MinimumAltitude).Should().BeTrue();
            analyzer.SessionIsRejected().Should().BeFalse();
        }

        [Test]
        public void TestAdjustForTwilight() {
            ImagingCriteriaAnalyzer analyzer = new ImagingCriteriaAnalyzer(DateTime.MinValue, DateTime.MinValue);
            analyzer.AdjustForTwilight(start, end);

            analyzer.StartImagingTime.Should().Be(start);
            analyzer.EndImagingTime.Should().Be(end);
            analyzer.StartLimitingFactor.Equals(ImagingLimit.Twilight).Should().BeTrue();
            analyzer.EndLimitingFactor.Equals(ImagingLimit.Twilight).Should().BeTrue();
            analyzer.SessionIsRejected().Should().BeFalse();

            analyzer = new ImagingCriteriaAnalyzer(start, end);
            analyzer.AdjustForTwilight(start.AddHours(-1), end.AddHours(1));

            analyzer.StartImagingTime.Should().Be(start);
            analyzer.EndImagingTime.Should().Be(end);
            analyzer.StartLimitingFactor.Equals(ImagingLimit.MinimumAltitude).Should().BeTrue();
            analyzer.EndLimitingFactor.Equals(ImagingLimit.MinimumAltitude).Should().BeTrue();
            analyzer.SessionIsRejected().Should().BeFalse();

            analyzer = new ImagingCriteriaAnalyzer(start, end);
            var ex = Assert.Throws<ArgumentException>(() => analyzer.AdjustForTwilight(end, start));
            ex.Message.Should().Be("imagingDayStartTime must be before imagingDayEndTime");
        }

        [Test]
        public void TestAdjustForMeridianProximityCase1() {
            // Case 1: ------S------M======T======M -> start before the entire span (clip start)
            long meridianProximityTime = 60;
            DateTime transitTime = new DateTime(2022, 10, 22, 0, 0, 0);

            DateTime start = transitTime.AddMinutes(-61);
            DateTime end = transitTime.AddMinutes(59);

            ImagingCriteriaAnalyzer analyzer = new ImagingCriteriaAnalyzer(start, end);
            analyzer.AdjustForMeridianProximity(transitTime, meridianProximityTime);

            analyzer.StartImagingTime.Should().Be(start.AddMinutes(1));
            analyzer.EndImagingTime.Should().Be(end);
            analyzer.StartLimitingFactor.Equals(ImagingLimit.MeridianClip).Should().BeTrue();
            analyzer.EndLimitingFactor.Equals(ImagingLimit.MinimumAltitude).Should().BeTrue();
            analyzer.SessionIsRejected().Should().BeFalse();
        }

        [Test]
        public void TestAdjustForMeridianProximityCase2() {
            long meridianProximityTime = 60;
            DateTime transitTime = new DateTime(2022, 10, 22, 0, 0, 0);

            // Case 2: M======T======M------S------ -> start after the entire span (reject)
            DateTime start = transitTime.AddHours(2);
            DateTime end = transitTime.AddHours(4);

            ImagingCriteriaAnalyzer analyzer = new ImagingCriteriaAnalyzer(start, end);
            analyzer.AdjustForMeridianProximity(transitTime, meridianProximityTime);

            analyzer.StartLimitingFactor.Equals(ImagingLimit.Meridian).Should().BeTrue();
            analyzer.EndLimitingFactor.Equals(ImagingLimit.Meridian).Should().BeTrue();
            analyzer.SessionIsRejected().Should().BeTrue();
        }

        [Test]
        public void TestAdjustForMeridianProximityCase3() {
            long meridianProximityTime = 60;
            DateTime transitTime = new DateTime(2022, 10, 22, 0, 0, 0);

            // Case 3: ------M===S===T======M------ -> start in span, before transit (start no change)
            DateTime start = transitTime.AddMinutes(-59);
            DateTime end = transitTime.AddMinutes(61);

            ImagingCriteriaAnalyzer analyzer = new ImagingCriteriaAnalyzer(start, end);
            analyzer.AdjustForMeridianProximity(transitTime, meridianProximityTime);

            analyzer.StartImagingTime.Should().Be(start);
            analyzer.EndImagingTime.Should().Be(end.AddMinutes(-1));
            analyzer.StartLimitingFactor.Equals(ImagingLimit.MinimumAltitude).Should().BeTrue();
            analyzer.EndLimitingFactor.Equals(ImagingLimit.MeridianClip).Should().BeTrue();
            analyzer.SessionIsRejected().Should().BeFalse();
        }

        [Test]
        public void TestAdjustForMeridianProximityCase4() {
            long meridianProximityTime = 60;
            DateTime transitTime = new DateTime(2022, 10, 22, 0, 0, 0);

            // Case 4: ------M======T===S===M------ -> start in span, after transit (start no change)
            DateTime start = transitTime.AddMinutes(30);
            DateTime end = transitTime.AddHours(2);

            ImagingCriteriaAnalyzer analyzer = new ImagingCriteriaAnalyzer(start, end);
            analyzer.AdjustForMeridianProximity(transitTime, meridianProximityTime);

            analyzer.StartImagingTime.Should().Be(start);
            analyzer.EndImagingTime.Should().Be(transitTime.AddHours(1));
            analyzer.StartLimitingFactor.Equals(ImagingLimit.MinimumAltitude).Should().BeTrue();
            analyzer.EndLimitingFactor.Equals(ImagingLimit.MeridianClip).Should().BeTrue();
            analyzer.SessionIsRejected().Should().BeFalse();
        }

        [Test]
        public void TestAdjustForMeridianProximityCase5() {
            long meridianProximityTime = 60;
            DateTime transitTime = new DateTime(2022, 10, 22, 0, 0, 0);

            // Case 5: ------E------M======T======M -> end before the entire span (reject)
            DateTime start = transitTime.AddHours(-4);
            DateTime end = transitTime.AddHours(-2);

            ImagingCriteriaAnalyzer analyzer = new ImagingCriteriaAnalyzer(start, end);
            analyzer.AdjustForMeridianProximity(transitTime, meridianProximityTime);

            analyzer.StartLimitingFactor.Equals(ImagingLimit.Meridian).Should().BeTrue();
            analyzer.EndLimitingFactor.Equals(ImagingLimit.Meridian).Should().BeTrue();
            analyzer.SessionIsRejected().Should().BeTrue();
        }

        [Test]
        public void TestAdjustForMeridianProximityCase6() {
            long meridianProximityTime = 60;
            DateTime transitTime = new DateTime(2022, 10, 22, 0, 0, 0);

            // Case 6: M======T======M------E------ -> end after the entire span (clip end)
            DateTime start = transitTime.AddHours(-2);
            DateTime end = transitTime.AddHours(2);

            ImagingCriteriaAnalyzer analyzer = new ImagingCriteriaAnalyzer(start, end);
            analyzer.AdjustForMeridianProximity(transitTime, meridianProximityTime);

            analyzer.StartImagingTime.Should().Be(transitTime.AddHours(-1));
            analyzer.EndImagingTime.Should().Be(transitTime.AddHours(1));
            analyzer.StartLimitingFactor.Equals(ImagingLimit.MeridianClip).Should().BeTrue();
            analyzer.EndLimitingFactor.Equals(ImagingLimit.MeridianClip).Should().BeTrue();
            analyzer.SessionIsRejected().Should().BeFalse();
        }

        [Test]
        public void TestAdjustForMeridianProximityCase7() {
            long meridianProximityTime = 60;
            DateTime transitTime = new DateTime(2022, 10, 22, 0, 0, 0);

            // Case 7: ------M===E===T======M------ -> end in span, before transit (end no change)
            DateTime start = transitTime.AddHours(-2);
            DateTime end = transitTime.AddMinutes(-30);

            ImagingCriteriaAnalyzer analyzer = new ImagingCriteriaAnalyzer(start, end);
            analyzer.AdjustForMeridianProximity(transitTime, meridianProximityTime);

            analyzer.StartImagingTime.Should().Be(transitTime.AddHours(-1));
            analyzer.EndImagingTime.Should().Be(end);
            analyzer.StartLimitingFactor.Equals(ImagingLimit.MeridianClip).Should().BeTrue();
            analyzer.EndLimitingFactor.Equals(ImagingLimit.MinimumAltitude).Should().BeTrue();
            analyzer.SessionIsRejected().Should().BeFalse();
        }

        [Test]
        public void TestAdjustForMeridianProximityCase8() {
            long meridianProximityTime = 60;
            DateTime transitTime = new DateTime(2022, 10, 22, 0, 0, 0);

            // Case 8: ------M======T===E===M------ -> end in span, after transit (end no change)
            DateTime start = transitTime.AddMinutes(-61);
            DateTime end = transitTime.AddMinutes(59);

            ImagingCriteriaAnalyzer analyzer = new ImagingCriteriaAnalyzer(start, end);
            analyzer.AdjustForMeridianProximity(transitTime, meridianProximityTime);

            analyzer.StartImagingTime.Should().Be(start.AddMinutes(1));
            analyzer.EndImagingTime.Should().Be(end);
            analyzer.StartLimitingFactor.Equals(ImagingLimit.MeridianClip).Should().BeTrue();
            analyzer.EndLimitingFactor.Equals(ImagingLimit.MinimumAltitude).Should().BeTrue();
            analyzer.SessionIsRejected().Should().BeFalse();
        }

        [Test]
        public void TestAdjustForMeridianProximityNullTransit() {
            ImagingCriteriaAnalyzer analyzer = new ImagingCriteriaAnalyzer(start, end);
            var ex = Assert.Throws<ArgumentException>(() => analyzer.AdjustForMeridianProximity(DateTime.MinValue, 0));
            ex.Message.Should().Be("Transit 'clipped' by start/end twilight?  Need to compute ...");
        }

        [Test]
        public void TestAdjustForMinimumImagingTime() {

            // 11pm - 1am: have 2 hours to image and minimum is 1h -> no reject
            ImagingCriteriaAnalyzer analyzer = new ImagingCriteriaAnalyzer(start.AddHours(5), end.AddHours(-5));
            analyzer.AdjustForMinimumImagingTime(60);

            analyzer.StartImagingTime.Should().Be(start.AddHours(5));
            analyzer.EndImagingTime.Should().Be(end.AddHours(-5));
            analyzer.StartLimitingFactor.Equals(ImagingLimit.MinimumAltitude).Should().BeTrue();
            analyzer.EndLimitingFactor.Equals(ImagingLimit.MinimumAltitude).Should().BeTrue();
            analyzer.SessionIsRejected().Should().BeFalse();

            // 11pm - 1am: have 2 hours to image and minimum is 2h+1m -> reject
            analyzer = new ImagingCriteriaAnalyzer(start.AddHours(5), end.AddHours(-5));
            analyzer.AdjustForMinimumImagingTime(121);

            analyzer.StartLimitingFactor.Equals(ImagingLimit.MinimumTime).Should().BeTrue();
            analyzer.EndLimitingFactor.Equals(ImagingLimit.MinimumTime).Should().BeTrue();
            analyzer.SessionIsRejected().Should().BeTrue();
        }

        [Test]
        public void TestAdjustForMoonIllumination() {
            ImagingCriteriaAnalyzer analyzer = new ImagingCriteriaAnalyzer(start, end);

            analyzer.AdjustForMoonIllumination(.1, .5);
            analyzer.StartLimitingFactor.Equals(ImagingLimit.MinimumAltitude).Should().BeTrue();
            analyzer.EndLimitingFactor.Equals(ImagingLimit.MinimumAltitude).Should().BeTrue();
            analyzer.SessionIsRejected().Should().BeFalse();

            analyzer.AdjustForMoonIllumination(.51, .5);
            analyzer.StartLimitingFactor.Equals(ImagingLimit.MoonIllumination).Should().BeTrue();
            analyzer.EndLimitingFactor.Equals(ImagingLimit.MoonIllumination).Should().BeTrue();
            analyzer.SessionIsRejected().Should().BeTrue();
        }

        [Test]
        public void TestAdjustForMoonSeparation() {
            ImagingCriteriaAnalyzer analyzer = new ImagingCriteriaAnalyzer(start, end);

            analyzer.AdjustForMoonSeparation(100, 40);
            analyzer.StartLimitingFactor.Equals(ImagingLimit.MinimumAltitude).Should().BeTrue();
            analyzer.EndLimitingFactor.Equals(ImagingLimit.MinimumAltitude).Should().BeTrue();
            analyzer.SessionIsRejected().Should().BeFalse();

            analyzer.AdjustForMoonSeparation(39.9, 40);
            analyzer.StartLimitingFactor.Equals(ImagingLimit.MoonSeparation).Should().BeTrue();
            analyzer.EndLimitingFactor.Equals(ImagingLimit.MoonSeparation).Should().BeTrue();
            analyzer.SessionIsRejected().Should().BeTrue();
        }
    }

}
