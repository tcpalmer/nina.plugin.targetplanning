using FluentAssertions;
using Moq;
using NINA.Astrometry;
using NINA.Astrometry.Interfaces;
using NINA.Plugin.TargetPlanning.Test.Astrometry;
using NUnit.Framework;
using System;
using System.Security.Cryptography;
using System.Security.Policy;
using TargetPlanning.NINAPlugin.Astrometry;
using static Google.Protobuf.Reflection.SourceCodeInfo.Types;
using static NINA.Image.FileFormat.XISF.XISFImageProperty.Observation;

namespace TargetPlanning.NINAPlugin.Test.Astrometry {

    public class AstrometryUtilsTest {

        [Test]
        [TestCase("5:55:11", "7:24:30", 2022, 10, 27, 22, 25, 0, 0.0792, 80.9998)] // Betelgeuse rise
        [TestCase("5:55:11", "7:24:30", 2022, 10, 28, 4, 46, 20, 62.4049, 181.0032)] // Betelgeuse transit
        [TestCase("5:55:11", "7:24:30", 2022, 10, 28, 11, 5, 36, -0.2597, 279.2407)] // Betelgeuse set
        public void TestGetHorizontalCoordinates(string ra, string dec, int yr, int mon, int day, int hh, int mm, int ss, double expectedAlt, double expectedAz) {
            DateTime atTime = new DateTime(yr, mon, day, hh, mm, ss);
            Coordinates coordinates = new Coordinates(AstroUtil.HMSToDegrees(ra), AstroUtil.DMSToDegrees(dec), Epoch.J2000, Coordinates.RAType.Degrees);

            HorizontalCoordinate hc = AstrometryUtils.GetHorizontalCoordinates(TestUtil.TEST_LOCATION_1, coordinates, atTime);
            hc.Altitude.Should().BeApproximately(expectedAlt, 0.001);
            hc.Azimuth.Should().BeApproximately(expectedAz, 0.001);
        }

        [Test]
        [TestCase(1, 2, 37, 0.503)] // 1st quarter
        [TestCase(8, 6, 2, 1.0)] // full (also a TLE)
        [TestCase(16, 8, 27, 0.508)] // 3rd quarter
        [TestCase(23, 17, 57, 0.0)] // new
        public void TestGetMoonIllumination(int day, int hour, int min, double expected) {
            DateTime dateTime = new DateTime(2022, 11, day, hour, min, 0);
            double moonIllumination = AstrometryUtils.GetMoonIllumination(dateTime);
            moonIllumination.Should().BeApproximately(expected, 0.001);
        }

        [Test]
        [TestCase(9, 0, 7, 37.118)]
        [TestCase(12, 0, 4, 20.155)]
        [TestCase(16, 0, 7, 55.375)]
        public void TestGetMoonSeparationAngle(int day, int hour, int min, double expected) {
            DateTime dateTime = new DateTime(2022, 11, day, hour, min, 0);
            double moonSeparation = AstrometryUtils.GetMoonSeparationAngle(TestUtil.TEST_LOCATION_1, dateTime, TestUtil.BETELGEUSE);
            moonSeparation.Should().BeApproximately(expected, 0.001);
        }

        [Test]
        [TestCase(0, -50, true)]
        [TestCase(0, -56, false)]
        [TestCase(0, 80, true)]
        public void TestRisesAtLocationNorthHemisphere(double ra, double dec, bool expected) {
            Coordinates coordinates = new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Degrees);
            AstrometryUtils.RisesAtLocation(TestUtil.TEST_LOCATION_1, coordinates).Should().Be(expected);
        }

        [Test]
        [TestCase(0, -50, true)]
        [TestCase(0, 56, false)]
        [TestCase(0, -80, true)]
        public void TestRisesAtLocationSouthHemisphere(double ra, double dec, bool expected) {
            Coordinates coordinates = new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Degrees);
            AstrometryUtils.RisesAtLocation(TestUtil.TEST_LOCATION_2, coordinates).Should().Be(expected);
        }

        [Test]
        [TestCase(0, 56, true)]
        [TestCase(0, 54, false)]
        public void TestCircumpolarAtLocationNorthHemisphere(double ra, double dec, bool expected) {
            Coordinates coordinates = new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Degrees);
            AstrometryUtils.CircumpolarAtLocation(TestUtil.TEST_LOCATION_1, coordinates).Should().Be(expected);
        }

        [Test]
        [TestCase(0, -56, true)]
        [TestCase(0, -54, false)]
        public void TestCircumpolarAtLocationSouthHemisphere(double ra, double dec, bool expected) {
            Coordinates coordinates = new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Degrees);
            AstrometryUtils.CircumpolarAtLocation(TestUtil.TEST_LOCATION_2, coordinates).Should().Be(expected);
        }

        [Test]
        [TestCase(0, 66, 10, true)]
        [TestCase(0, 56, 10, false)]
        public void TestCircumpolarAtLocationWithMinimumAltNorthHemisphere(double ra, double dec, double minAlt, bool expected) {
            Coordinates coordinates = new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Degrees);
            AstrometryUtils.CircumpolarAtLocationWithMinimumAltitude(TestUtil.TEST_LOCATION_1, coordinates, minAlt).Should().Be(expected);
        }

        [Test]
        [TestCase(0, -66, 10, true)]
        [TestCase(0, -56, 10, false)]
        public void TestCircumpolarAtLocationWithMinimumAltSouthHemisphere(double ra, double dec, double minAlt, bool expected) {
            Coordinates coordinates = new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Degrees);
            AstrometryUtils.CircumpolarAtLocationWithMinimumAltitude(TestUtil.TEST_LOCATION_2, coordinates, minAlt).Should().Be(expected);
        }

        [Test]
        public void TestIsAbovePolarCircle() {
            AstrometryUtils.IsAbovePolarCircle(TestUtil.TEST_LOCATION_1).Should().BeFalse();
            AstrometryUtils.IsAbovePolarCircle(TestUtil.TEST_LOCATION_3).Should().BeTrue();
        }

        [Test]
        public void TestBad() {

            DateTime dt = DateTime.Now;

            var ex = Assert.Throws<ArgumentException>(() => AstrometryUtils.GetHorizontalCoordinates(null, null, dt));
            ex.Message.Should().Be("location cannot be null");

            ObserverInfo oi = new ObserverInfo();
            ex = Assert.Throws<ArgumentException>(() => AstrometryUtils.GetHorizontalCoordinates(oi, null, dt));
            ex.Message.Should().Be("coordinates cannot be null");
        }
    }

}
