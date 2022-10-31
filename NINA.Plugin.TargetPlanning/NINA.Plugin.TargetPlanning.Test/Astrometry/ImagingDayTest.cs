using FluentAssertions;
using NINA.Astrometry;
using NINA.Plugin.TargetPlanning.Test.Astrometry;
using NUnit.Framework;
using System;
using TargetPlanning.NINAPlugin.Astrometry;

namespace TargetPlanning.NINAPlugin.Test.Astrometry {

    public class ImagingDayTest {

        public void TestIsEverAboveMinimumAltitude() {

            DateTime start = new DateTime(2022, 10, 15, 18, 0, 0);
            DateTime end = new DateTime(2022, 10, 16, 6, 0, 0);

            ImagingDay imagingDay = new ImagingDay(start, end, TestUtil.TEST_LOCATION_1, TestUtil.BETELGEUSE);

            Altitudes samplePositions = imagingDay.SamplePositions;
            samplePositions.Should().NotBeNull();
            samplePositions.AltitudeList.Count.Should().Be(146);

            // Betelgeuse is up in min Oct - but not above 62
            imagingDay.IsEverAboveMinimumAltitude(10).Should().BeTrue();
            imagingDay.IsEverAboveMinimumAltitude(62).Should().BeFalse();

            // Spica is not up at all in Oct
            imagingDay = new ImagingDay(start, end, TestUtil.TEST_LOCATION_1, TestUtil.SPICA);
            imagingDay.IsEverAboveMinimumAltitude(10).Should().BeFalse();

            start = new DateTime(2022, 6, 25, 21, 0, 0);
            end = new DateTime(2022, 6, 26, 5, 0, 0);

            // Betelgeuse is not up in June
            imagingDay = new ImagingDay(start, end, TestUtil.TEST_LOCATION_1, TestUtil.BETELGEUSE);
            imagingDay.IsEverAboveMinimumAltitude(20).Should().BeFalse();

            // Spica is up in June - but not above 43
            imagingDay = new ImagingDay(start, end, TestUtil.TEST_LOCATION_1, TestUtil.SPICA);
            imagingDay.IsEverAboveMinimumAltitude(20).Should().BeTrue();
            imagingDay.IsEverAboveMinimumAltitude(43).Should().BeFalse();
        }

        [Test]
        public void TestTransitTime() {
            DateTime start = new DateTime(2022, 10, 15, 18, 0, 0);
            DateTime end = new DateTime(2022, 10, 16, 6, 0, 0);

            ImagingDay imagingDay = new ImagingDay(start, end, TestUtil.TEST_LOCATION_1, TestUtil.BETELGEUSE);
            DateTime transitTime = imagingDay.GetTransitTime();
            TestUtil.AssertTime(end, transitTime, 5, 31, 39);
        }

        [Test]
        public void TestGetRiseAboveMinimumTime() {
            DateTime start = new DateTime(2022, 10, 15, 18, 0, 0);
            DateTime end = new DateTime(2022, 10, 16, 6, 0, 0);

            ImagingDay imagingDay = new ImagingDay(start, end, TestUtil.TEST_LOCATION_1, TestUtil.BETELGEUSE);
            DateTime riseAboveMinimumTime = imagingDay.GetRiseAboveMinimumTime(20);
            TestUtil.AssertTime(end, riseAboveMinimumTime, 0, 49, 29);

            // Betelgeuse doesn't rise above 20 on 8/7 until 5:28am
            start = new DateTime(2022, 8, 7, 18, 0, 0);
            end = new DateTime(2022, 8, 8, 5, 20, 0);

            imagingDay = new ImagingDay(start, end, TestUtil.TEST_LOCATION_1, TestUtil.BETELGEUSE);
            riseAboveMinimumTime = imagingDay.GetRiseAboveMinimumTime(20);
            riseAboveMinimumTime.Should().Be(DateTime.MinValue);
        }

        [Test]
        public void TestGetSetBelowMinimumTime() {
            DateTime start = new DateTime(2022, 3, 15, 18, 0, 0);
            DateTime end = new DateTime(2022, 3, 16, 6, 0, 0);

            ImagingDay imagingDay = new ImagingDay(start, end, TestUtil.TEST_LOCATION_1, TestUtil.BETELGEUSE);
            DateTime setBelowMinimumTime = imagingDay.GetSetBelowMinimumTime(20);
            TestUtil.AssertTime(end, setBelowMinimumTime, 0, 19, 8);

            // Betelgeuse doesn't set below minimum during this interval
            start = new DateTime(2022, 10, 15, 18, 0, 0);
            end = new DateTime(2022, 10, 16, 6, 0, 0);

            imagingDay = new ImagingDay(start, end, TestUtil.TEST_LOCATION_1, TestUtil.BETELGEUSE);
            setBelowMinimumTime = imagingDay.GetSetBelowMinimumTime(20);
            setBelowMinimumTime.Should().Be(DateTime.MinValue);
        }

