using FluentAssertions;
using Moq;
using NINA.Astrometry;
using NINA.Astrometry.Interfaces;
using NUnit.Framework;
using System;
using TargetPlanning.NINAPlugin.Astrometry;
using static Google.Protobuf.Reflection.SourceCodeInfo.Types;
using static NINA.Image.FileFormat.XISF.XISFImageProperty.Observation;

namespace TargetPlanning.NINAPlugin.Test.Astrometry {

    public class AstrometryUtilsTest {

        [Test]
        [TestCase("5:55:11", "7:24:30", 2022, 10, 28, 4, 46, 20, 61.5540, 180.6349)] // Betelgeuse transit
        public void TestGetHorizontalCoordinates(string ra, string dec, int yr, int mon, int day, int hh, int mm, int ss, double expectedAlt, double expectedAz) {
            DateTime atTime = new DateTime(yr, mon, day, hh, mm, ss);
            Coordinates coordinates = new Coordinates(AstroUtil.HMSToDegrees(ra), AstroUtil.DMSToDegrees(dec), Epoch.J2000, Coordinates.RAType.Degrees);
            ObserverInfo location = new ObserverInfo();
            location.Latitude = 35.852934;
            location.Longitude = -79.163632;
            location.Elevation = 165;

            HorizontalCoordinate hc = AstrometryUtils.GetHorizontalCoordinates(location, coordinates, atTime);
            hc.Altitude.Should().BeApproximately(expectedAlt, 0.001);
            hc.Azimuth.Should().BeApproximately(expectedAz, 0.001);
        }

        /*
        [Test]
        [TestCase(-3, 0)]
        [TestCase(0, 0)]
        [TestCase(5, 5)]
        [TestCase(255, 255)]
        [TestCase(1000, 255)]
        public async Task TestBrightness(int setValue, int expectedValue) {
            await _sut.Connect(new CancellationToken());
            _sut.Brightness = setValue;
            Assert.That(_sut.Brightness, Is.EqualTo(expectedValue));
        }
         */

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
