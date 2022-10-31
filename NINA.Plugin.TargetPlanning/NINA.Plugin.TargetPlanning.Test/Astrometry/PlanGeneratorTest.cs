using FluentAssertions;
using NINA.Astrometry;
using NINA.Plugin.TargetPlanning.Test.Astrometry;
using NUnit.Framework;
using System;
using TargetPlanning.NINAPlugin.Astrometry;

namespace NINA.Plugin.TargetPlanning.Test.Astrometry {

    public class PlanGeneratorTest {

        [Test]
        public void TestMidpoint() {
            DateTime start = DateTime.Now;
            DateTime mid = PlanGenerator.GetMidpointTime(start, start.AddHours(1));
            mid.Should().Be(start.AddMinutes(30));
        }
    }

}
