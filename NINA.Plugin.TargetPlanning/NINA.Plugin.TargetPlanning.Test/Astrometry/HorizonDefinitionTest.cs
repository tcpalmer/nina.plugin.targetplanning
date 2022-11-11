using FluentAssertions;
using NUnit.Framework;
using System;
using TargetPlanning.NINAPlugin.Astrometry;

namespace NINA.Plugin.TargetPlanning.Test.Astrometry {

    public class HorizonDefinitionTest {

        [Test]
        public void TestHorizonDefinition() {
            HorizonDefinition sut = new HorizonDefinition(12);
            sut.IsCustom().Should().BeFalse();
            sut.GetTargetAltitude(null).Should().Be(12);

            sut = new HorizonDefinition(TestUtil.GetTestHorizon(1), 0);
            sut.GetTargetAltitude(new AltitudeAtTime(0, 0, DateTime.Now)).Should().BeApproximately(20, 0.001);
            sut.GetTargetAltitude(new AltitudeAtTime(0, 90, DateTime.Now)).Should().BeApproximately(20, 0.001);
            sut.GetTargetAltitude(new AltitudeAtTime(0, 270, DateTime.Now)).Should().BeApproximately(20, 0.001);

            sut = new HorizonDefinition(TestUtil.GetTestHorizon(1), 10);
            sut.IsCustom().Should().BeTrue();
            sut.GetTargetAltitude(new AltitudeAtTime(0, 0, DateTime.Now)).Should().BeApproximately(30, 0.001);
            sut.GetTargetAltitude(new AltitudeAtTime(0, 90, DateTime.Now)).Should().BeApproximately(30, 0.001);
            sut.GetTargetAltitude(new AltitudeAtTime(0, 270, DateTime.Now)).Should().BeApproximately(30, 0.001);

            var ex = Assert.Throws<ArgumentException>(() => sut.GetFixedMinimumAltitude());
            ex.Message.Should().Be("minimumAltitude n/a in this context");

            sut = new HorizonDefinition(null, 0); // no custom horizon defaults to 0 min altitude
            sut.GetTargetAltitude(new AltitudeAtTime(0, 0, DateTime.Now)).Should().Be(0);
            sut.GetTargetAltitude(new AltitudeAtTime(0, 90, DateTime.Now)).Should().Be(0);
            sut.GetTargetAltitude(new AltitudeAtTime(0, 270, DateTime.Now)).Should().Be(0);
        }

        [Test]
        public void TestHorizonDefinitionBad() {
            var ex = Assert.Throws<ArgumentException>(() => new HorizonDefinition(-1));
            ex.Message.Should().Be("minimumAltitude must be >= 0 and < 90");

            ex = Assert.Throws<ArgumentException>(() => new HorizonDefinition(90));
            ex.Message.Should().Be("minimumAltitude must be >= 0 and < 90");

            ex = Assert.Throws<ArgumentException>(() => new HorizonDefinition(TestUtil.GetTestHorizon(1), -1));
            ex.Message.Should().Be("offset must be >= 0");
        }
    }

}
