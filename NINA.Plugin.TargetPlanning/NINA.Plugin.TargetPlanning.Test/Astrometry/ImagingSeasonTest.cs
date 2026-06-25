using FluentAssertions;
using NINA.Astrometry;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using TargetPlanning.NINAPlugin.Astrometry;

namespace NINA.Plugin.TargetPlanning.Test.Astrometry {

    /// <summary>
    /// Characterization test for the "Imaging Season" function (OptimalImagingSeason).  Captures the behavior of the
    /// current astrometry code so it can be compared before and after the migration to the Target Scheduler astrometry
    /// classes.
    ///
    /// OptimalImagingSeason computes the season window (start date + number of days) from the target's heliacal
    /// rising/setting, then runs PlanGenerator over that window.  Uses TestUtil.M42 at TestUtil.TEST_LOCATION_1 with
    /// all planning criteria left at the plugin's default values.
    ///
    /// Numeric expectations use tolerances rather than exact equality: the replacement astrometry uses a sampling
    /// approach (vs the current exact solver) so the season window edges and aggregate imaging time may shift slightly.
    /// </summary>
    [TestFixture]
    public class ImagingSeasonTest {

        // The season window is determined by heliacal solving; allow a couple of days of drift at each edge and a
        // correspondingly wider tolerance on the aggregate imaging minutes after the migration.
        private const int DAYS_TOLERANCE = 3;
        private const double TOTAL_MINUTES_TOLERANCE = 2000;

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
        public void TestImagingSeasonDefaults() {
            PlanParameters pp = GetDefaultParameters();
            IList<ImagingDayPlan> results = new OptimalImagingSeason().GetOptimalSeason(pp, tokenSource.Token);

            results.Should().NotBeNull();

            // OptimalImagingSeason mutates StartDate/PlanDays to the computed season window.
            pp.StartDate.Should().BeCloseTo(new DateTime(2025, 7, 28), TimeSpan.FromDays(DAYS_TOLERANCE));
            ((double)pp.PlanDays).Should().BeApproximately(281, DAYS_TOLERANCE);
            ((double)results.Count).Should().BeApproximately(280, DAYS_TOLERANCE);

            long totalMinutes = 0;
            int accepted = 0;
            DateTime? firstAccepted = null;
            DateTime? lastAccepted = null;
            foreach (ImagingDayPlan plan in results) {
                if (plan.IsAccepted()) {
                    accepted++;
                    totalMinutes += plan.GetImagingMinutes();
                    firstAccepted ??= plan.StartDay;
                    lastAccepted = plan.StartDay;
                }
            }

            ((double)accepted).Should().BeApproximately(272, DAYS_TOLERANCE);
            ((double)totalMinutes).Should().BeApproximately(105273, TOTAL_MINUTES_TOLERANCE);
            firstAccepted.Should().NotBeNull();
            lastAccepted.Should().NotBeNull();
            ((DateTime)firstAccepted).Should().BeCloseTo(new DateTime(2025, 8, 2), TimeSpan.FromDays(DAYS_TOLERANCE));
            ((DateTime)lastAccepted).Should().BeCloseTo(new DateTime(2026, 4, 29), TimeSpan.FromDays(DAYS_TOLERANCE));
        }

        private static PlanParameters GetDefaultParameters() {
            return new PlanParameters {
                Target = new DeepSkyObject("M42", TestUtil.M42, null),
                ObserverInfo = TestUtil.TEST_LOCATION_1,
                StartDate = new DateTime(2025, 9, 1),
                PlanDays = 14,
                HorizonDefinition = new HorizonDefinition(0),
                MinimumImagingTime = 60,
                MeridianTimeSpan = 0,
                TwilightInclude = TwilightInclude.ASTRO,
                MinimumMoonSeparation = 0,
                MaximumMoonIllumination = 0,
                MoonAvoidanceEnabled = false,
                MoonAvoidanceWidth = 7,
            };
        }
    }
}
