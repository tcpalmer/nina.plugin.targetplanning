using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using TargetPlanning.NINAPlugin.Astrometry;

namespace TargetPlanning.NINAPlugin.Test.Astrometry {

    public class AltitudesTest {

        [Test]
        public void TestBad() {

            var ex = Assert.Throws<ArgumentException>(() => new Altitudes(null));
            Assert.AreEqual("altitudes cannot be null", ex.Message);

            List<AltitudeAtTime> altitudes = new List<AltitudeAtTime>();
            ex = Assert.Throws<ArgumentException>(() => new Altitudes(altitudes));
            Assert.AreEqual("altitudes must have at least two values", ex.Message);

            altitudes.Add(new AltitudeAtTime(0, 180, DateTime.Now));
            ex = Assert.Throws<ArgumentException>(() => new Altitudes(altitudes));
            Assert.AreEqual("altitudes must have at least two values", ex.Message);

            altitudes.Add(new AltitudeAtTime(1, 180, DateTime.Now.AddSeconds(-100)));
            ex = Assert.Throws<ArgumentException>(() => new Altitudes(altitudes));
            Assert.AreEqual("startTime must be before endTime", ex.Message);

            altitudes.Clear();

            DateTime dt = DateTime.Now;
            altitudes.Add(new AltitudeAtTime(0.01, 180, dt));
            altitudes.Add(new AltitudeAtTime(1.12, 180, dt.AddMinutes(10)));
            altitudes.Add(new AltitudeAtTime(2.23, 180, dt.AddMinutes(5)));
            altitudes.Add(new AltitudeAtTime(3.45, 180, dt.AddMinutes(20)));

            //TestContext.Out.WriteLine(new Altitudes(altitudes).ToString());

            ex = Assert.Throws<ArgumentException>(() => new Altitudes(altitudes));
            Assert.AreEqual("time is not always increasing", ex.Message);
        }
    }

    public class AltitudeAtTimeTest {

        [Test]
        public void TestOk() {
            DateTime dt = DateTime.Now;
            AltitudeAtTime sut = new AltitudeAtTime(45, 180, dt);

            sut.Altitude.Should().Be(45);
            sut.AtTime.Should().Be(dt);
        }

        [Test]
        public void TestBad() {
            var ex = Assert.Throws<ArgumentException>(() => new AltitudeAtTime(180, 180, DateTime.Now));
            Assert.AreEqual("altitude must be <= 90 and >= -90", ex.Message);

            ex = Assert.Throws<ArgumentException>(() => new AltitudeAtTime(45, 361, DateTime.Now));
            Assert.AreEqual("azimuth must be >= 0 and <= 360", ex.Message);
        }
    }

}