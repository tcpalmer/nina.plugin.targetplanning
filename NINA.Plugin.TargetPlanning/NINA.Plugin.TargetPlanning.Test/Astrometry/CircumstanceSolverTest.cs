using FluentAssertions;
using FluentAssertions.Common;
using NINA.Astrometry;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Security.Policy;
using TargetPlanning.NINAPlugin.Astrometry;
using TargetPlanning.NINAPlugin.Test.Astrometry;

namespace NINA.Plugin.TargetPlanning.Test.Astrometry {

    public class CircumstanceSolverTest {

        [Test]
        [TestCase(-10, -5, 60, double.MinValue)] // No rising
        [TestCase(1, 10, 60, double.MinValue)] // No rising
        [TestCase(-45, 45, 21600, 0.00307)]
        public void testFindRising(double altStart, double altEnd, int secs, double expectedAlt) {
            DateTime start = DateTime.Now;
            List<AltitudeAtTime> alts = new List<AltitudeAtTime>();

            alts.Add(new AltitudeAtTime(altStart, start));
            alts.Add(new AltitudeAtTime(altEnd, start.AddSeconds(secs)));
            AltitudeAtTime aat = new CircumstanceSolver(new TestAltitudeRefiner()).FindRising(new Altitudes(alts));

            if (expectedAlt == double.MinValue) {
                aat.Should().BeNull();
            }
            else {
                aat.Altitude.Should().BeApproximately(expectedAlt, 0.001);
            }
        }

        [Test]
        public void testFindRiseAboveMinimum() {

            CircumstanceSolver cs = new CircumstanceSolver(new TestAltitudeRefiner());

            var ex = Assert.Throws<ArgumentException>(() => cs.FindRiseAboveMinimum(null, 0));
            ex.Message.Should().Be("altitudes cannot be null");

            DateTime start = DateTime.Now.Date;
            List<AltitudeAtTime> alts = new List<AltitudeAtTime>();

            alts.Add(new AltitudeAtTime(0, start));
            alts.Add(new AltitudeAtTime(10, start.AddSeconds(60)));

            ex = Assert.Throws<ArgumentException>(() => cs.FindRiseAboveMinimum(new Altitudes(alts), 0));
            ex.Message.Should().Be("minimumAltitude must be > 0");

            ex = Assert.Throws<ArgumentException>(() => cs.FindRiseAboveMinimum(new Altitudes(alts), 90.1));
            ex.Message.Should().Be("minimumAltitude must be <= 90");

            alts.Clear();
            alts.Add(new AltitudeAtTime(10, start));
            alts.Add(new AltitudeAtTime(50, start.AddSeconds(60)));

            // min outside span
            cs.FindRiseAboveMinimum(new Altitudes(alts), 5).Should().BeNull();
            cs.FindRiseAboveMinimum(new Altitudes(alts), 55).Should().BeNull();

            alts.Clear();
            alts.Add(new AltitudeAtTime(1, start));
            alts.Add(new AltitudeAtTime(10, start.AddMinutes(1)));
            alts.Add(new AltitudeAtTime(20, start.AddMinutes(2)));
            alts.Add(new AltitudeAtTime(30, start.AddMinutes(3)));

            AltitudeAtTime aat = cs.FindRiseAboveMinimum(new Altitudes(alts), 15);
            aat.Altitude.Should().BeApproximately(15.0413, 0.001);

            aat.AtTime.Hour.Should().Be(0);
            aat.AtTime.Minute.Should().Be(1);
            aat.AtTime.Second.Should().Be(30);
        }

        [Test]
        public void testFindTransit() {

            // Note that you can't test transit with the test refiner since it can't refine a wrapped interval properly

            Coordinates betelgeuse = new Coordinates(AstroUtil.HMSToDegrees("5:55:11"), AstroUtil.DMSToDegrees("7:24:30"), Epoch.J2000, Coordinates.RAType.Degrees);
            DSORefiner refiner = new DSORefiner(AstrometryUtilsTest.TEST_LOCATION_1, betelgeuse);
            DateTime day = new DateTime(2022, 10, 12).Date;

            Altitudes generated = refiner.GetHourlyAltitudesForDay(day);
            AltitudeAtTime aat = new CircumstanceSolver(refiner, 1).FindTransit(generated);
            aat.Altitude.Should().BeApproximately(62.4083, 0.001);

            DateTime at = aat.AtTime;
            DateTime actual = new DateTime(at.Year, at.Month, at.Day, at.Hour, at.Minute, 0);
            DateTime expected = new DateTime(2022, 10, 12, 5, 47, 0);
            actual.Should().Be(expected);

            var ex = Assert.Throws<ArgumentException>(() => new CircumstanceSolver(new TestAltitudeRefiner(), 1).FindTransit(null));
            ex.Message.Should().Be("altitudes cannot be null");
        }

