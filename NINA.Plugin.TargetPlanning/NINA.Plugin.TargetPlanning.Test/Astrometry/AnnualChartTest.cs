using FluentAssertions;
using NINA.Astrometry;
using NUnit.Framework;
using System;
using System.Threading;
using TargetPlanning.NINAPlugin.AnnualChart;

namespace NINA.Plugin.TargetPlanning.Test.Astrometry {

    /// <summary>
    /// Characterization test for the "Annual Chart" function (AnnualPlanningChartModel).  Captures the behavior of the
    /// current astrometry code so it can be compared before and after the migration to the Target Scheduler astrometry
    /// classes.
    ///
    /// AnnualPlanningChartModel computes the target and moon altitude for each day of the year (the year of the supplied
    /// start time).  Uses TestUtil.M42 at TestUtil.TEST_LOCATION_1 for 2025.
    ///
    /// Altitude expectations use a tolerance rather than exact equality to absorb minor differences after the migration.
    /// </summary>
    [TestFixture]
    public class AnnualChartTest {

        private const double ALTITUDE_TOLERANCE = 0.5;

        private CancellationTokenSource tokenSource;

        [SetUp]
        public void SetUp() {
            tokenSource = new CancellationTokenSource();
        }

        [TearDown]
        public void TearDown() {
            tokenSource.Dispose();
        }

        [Test]
        public void TestAnnualChartDefaults() {
            var model = new AnnualPlanningChartModel(TestUtil.TEST_LOCATION_1,
                new DeepSkyObject("M42", TestUtil.M42, null), new DateTime(2025, 9, 1), tokenSource.Token);

            model.TargetName.Should().Be("M42");
            model.TargetAltitudes.Count.Should().Be(365);
            model.MoonAltitudes.Count.Should().Be(365);

            // Baseline captured from the current astrometry code: { dayIndex, targetAltitude, moonAltitude }.
            double[,] expected = {
                { 0, 47.7159, -68.5694 },
                { 60, 11.0921, -38.8108 },
                { 120, -24.7110, -1.1385 },
                { 180, -60.1211, -2.6687 },
                { 240, -31.6006, -19.4929 },
                { 300, 16.0817, -14.2433 },
                { 364, 48.0421, 49.2126 },
            };

            for (int row = 0; row < expected.GetLength(0); row++) {
                int idx = (int)expected[row, 0];
                model.TargetAltitudes[idx].Y.Should().BeApproximately(expected[row, 1], ALTITUDE_TOLERANCE,
                    $"target altitude mismatch at day index {idx}");
                model.MoonAltitudes[idx].Y.Should().BeApproximately(expected[row, 2], ALTITUDE_TOLERANCE,
                    $"moon altitude mismatch at day index {idx}");
            }
        }
    }
}
