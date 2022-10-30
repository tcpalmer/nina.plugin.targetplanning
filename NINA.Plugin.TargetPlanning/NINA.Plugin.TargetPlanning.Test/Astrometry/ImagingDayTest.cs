using FluentAssertions;
using NINA.Astrometry;
using NUnit.Framework;
using System;
using System.Security.Policy;
using TargetPlanning.NINAPlugin.Astrometry;

namespace TargetPlanning.NINAPlugin.Test.Astrometry {

    public class ImagingDayTest {

        private Coordinates betelgeuse = new Coordinates(AstroUtil.HMSToDegrees("5:55:11"), AstroUtil.DMSToDegrees("7:24:30"), Epoch.J2000, Coordinates.RAType.Degrees);
        private Coordinates spica = new Coordinates(AstroUtil.HMSToDegrees("13:26:25.92"), AstroUtil.DMSToDegrees("-11:17:2.6"), Epoch.J2000, Coordinates.RAType.Degrees);

        public void testIsEverAboveMinimumAltitude() {

            DateTime start = new DateTime(2022, 10, 15, 18, 0, 0);
            DateTime end = new DateTime(2022, 10, 16, 6, 0, 0);

            ImagingDay imagingDay = new ImagingDay(start, end, AstrometryUtilsTest.TEST_LOCATION_1, betelgeuse);

            Altitudes samplePositions = imagingDay.SamplePositions;
            samplePositions.Should().NotBeNull();
            samplePositions.AltitudeList.Count.Should().Be(146);

            // Betelgeuse is up in min Oct - but not above 62
            imagingDay.IsEverAboveMinimumAltitude(10).Should().BeTrue();
            imagingDay.IsEverAboveMinimumAltitude(62).Should().BeFalse();

            // Spica is not up at all in Oct
            imagingDay = new ImagingDay(start, end, AstrometryUtilsTest.TEST_LOCATION_1, spica);
            imagingDay.IsEverAboveMinimumAltitude(10).Should().BeFalse();

            start = new DateTime(2022, 6, 25, 21, 0, 0);
            end = new DateTime(2022, 6, 26, 5, 0, 0);

            // Betelgeuse is not up in June
            imagingDay = new ImagingDay(start, end, AstrometryUtilsTest.TEST_LOCATION_1, betelgeuse);
            imagingDay.IsEverAboveMinimumAltitude(20).Should().BeFalse();

            // Spica is up in June - but not above 43
            imagingDay = new ImagingDay(start, end, AstrometryUtilsTest.TEST_LOCATION_1, spica);
            imagingDay.IsEverAboveMinimumAltitude(20).Should().BeTrue();
            imagingDay.IsEverAboveMinimumAltitude(43).Should().BeFalse();
        }

        [Test]
        public void testTransitTime() {
            DateTime start = new DateTime(2022, 10, 15, 18, 0, 0);
            DateTime end = new DateTime(2022, 10, 16, 6, 0, 0);

            ImagingDay imagingDay = new ImagingDay(start, end, AstrometryUtilsTest.TEST_LOCATION_1, betelgeuse);
            DateTime transitTime = imagingDay.getTransitTime();
            assertTime(end, transitTime, 5, 31, 39);
        }

        [Test]
        public void testGetRiseAboveMinimumTime() {
            DateTime start = new DateTime(2022, 10, 15, 18, 0, 0);
            DateTime end = new DateTime(2022, 10, 16, 6, 0, 0);

            ImagingDay imagingDay = new ImagingDay(start, end, AstrometryUtilsTest.TEST_LOCATION_1, betelgeuse);
            DateTime riseAboveMinimumTime = imagingDay.GetRiseAboveMinimumTime(20);
            assertTime(end, riseAboveMinimumTime, 0, 49, 29);

            // Betelgeuse doesn't rise above 20 on 8/7 until 5:28am
            start = new DateTime(2022, 8, 7, 18, 0, 0);
            end = new DateTime(2022, 8, 8, 5, 20, 0);

            imagingDay = new ImagingDay(start, end, AstrometryUtilsTest.TEST_LOCATION_1, betelgeuse);
            riseAboveMinimumTime = imagingDay.GetRiseAboveMinimumTime(20);
            riseAboveMinimumTime.Should().Be(DateTime.MinValue);
        }