        [Test]
        public void testFindSetBelowMinimum() {

            CircumstanceSolver cs = new CircumstanceSolver(new TestAltitudeRefiner());

            var ex = Assert.Throws<ArgumentException>(() => cs.FindSetBelowMinimum(null, 0));
            ex.Message.Should().Be("altitudes cannot be null");

            DateTime start = DateTime.Now.Date;
            List<AltitudeAtTime> alts = new List<AltitudeAtTime>();

            alts.Add(new AltitudeAtTime(0, start));
            alts.Add(new AltitudeAtTime(10, start.AddSeconds(60)));

            ex = Assert.Throws<ArgumentException>(() => cs.FindSetBelowMinimum(new Altitudes(alts), 0));
            ex.Message.Should().Be("minimumAltitude must be > 0");

            ex = Assert.Throws<ArgumentException>(() => cs.FindSetBelowMinimum(new Altitudes(alts), 90.1));
            ex.Message.Should().Be("minimumAltitude must be <= 90");

            alts.Clear();
            alts.Add(new AltitudeAtTime(50, start));
            alts.Add(new AltitudeAtTime(10, start.AddSeconds(60)));

            // min outside span
            cs.FindSetBelowMinimum(new Altitudes(alts), 55).Should().BeNull();
            cs.FindSetBelowMinimum(new Altitudes(alts), 5).Should().BeNull();

            alts.Clear();
            alts.Add(new AltitudeAtTime(60, start));
            alts.Add(new AltitudeAtTime(50, start.AddMinutes(1)));
            alts.Add(new AltitudeAtTime(40, start.AddMinutes(2)));
            alts.Add(new AltitudeAtTime(30, start.AddMinutes(3)));

            AltitudeAtTime aat = cs.FindSetBelowMinimum(new Altitudes(alts), 55);

            aat.Altitude.Should().BeApproximately(55.0413, 0.001);
            aat.AtTime.Hour.Should().Be(0);
            aat.AtTime.Minute.Should().Be(0);
            aat.AtTime.Second.Should().Be(29);
        }

        [Test]
        public void testFindSetting() {
            DateTime start = DateTime.Now;
            List<AltitudeAtTime> alts = new List<AltitudeAtTime>();
            CircumstanceSolver cs = new CircumstanceSolver(new TestAltitudeRefiner());

            // No setting present
            alts.Add(new AltitudeAtTime(10, start));
            alts.Add(new AltitudeAtTime(5, start.AddSeconds(60)));
            cs.FindSetting(new Altitudes(alts)).Should().BeNull();

            // No setting present
            alts.Clear();
            alts.Add(new AltitudeAtTime(-1, start));
            alts.Add(new AltitudeAtTime(-10, start.AddSeconds(60)));
            cs.FindSetting(new Altitudes(alts)).Should().BeNull();

            // Good test
            alts.Clear();
            alts.Add(new AltitudeAtTime(45, start));
            alts.Add(new AltitudeAtTime(-45, start.AddHours(6)));
            AltitudeAtTime aat = cs.FindSetting(new Altitudes(alts));
            aat.Altitude.Should().BeApproximately(0.00307, 0.001);

            var ex = Assert.Throws<ArgumentException>(() => cs.FindSetting(null));
            ex.Message.Should().Be("altitudes cannot be null");
        }

        [Test]
        public void testGetStepInterval() {
            CircumstanceSolver cs = new CircumstanceSolver(new TestAltitudeRefiner(), 1);

            var ex = Assert.Throws<ArgumentException>(() => cs.GetStepInterval(null));
            ex.Message.Should().Be("step cannot be null");

            DateTime start = DateTime.Now;
            List<AltitudeAtTime> alts = new List<AltitudeAtTime>();

            alts.Add(new AltitudeAtTime(-1, start));
            alts.Add(new AltitudeAtTime(1, start.AddSeconds(10)));
            alts.Add(new AltitudeAtTime(2, start.AddSeconds(20)));

            ex = Assert.Throws<ArgumentException>(() => cs.GetStepInterval(new Altitudes(alts)));
            ex.Message.Should().Be("altitudes must have exactly two points");

            alts.Clear();
            alts.Add(new AltitudeAtTime(-1, start));
            alts.Add(new AltitudeAtTime(1, start.AddSeconds(60)));

            cs.GetStepInterval(new Altitudes(alts)).Should().Be(60);
        }

        [Test]
        public void testBad() {

            var ex = Assert.Throws<ArgumentException>(() => new CircumstanceSolver(null, 0));
            ex.Message.Should().Be("refiner cannot be null");

            ex = Assert.Throws<ArgumentException>(() => new CircumstanceSolver(null, 0));
            ex.Message.Should().Be("refiner cannot be null");

            ex = Assert.Throws<ArgumentException>(() => new CircumstanceSolver(new TestAltitudeRefiner(), 0));
            ex.Message.Should().Be("max final time step must be >= 1");
        }
    }

}
