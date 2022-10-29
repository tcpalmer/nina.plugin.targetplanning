using FluentAssertions;
using Moq;
using NINA.Astrometry;
using NINA.Astrometry.Interfaces;
using NUnit.Framework;
using System;
using TargetPlanning.NINAPlugin.Astrometry;

namespace TargetPlanning.NINAPlugin.Test.Astrometry {

    public class TargetImagingCircumstancesTest {

        private Mock<IDeepSkyObject> dsoMock = new Mock<IDeepSkyObject>();

        [SetUp]
        public void Setup() {
            dsoMock.Reset();
        }

        [Test]
        public void TestBad() {

            DateTime dt = DateTime.Now;

            var ex = Assert.Throws<ArgumentException>(() => new TargetImagingCircumstances(null, null, dt, dt, 0));
            ex.Message.Should().Be("location cannot be null");

            ObserverInfo oi = new ObserverInfo();
            ex = Assert.Throws<ArgumentException>(() => new TargetImagingCircumstances(oi, null, dt, dt, 0));
            ex.Message.Should().Be("target cannot be null");

            ex = Assert.Throws<ArgumentException>(() => new TargetImagingCircumstances(oi, dsoMock.Object, dt, dt.AddMinutes(-1), 0));
            ex.Message.Should().Be("startTime must be before endTime");

            ex = Assert.Throws<ArgumentException>(() => new TargetImagingCircumstances(oi, dsoMock.Object, dt, dt.AddMinutes(1), -1));
            ex.Message.Should().Be("minimumAltitude must be >= 0");
        }
    }

}
