using FluentAssertions;
using Moq;
using NINA.Astrometry;
using NINA.Astrometry.Interfaces;
using NUnit.Framework;
using System;
using System.Security.Cryptography;
using TargetPlanning.NINAPlugin.Astrometry;
using static Google.Protobuf.Reflection.SourceCodeInfo.Types;
using static NINA.Image.FileFormat.XISF.XISFImageProperty.Observation;

namespace TargetPlanning.NINAPlugin.Test.Astrometry {

    public class AstrometryUtilsTest {

        public static readonly ObserverInfo TEST_LOCATION_1, TEST_LOCATION_2, TEST_LOCATION_3;

        static AstrometryUtilsTest() {

            // Northern hemisphere
            TEST_LOCATION_1 = new ObserverInfo();
            TEST_LOCATION_1.Latitude = 35;
            TEST_LOCATION_1.Longitude = -79;
            TEST_LOCATION_1.Elevation = 165;

            // Southern hemisphere
            TEST_LOCATION_2 = new ObserverInfo();
            TEST_LOCATION_2.Latitude = -35;
            TEST_LOCATION_2.Longitude = -80;
            TEST_LOCATION_2.Elevation = 165;

            // Northern hemisphere, above artic circle
            TEST_LOCATION_3 = new ObserverInfo();
            TEST_LOCATION_3.Latitude = 67;
            TEST_LOCATION_3.Longitude = -80;
            TEST_LOCATION_3.Elevation = 165;
        }

        [Test]
        [TestCase("5:55:11", "7:24:30", 2022, 10, 27, 22, 25, 0, 0.0792, 80.9998)] // Betelgeuse rise
        [TestCase("5:55:11", "7:24:30", 2022, 10, 28, 4, 46, 20, 62.4049, 181.0032)] // Betelgeuse transit
        [TestCase("5:55:11", "7:24:30", 2022, 10, 28, 11, 5, 36, -0.2597, 279.2407)] // Betelgeuse set
        public void TestGetHorizontalCoordinates(string ra, string dec, int yr, int mon, int day, int hh, int mm, int ss, double expectedAlt, double expectedAz) {
            DateTime atTime = new DateTime(yr, mon, day, hh, mm, ss);
            Coordinates coordinates = new Coordinates(AstroUtil.HMSToDegrees(ra), AstroUtil.DMSToDegrees(dec), Epoch.J2000, Coordinates.RAType.Degrees);

            HorizontalCoordinate hc = AstrometryUtils.GetHorizontalCoordinates(TEST_LOCATION_1, coordinates, atTime);
            hc.Altitude.Should().BeApproximately(expectedAlt, 0.001);
            hc.Azimuth.Should().BeApproximately(expectedAz, 0.001);
        }

        [Test]
        [TestCase(0, -50, true)]
        [TestCase(0, -56, false)]
        [TestCase(0, 80, true)]
        public void testRisesAtLocationNorthHemisphere(double ra, double dec, bool expected) {
            Coordinates coordinates = new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Degrees);
            AstrometryUtils.RisesAtLocation(TEST_LOCATION_1, coordinates).Should().Be(expected);
        }

        [Test]
        [TestCase(0, -50, true)]
        [TestCase(0, 56, false)]
        [TestCase(0, -80, true)]
        public void testRisesAtLocationSouthHemisphere(double ra, double dec, bool expected) {
            Coordinates coordinates = new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Degrees);
            AstrometryUtils.RisesAtLocation(TEST_LOCATION_2, coordinates).Should().Be(expected);
        }

        [Test]
        [TestCase(0, 56, true)]
        [TestCase(0, 54, false)]
        public void testCircumpolarAtLocationNorthHemisphere(double ra, double dec, bool expected) {
            Coordinates coordinates = new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Degrees);
            AstrometryUtils.CircumpolarAtLocation(TEST_LOCATION_1, coordinates).Should().Be(expected);
        }

        [Test]
        [TestCase(0, -56, true)]
        [TestCase(0, -54, false)]
        public void testCircumpolarAtLocationSouthHemisphere(double ra, double dec, bool expected) {
            Coordinates coordinates = new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Degrees);
            AstrometryUtils.CircumpolarAtLocation(TEST_LOCATION_2, coordinates).Should().Be(expected);
        }

        [Test]
        [TestCase(0, 66, 10, true)]
        [TestCase(0, 56, 10, false)]
        public void testCircumpolarAtLocationWithMinimumAltNorthHemisphere(double ra, double dec, double minAlt, bool expected) {
            Coordinates coordinates = new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Degrees);
            AstrometryUtils.CircumpolarAtLocationWithMinimumAltitude(TEST_LOCATION_1, coordinates, minAlt).Should().Be(expected);
        }

        [Test]
        [TestCase(0, -66, 10, true)]
        [TestCase(0, -56, 10, false)]
        public void testCircumpolarAtLocationWithMinimumAltSouthHemisphere(double ra, double dec, double minAlt, bool expected) {
            Coordinates coordinates = new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Degrees);
            AstrometryUtils.CircumpolarAtLocationWithMinimumAltitude(TEST_LOCATION_2, coordinates, minAlt).Should().Be(expected);
        }

        [Test]
        public void testIsAbovePolarCircle() {
            AstrometryUtils.IsAbovePolarCircle(TEST_LOCATION_1).Should().BeFalse();
            AstrometryUtils.IsAbovePolarCircle(TEST_LOCATION_3).Should().BeTrue();
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
