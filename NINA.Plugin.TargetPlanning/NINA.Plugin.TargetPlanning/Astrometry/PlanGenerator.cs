using NINA.Astrometry;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;

namespace TargetPlanning.NINAPlugin.Astrometry {

    public class PlanGenerator {

        // Sampling interval (seconds) for the Target Scheduler sample-based visibility approach.
        private const int SAMPLE_INTERVAL = 10;

        private PlanParameters PlanParameters;

        public PlanGenerator(PlanParameters planParameters) {
            this.PlanParameters = planParameters;
        }

        public IList<ImagingDayPlan> Generate(CancellationToken token) {

            List<ImagingDayPlan> imagingDayList = new List<ImagingDayPlan>();
            ImagingDayPlanCache imagingDayPlanCache = new ImagingDayPlanCache();

            DeepSkyObject target = PlanParameters.Target;
            ObserverInfo location = PlanParameters.ObserverInfo;
            TwilightLevel twilightLevel = TwilightInclude.ToTwilightLevel(PlanParameters.TwilightInclude);

            using (MyStopWatch.Measure("planGenerate")) {
                for (int i = 0; i < PlanParameters.PlanDays - 1; i++) {

                    if (token.IsCancellationRequested) {
                        throw new OperationCanceledException();
                    }

                    DateTime date = PlanParameters.StartDate.AddDays(i);
                    TwilightCircumstances twilightCircumstances = new TwilightCircumstances(location, date);

                    // Get the presumptive start and end times for this 'imaging day'
                    TimeInterval twilightSpan = twilightCircumstances.GetTwilightSpan(twilightLevel);
                    ImagingDayPlan plan;
                    DateTime midPointTime;
                    double moonIllumination;
                    double moonSeparation;
                    double moonAvoidanceSeparation = double.MinValue;

                    // If the desired twilight span doesn't apply on this day (e.g. very high latitudes near summer solstice),
                    // then set time limits to sun set/rise and bail out.
                    if (twilightSpan == null) {
                        TimeInterval daylightSpan = twilightCircumstances.GetTwilightSpan(TwilightLevel.Civil);
                        midPointTime = Utils.GetMidpointTime(daylightSpan.StartTime, daylightSpan.EndTime);
                        moonIllumination = AstrometryUtils.GetMoonIllumination(midPointTime);
                        moonSeparation = AstrometryUtils.GetMoonSeparationAngle(location, midPointTime, target.Coordinates);

                        plan = new ImagingDayPlan(daylightSpan.StartTime, daylightSpan.EndTime, DateTime.MinValue, ImagingLimit.NoTwilight, ImagingLimit.NoTwilight,
                            moonIllumination, moonSeparation, moonAvoidanceSeparation);
                        imagingDayPlanCache.Put(plan, daylightSpan.StartTime, daylightSpan.EndTime, PlanParameters);
                        imagingDayList.Add(plan);
                        continue;
                    }

                    DateTime startTime = twilightSpan.StartTime;
                    DateTime endTime = twilightSpan.EndTime;

                    // Check the cache
                    plan = imagingDayPlanCache.Get(startTime, endTime, PlanParameters);
                    if (plan != null) {
                        imagingDayList.Add(plan);
                        continue;
                    }

                    VisibilityCircumstances circumstances = AnalyzeVisibility(twilightCircumstances, twilightSpan, location,
                        target.Coordinates, PlanParameters.HorizonDefinition, date);
                    ImagingCriteriaAnalyzer analyzer;

                    // Check if the target is visible at all
                    if (!circumstances.PotentiallyVisible) {
                        analyzer = new ImagingCriteriaAnalyzer(startTime, endTime);

                        midPointTime = Utils.GetMidpointTime(analyzer.StartImagingTime, analyzer.EndImagingTime);
                        moonIllumination = AstrometryUtils.GetMoonIllumination(midPointTime);
                        moonSeparation = AstrometryUtils.GetMoonSeparationAngle(location, midPointTime, target.Coordinates);

                        plan = new ImagingDayPlan(startTime, endTime, startTime.AddMinutes(1), ImagingLimit.NotVisible, ImagingLimit.NotVisible,
                            moonIllumination, moonSeparation, moonAvoidanceSeparation);
                        imagingDayPlanCache.Put(plan, startTime, endTime, PlanParameters);

                        imagingDayList.Add(plan);
                        continue;
                    }

                    DateTime startImagingTime = circumstances.RiseAboveMinimumTime;
                    DateTime endImagingTime = circumstances.SetBelowMinimumTime;
                    analyzer = new ImagingCriteriaAnalyzer(startImagingTime, endImagingTime);

                    // Adjust for twilight
                    analyzer.AdjustForTwilight(startTime, endTime);

                    // Adjust for meridian proximity criteria
                    DateTime transitTime = circumstances.TransitTime;
                    if (transitTime == DateTime.MinValue) {
                        Logger.Warning("no transit found");
                    }

                    if (PlanParameters.MeridianTimeSpan != 0 && transitTime != DateTime.MinValue) {
                        analyzer.AdjustForMeridianProximity(transitTime, PlanParameters.MeridianTimeSpan);
                    }

                    // Calculate moon metrics here so available if rejected early
                    midPointTime = Utils.GetMidpointTime(analyzer.StartImagingTime, analyzer.EndImagingTime);
                    moonIllumination = AstrometryUtils.GetMoonIllumination(midPointTime);
                    moonSeparation = AstrometryUtils.GetMoonSeparationAngle(location, midPointTime, target.Coordinates);

                    // Stop if already rejected
                    if (analyzer.SessionIsRejected()) {
                        plan = GetPlan(analyzer, transitTime, moonIllumination, moonSeparation, moonAvoidanceSeparation);
                        imagingDayPlanCache.Put(plan, startTime, endTime, PlanParameters);

                        imagingDayList.Add(plan);
                        continue;
                    }

                    // Accept/reject for moon illumination criteria
                    if (!PlanParameters.MoonAvoidanceEnabled && PlanParameters.MaximumMoonIllumination != 0) {
                        analyzer.AdjustForMoonIllumination(moonIllumination, PlanParameters.MaximumMoonIllumination);

                        // Stop if already rejected
                        if (analyzer.SessionIsRejected()) {
                            plan = GetPlan(analyzer, transitTime, moonIllumination, moonSeparation, moonAvoidanceSeparation);
                            imagingDayPlanCache.Put(plan, startTime, endTime, PlanParameters);

                            imagingDayList.Add(plan);
                            continue;
                        }
                    }

                    // Accept/reject for moon separation criteria
                    if (!PlanParameters.MoonAvoidanceEnabled && PlanParameters.MinimumMoonSeparation != 0) {
                        analyzer.AdjustForMoonSeparation(moonSeparation, PlanParameters.MinimumMoonSeparation);

                        // Stop if already rejected
                        if (analyzer.SessionIsRejected()) {
                            plan = GetPlan(analyzer, transitTime, moonIllumination, moonSeparation, moonAvoidanceSeparation);
                            imagingDayPlanCache.Put(plan, startTime, endTime, PlanParameters);

                            imagingDayList.Add(plan);
                            continue;
                        }
                    }

                    // Accept/reject for moon avoidance separation criteria
                    if (PlanParameters.MoonAvoidanceEnabled && PlanParameters.MinimumMoonSeparation != 0) {
                        moonAvoidanceSeparation = analyzer.AdjustForMoonAvoidanceSeparation(midPointTime, moonSeparation,
                            PlanParameters.MinimumMoonSeparation, PlanParameters.MoonAvoidanceWidth);

                        // Stop if already rejected
                        if (analyzer.SessionIsRejected()) {
                            plan = GetPlan(analyzer, transitTime, moonIllumination, moonSeparation, moonAvoidanceSeparation);
                            imagingDayPlanCache.Put(plan, startTime, endTime, PlanParameters);

                            imagingDayList.Add(plan);
                            continue;
                        }
                    }

                    // Finally, accept/reject for minimum imaging time criteria
                    if (PlanParameters.MinimumImagingTime != 0) {
                        analyzer.AdjustForMinimumImagingTime(PlanParameters.MinimumImagingTime);
                    }

                    plan = GetPlan(analyzer, transitTime, moonIllumination, moonSeparation, moonAvoidanceSeparation);
                    imagingDayPlanCache.Put(plan, startTime, endTime, PlanParameters);

                    imagingDayList.Add(plan);
                }

                return imagingDayList;
            }
        }

