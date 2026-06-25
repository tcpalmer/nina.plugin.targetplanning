using FluentAssertions;
using NINA.Astrometry;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using TargetPlanning.NINAPlugin.Astrometry;

namespace NINA.Plugin.TargetPlanning.Test.Astrometry {

    /// <summary>
    /// Characterization test for the "Daily Details" function (PlanGenerator).  Captures the behavior of the current
    /// astrometry code so it can be compared before and after the migration to the Target Scheduler astrometry classes.
    ///
    /// Uses TestUtil.M42 at TestUtil.TEST_LOCATION_1, planning 14 days from Sept 1 2025, with all planning criteria
    /// left at the plugin's default values (see GetDefaultParameters).
    ///
    /// Numeric expectations use tolerances rather than exact equality: the replacement astrometry uses a sampling
    /// approach (vs the current exact solver) so imaging times and angles may shift slightly.
    /// </summary>
    [TestFixture]
    public class DailyDetailsTest {

        // Tolerances chosen to absorb the post-migration sampling approach while still catching real regressions.
        private const double MINUTES_TOLERANCE = 2;
        private const double ILLUMINATION_TOLERANCE = 0.01;
        private const double SEPARATION_TOLERANCE = 0.5;

        private const string LIMIT_MIN_ALTITUDE = "  (limited by minimum altitude)";
        private const string LIMIT_TWILIGHT = "  (limited by twilight)";

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
        public void TestDailyDetailsDefaults() {
            IList<ImagingDayPlan> results = new PlanGenerator(GetDefaultParameters()).Generate(tokenSource.Token);

            results.Count.Should().Be(13);

            int i = 0;
            ExpectedPlan[] e = ExpectedPlans();
            e.Length.Should().Be(results.Count);
            foreach (ImagingDayPlan plan in results) {
                AssertPlan(plan, e[i++]);
            }
        }

        private static ExpectedPlan[] ExpectedPlans() {
            // Baseline captured from the current astrometry code.
            return new[] {
                new ExpectedPlan("2025-09-02", 213, true, LIMIT_MIN_ALTITUDE, LIMIT_TWILIGHT, 0.6984, 144.7737),
                new ExpectedPlan("2025-09-03", 218, true, LIMIT_MIN_ALTITUDE, LIMIT_TWILIGHT, 0.7847, 140.3864),
                new ExpectedPlan("2025-09-04", 223, true, LIMIT_MIN_ALTITUDE, LIMIT_TWILIGHT, 0.8620, 133.2195),
                new ExpectedPlan("2025-09-05", 227, true, LIMIT_MIN_ALTITUDE, LIMIT_TWILIGHT, 0.9260, 124.1071),
                new ExpectedPlan("2025-09-06", 232, true, LIMIT_MIN_ALTITUDE, LIMIT_TWILIGHT, 0.9724, 113.6655),
                new ExpectedPlan("2025-09-07", 237, true, LIMIT_MIN_ALTITUDE, LIMIT_TWILIGHT, 0.9970, 102.3228),
                new ExpectedPlan("2025-09-08", 242, true, LIMIT_MIN_ALTITUDE, LIMIT_TWILIGHT, 0.9964, 90.4188),
                new ExpectedPlan("2025-09-09", 246, true, LIMIT_MIN_ALTITUDE, LIMIT_TWILIGHT, 0.9690, 78.2804),
                new ExpectedPlan("2025-09-10", 251, true, LIMIT_MIN_ALTITUDE, LIMIT_TWILIGHT, 0.9152, 66.2783),
                new ExpectedPlan("2025-09-11", 256, true, LIMIT_MIN_ALTITUDE, LIMIT_TWILIGHT, 0.8378, 54.8947),
                new ExpectedPlan("2025-09-12", 261, true, LIMIT_MIN_ALTITUDE, LIMIT_TWILIGHT, 0.7417, 44.8461),
                new ExpectedPlan("2025-09-13", 265, true, LIMIT_MIN_ALTITUDE, LIMIT_TWILIGHT, 0.6330, 37.2787),
                new ExpectedPlan("2025-09-14", 270, true, LIMIT_MIN_ALTITUDE, LIMIT_TWILIGHT, 0.5185, 33.7601),
            };
        }

        private static void AssertPlan(ImagingDayPlan plan, ExpectedPlan e) {
            string day = plan.StartDay.ToString("yyyy-MM-dd");
            day.Should().Be(e.Day);
            plan.IsAccepted().Should().Be(e.Accepted, $"accepted mismatch on {e.Day}");
            plan.StartLimitingFactor.Name.Should().Be(e.StartLimit, $"start limit mismatch on {e.Day}");
            plan.EndLimitingFactor.Name.Should().Be(e.EndLimit, $"end limit mismatch on {e.Day}");
            ((double)plan.GetImagingMinutes()).Should().BeApproximately(e.ImagingMinutes, MINUTES_TOLERANCE, $"imaging minutes mismatch on {e.Day}");
            plan.MoonIllumination.Should().BeApproximately(e.MoonIllumination, ILLUMINATION_TOLERANCE, $"moon illumination mismatch on {e.Day}");
            plan.MoonSeparation.Should().BeApproximately(e.MoonSeparation, SEPARATION_TOLERANCE, $"moon separation mismatch on {e.Day}");
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

        private class ExpectedPlan {
            public string Day { get; }
            public double ImagingMinutes { get; }
            public bool Accepted { get; }
            public string StartLimit { get; }
            public string EndLimit { get; }
            public double MoonIllumination { get; }
            public double MoonSeparation { get; }

            public ExpectedPlan(string day, double imagingMinutes, bool accepted, string startLimit, string endLimit,
                double moonIllumination, double moonSeparation) {
                Day = day;
                ImagingMinutes = imagingMinutes;
                Accepted = accepted;
                StartLimit = startLimit;
                EndLimit = endLimit;
                MoonIllumination = moonIllumination;
                MoonSeparation = moonSeparation;
            }
        }
    }
}