        [Test]
        public void TestCircumpolarNorth() {
            DateTime start = new DateTime(2022, 10, 15, 18, 0, 0);
            DateTime end = new DateTime(2022, 10, 16, 6, 0, 0);
            ImagingDay imagingDay = new ImagingDay(start, end, TestUtil.TEST_LOCATION_1, TestUtil.STAR_NORTH_CIRCP);

            imagingDay.GetRiseAboveMinimumTime(20).Should().Be(DateTime.MinValue);

            TestUtil.AssertTime(start, imagingDay.GetTransitTime(), 23, 37, 26);
            imagingDay.GetSetBelowMinimumTime(20).Should().Be(DateTime.MinValue);
        }

        [Test]
        public void TestCircumpolarSouth() {
            DateTime start = new DateTime(2022, 10, 15, 18, 0, 0);
            DateTime end = new DateTime(2022, 10, 16, 6, 0, 0);
            ImagingDay imagingDay = new ImagingDay(start, end, TestUtil.TEST_LOCATION_2, TestUtil.STAR_SOUTH_CIRCP);

            imagingDay.GetRiseAboveMinimumTime(20).Should().Be(DateTime.MinValue);
            TestUtil.AssertTime(start, imagingDay.GetTransitTime(), 23, 41, 25);
            imagingDay.GetSetBelowMinimumTime(20).Should().Be(DateTime.MinValue);
        }

        [Test]
        public void TestNeverRisesNorth() {
            DateTime start = new DateTime(2022, 10, 15, 18, 0, 0);
            DateTime end = new DateTime(2022, 10, 16, 6, 0, 0);
            ImagingDay imagingDay = new ImagingDay(start, end, TestUtil.TEST_LOCATION_1, TestUtil.STAR_SOUTH_CIRCP);

            imagingDay.GetRiseAboveMinimumTime(20).Should().Be(DateTime.MinValue);
            imagingDay.GetTransitTime().Should().Be(DateTime.MinValue);
            imagingDay.GetSetBelowMinimumTime(20).Should().Be(DateTime.MinValue);
        }

        [Test]
        public void TestNeverRisesSouth() {
            DateTime start = new DateTime(2022, 10, 15, 18, 0, 0);
            DateTime end = new DateTime(2022, 10, 16, 6, 0, 0);
            ImagingDay imagingDay = new ImagingDay(start, end, TestUtil.TEST_LOCATION_2, TestUtil.STAR_NORTH_CIRCP);

            imagingDay.GetRiseAboveMinimumTime(20).Should().Be(DateTime.MinValue);
            imagingDay.GetTransitTime().Should().Be(DateTime.MinValue);
            imagingDay.GetSetBelowMinimumTime(20).Should().Be(DateTime.MinValue);
        }

        [Test]
        public void TestSetBelowMinIssue() {
            DateTime start = new DateTime(2022, 12, 23, 19, 0, 0);
            DateTime end = new DateTime(2022, 12, 24, 5, 0, 0);
            ImagingDay imagingDay = new ImagingDay(start, end, TestUtil.TEST_LOCATION_1, TestUtil.BETELGEUSE);

            TestUtil.AssertTime(start, imagingDay.GetRiseAboveMinimumTime(20), 19, 18, 11);
            TestUtil.AssertTime(end, imagingDay.GetTransitTime(), 0, 0, 21);
            TestUtil.AssertTime(end, imagingDay.GetSetBelowMinimumTime(20), 4, 42, 29);
        }

        [Test]
        public void TestBad() {
            DateTime dt = DateTime.Now;

            var ex = Assert.Throws<ArgumentException>(() => new ImagingDay(dt, dt, null, null));
            ex.Message.Should().Be("location cannot be null");

            ObserverInfo oi = new ObserverInfo();
            ex = Assert.Throws<ArgumentException>(() => new ImagingDay(dt, dt, oi, null));
            ex.Message.Should().Be("target cannot be null");
        }
    }

}