        /// <summary>
        /// Determine the target's visibility for the night using the Target Scheduler sample-based approach.  The result
        /// maps onto the conventions previously provided by TargetImagingCircumstances/ImagingDay so the downstream
        /// ImagingCriteriaAnalyzer logic is unchanged:
        ///   - RiseAboveMinimumTime is DateTime.MinValue when the target is already above the horizon at twilight start
        ///     (AdjustForTwilight then attributes the start to twilight); otherwise it is the rise-above-horizon time.
        ///   - SetBelowMinimumTime is DateTime.MinValue when the target is still above the horizon at twilight end;
        ///     otherwise it is the set-below-horizon time.
        /// </summary>
        private VisibilityCircumstances AnalyzeVisibility(TwilightCircumstances twilightCircumstances, TimeInterval twilightSpan,
            ObserverInfo location, Coordinates coordinates, HorizonDefinition horizon, DateTime date) {

            // If the target never rises at this location, stop
            if (!AstrometryUtils.RisesAtLocation(location, coordinates)) {
                return VisibilityCircumstances.NotVisible();
            }

            DateTime? sunset = twilightCircumstances.Sunset;
            DateTime? sunrise = twilightCircumstances.Sunrise;
            if (sunset == null || sunrise == null || sunset >= sunrise) {
                return VisibilityCircumstances.NotVisible();
            }

            TargetVisibility visibility = new TargetVisibility(GetTargetName(), 0, location, coordinates, date,
                sunset, sunrise, 0, SAMPLE_INTERVAL);

            if (!visibility.ImagingPossible) {
                return VisibilityCircumstances.NotVisible();
            }

            VisibilityDetermination viz = visibility.NextVisibleInterval(twilightSpan.StartTime, twilightSpan, horizon);
            if (!viz.IsVisible) {
                return VisibilityCircumstances.NotVisible();
            }

            // Reconstruct the rise/set 'MinValue means clipped by twilight' convention from the visible interval.
            DateTime riseAboveMinimum = (viz.StartTime - twilightSpan.StartTime).TotalSeconds <= SAMPLE_INTERVAL
                ? DateTime.MinValue : viz.StartTime;
            DateTime setBelowMinimum = (twilightSpan.EndTime - viz.StopTime).TotalSeconds <= SAMPLE_INTERVAL
                ? DateTime.MinValue : viz.StopTime;

            return new VisibilityCircumstances(true, riseAboveMinimum, setBelowMinimum, visibility.TransitTime);
        }