        /*
        public void testGetSetBelowMinimumTime() {
            DateTime start = new DateTime(2022, 3, 15, 18, 0, 0);
            DateTime end = new DateTime(2022, 3, 16, 6, 0, 0);
            ImagingDay imagingDay = new ImagingDay(start, end, TestDSOs.STAR_BETELGEUSE, northernHemisphere, quality);
            DateTime setBelowMinimumTime = imagingDay.getSetBelowMinimumTime(20);
            assertTime(end.truncatedTo(ChronoUnit.DAYS), setBelowMinimumTime, 0, 20, 51);

            // Betelgeuse doesn't set below minimum during this internal
            start = new DateTime(2022, 10, 15, 18, 0, 0);
            end = new DateTime(2022, 10, 16, 6, 0, 0);

            imagingDay = new ImagingDay(start, end, TestDSOs.STAR_BETELGEUSE, northernHemisphere, quality);
            setBelowMinimumTime = imagingDay.getSetBelowMinimumTime(20);
            assertNull(setBelowMinimumTime);
        }

        @Test
        public void testCircumpolarNorth() {
            DateTime start = new DateTime(2022, 10, 15, 18, 0, 0);
            DateTime end = new DateTime(2022, 10, 16, 6, 0, 0);
            ImagingDay imagingDay = new ImagingDay(start, end, TestDSOs.STAR_NORTH_CIRCUMPOLAR, northernHemisphere,
                                                   quality);

            assertNull(imagingDay.getRiseAboveMinimumTime(20));
            assertTime(start.truncatedTo(ChronoUnit.DAYS), imagingDay.getTransitTime(), 23, 38, 6);
            assertNull(imagingDay.getSetBelowMinimumTime(20));
        }

        @Test
        public void testCircumpolarSouth() {
            DateTime start = new DateTime(2022, 10, 15, 18, 0, 0);
            DateTime end = new DateTime(2022, 10, 16, 6, 0, 0);
            ImagingDay imagingDay = new ImagingDay(start, end, TestDSOs.STAR_SOUTH_CIRCUMPOLAR, southernHemisphere,
                                                   quality);

            assertNull(imagingDay.getRiseAboveMinimumTime(20));
            assertTime(start.truncatedTo(ChronoUnit.DAYS), imagingDay.getTransitTime(), 23, 41, 26);
            assertNull(imagingDay.getSetBelowMinimumTime(20));
        }

        @Test
        public void testNeverRisesNorth() {
            DateTime start = new DateTime(2022, 10, 15, 18, 0, 0);
            DateTime end = new DateTime(2022, 10, 16, 6, 0, 0);
            ImagingDay imagingDay = new ImagingDay(start, end, TestDSOs.STAR_SOUTH_CIRCUMPOLAR, northernHemisphere,
                                                   quality);

            assertNull(imagingDay.getRiseAboveMinimumTime(20));
            assertNull(imagingDay.getTransitTime());
            assertNull(imagingDay.getSetBelowMinimumTime(20));
        }

        @Test
        public void testNeverRisesSouth() {
            DateTime start = new DateTime(2022, 10, 15, 18, 0, 0);
            DateTime end = new DateTime(2022, 10, 16, 6, 0, 0);
            ImagingDay imagingDay = new ImagingDay(start, end, TestDSOs.STAR_NORTH_CIRCUMPOLAR, southernHemisphere,
                                                   quality);

            assertNull(imagingDay.getRiseAboveMinimumTime(20));
            assertNull(imagingDay.getTransitTime());
            assertNull(imagingDay.getSetBelowMinimumTime(20));
        }

        @Test
        public void testSetBelowMinIssue() {
            DateTime start = new DateTime(2022, 12, 23, 19, 0, 0);
            DateTime end = new DateTime(2022, 12, 24, 5, 0, 0);
            ImagingDay imagingDay = new ImagingDay(start, end, TestDSOs.STAR_BETELGEUSE, northernHemisphere, quality);

            assertTime(start.truncatedTo(ChronoUnit.DAYS), imagingDay.getRiseAboveMinimumTime(20), 19, 20, 13);
            assertTime(end.truncatedTo(ChronoUnit.DAYS), imagingDay.getTransitTime(), 0, 2, 14);
            assertTime(end.truncatedTo(ChronoUnit.DAYS), imagingDay.getSetBelowMinimumTime(20), 4, 44, 13);
        }
         */

        [Test]
        public void TestBad() {

            DateTime dt = DateTime.Now;

            var ex = Assert.Throws<ArgumentException>(() => new ImagingDay(dt, dt, null, null));
            ex.Message.Should().Be("location cannot be null");

            ObserverInfo oi = new ObserverInfo();
            ex = Assert.Throws<ArgumentException>(() => new ImagingDay(dt, dt, oi, null));
            ex.Message.Should().Be("target cannot be null");
        }

        private static void assertTime(DateTime expected, DateTime actual, int hours, int minutes, int seconds) {
            DateTime edt = expected.Date.AddHours(hours).AddMinutes(minutes).AddSeconds(seconds);
            DateTime adt = new DateTime(actual.Year, actual.Month, actual.Day, actual.Hour, actual.Minute, actual.Second);
            bool cond = edt == adt;
            cond.Should().BeTrue();
        }
    }

}

