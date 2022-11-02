using FluentAssertions;
using Moq;
using NINA.Astrometry;
using NINA.Astrometry.Interfaces;
using NUnit.Framework;
using System;
using TargetPlanning.NINAPlugin.Astrometry;

namespace TargetPlanning.NINAPlugin.Test.Astrometry {

    public class DSORefinerTest {

        private Mock<IDeepSkyObject> dsoMock = new Mock<IDeepSkyObject>();

        [SetUp]
        public void Setup() {
            dsoMock.Reset();
        }

        [Test]
        public void TestBad() {

            DateTime dt = DateTime.Now;

            var ex = Assert.Throws<ArgumentException>(() => new DSORefiner(null, null));
            ex.Message.Should().Be("location cannot be null");

            ObserverInfo oi = new ObserverInfo();
            ex = Assert.Throws<ArgumentException>(() => new DSORefiner(oi, null));
            ex.Message.Should().Be("target cannot be null");
        }
    }

}