        private string GetTargetName() {
            string name = PlanParameters.Target?.Name;
            return string.IsNullOrEmpty(name) ? "User coords" : name;
        }

        private ImagingDayPlan GetPlan(ImagingCriteriaAnalyzer analyzer, DateTime transitTime, double moonIllumination,
                                       double moonSeparation, double moonAvoidanceSeparation) {
            return new ImagingDayPlan(analyzer.StartImagingTime, analyzer.EndImagingTime, transitTime,
                                      analyzer.StartLimitingFactor, analyzer.EndLimitingFactor, moonIllumination,
                                      moonSeparation, moonAvoidanceSeparation);
        }

        // Adapter result mirroring the fields previously produced by TargetImagingCircumstances.
        private class VisibilityCircumstances {
            public bool PotentiallyVisible { get; }
            public DateTime RiseAboveMinimumTime { get; }
            public DateTime SetBelowMinimumTime { get; }
            public DateTime TransitTime { get; }

            public VisibilityCircumstances(bool potentiallyVisible, DateTime riseAboveMinimumTime, DateTime setBelowMinimumTime, DateTime transitTime) {
                PotentiallyVisible = potentiallyVisible;
                RiseAboveMinimumTime = riseAboveMinimumTime;
                SetBelowMinimumTime = setBelowMinimumTime;
                TransitTime = transitTime;
            }

            public static VisibilityCircumstances NotVisible() {
                return new VisibilityCircumstances(false, DateTime.MinValue, DateTime.MinValue, DateTime.MinValue);
            }
        }
    }

    public class PlanParameters {
        public DeepSkyObject Target;
        public ObserverInfo ObserverInfo { get; set; }
        public DateTime StartDate { get; set; }
        public int PlanDays { get; set; }
        public HorizonDefinition HorizonDefinition { get; set; }
        public int MinimumImagingTime { get; set; }
        public int MeridianTimeSpan { get; set; }
        public int TwilightInclude { get; set; }
        public double MinimumMoonSeparation { get; set; }
        public double MaximumMoonIllumination { get; set; }
        public bool MoonAvoidanceEnabled { get; set; }
        public int MoonAvoidanceWidth { get; set; }

        public string GetCacheKey() {
            StringBuilder sb = new StringBuilder();
            sb.Append(Target.Coordinates.ToString()).Append("_");
            sb.Append($"{ObserverInfo.Latitude.ToString("0.000000", CultureInfo.InvariantCulture)}_");
            sb.Append($"{ObserverInfo.Longitude.ToString("0.000000", CultureInfo.InvariantCulture)}_");
            sb.Append(HorizonDefinition.GetCacheKey()).Append("_");
            sb.Append(MinimumImagingTime.ToString()).Append("_");
            sb.Append(MeridianTimeSpan.ToString()).Append("_");
            sb.Append(TwilightInclude.ToString()).Append("_");
            sb.Append(MinimumMoonSeparation.ToString()).Append("_");
            sb.Append(MaximumMoonIllumination.ToString()).Append("_");
            sb.Append(MoonAvoidanceEnabled.ToString()).Append("_");
            sb.Append(MoonAvoidanceWidth.ToString());
            return sb.ToString();
        }
    }

}
