using NINA.Astrometry;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;

namespace TargetPlanning.NINAPlugin.Astrometry {

    public class PlanGenerator {
        private PlanParameters PlanParameters;

        public PlanGenerator(PlanParameters planParameters) {
            this.PlanParameters = planParameters;
        }

        public IList<ImagingDayPlan> Generate(CancellationToken token) {

            List<ImagingDayPlan> imagingDayList = new List<ImagingDayPlan>();
            ImagingDayPlanCache imagingDayPlanCache = new ImagingDayPlanCache();

            DeepSkyObject target = PlanParameters.Target;
            ObserverInfo location = PlanParameters.ObserverInfo;

            List<NighttimeCircumstances> nighttimeCircumstancesList = GetNighttimeCircumstances(PlanParameters.StartDate, PlanParameters.PlanDays, PlanParameters.ObserverInfo, PlanParameters.TwilightInclude, token);

            using (MyStopWatch.Measure("planGenerate")) {
                for (int i = 0; i < nighttimeCircumstancesList.Count - 1; i++) {

                    if (token.IsCancellationRequested) {
                        throw new OperationCanceledException();
                    }

                    // Get the presumptive start and end times for this 'imaging day'
                    Tuple<DateTime, DateTime> twilightSpan = nighttimeCircumstancesList[i].GetTwilightSpan(PlanParameters.TwilightInclude);
                    ImagingDayPlan plan;
                    DateTime midPointTime;
                    double moonIllumination;
                    double moonSeparation;
                    double moonAvoidanceSeparation = double.MinValue;

                    // If the desired twilight span doesn't apply on this day (e.g. very high latitudes near summer solstice),
                    // then set time limits to sun set/rise and bail out.
                    if (twilightSpan == null) {
                        Tuple<DateTime, DateTime> daylightSpan = nighttimeCircumstancesList[i].GetTwilightSpan(NighttimeCircumstances.TWILIGHT_INCLUDE_CIVIL);
                        midPointTime = Utils.GetMidpointTime(daylightSpan.Item1, daylightSpan.Item2);
                        moonIllumination = AstrometryUtils.GetMoonIllumination(midPointTime);
                        moonSeparation = AstrometryUtils.GetMoonSeparationAngle(location, midPointTime, target.Coordinates);

                        plan = new ImagingDayPlan(daylightSpan.Item1, daylightSpan.Item2, DateTime.MinValue, ImagingLimit.NoTwilight, ImagingLimit.NoTwilight,
                            moonIllumination, moonSeparation, moonAvoidanceSeparation);
                        imagingDayPlanCache.Put(plan, daylightSpan.Item1, daylightSpan.Item2, PlanParameters);
                        imagingDayList.Add(plan);
                        continue;
                    }

                    DateTime startTime = twilightSpan.Item1;
                    DateTime endTime = twilightSpan.Item2;

                    // Check the cache
                    plan = imagingDayPlanCache.Get(startTime, endTime, PlanParameters);
                    if (plan != null) {
                        imagingDayList.Add(plan);
                        continue;
                    }

                    TargetImagingCircumstances circumstances = new TargetImagingCircumstances(location,
                                                                                              target.Coordinates,
                                                                                              startTime, endTime,
                                                                                              PlanParameters.HorizonDefinition);
                    int status = circumstances.Analyze();
                    ImagingCriteriaAnalyzer analyzer;

                    // Check if the target is visible at all
                    if (status != TargetImagingCircumstances.STATUS_POTENTIALLY_VISIBLE) {
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

        private List<NighttimeCircumstances> GetNighttimeCircumstances(DateTime startDate, int planDays, ObserverInfo observerInfo, int twilightInclude, CancellationToken token) {
            List<NighttimeCircumstances> list = new List<NighttimeCircumstances>(planDays);
            DateTime date = startDate;

            using (MyStopWatch.Measure("getNighttimeCircumstances")) {
                for (int i = 0; i < planDays; i++) {
                    if (token.IsCancellationRequested) {
                        throw new OperationCanceledException();
                    }

                    list.Add(new NighttimeCircumstances(observerInfo, date, twilightInclude));
                    date = date.AddDays(1);
                }
            }

            return list;
        }

        private ImagingDayPlan GetPlan(ImagingCriteriaAnalyzer analyzer, DateTime transitTime, double moonIllumination,
                                       double moonSeparation, double moonAvoidanceSeparation) {
            return new ImagingDayPlan(analyzer.StartImagingTime, analyzer.EndImagingTime, transitTime,
                                      analyzer.StartLimitingFactor, analyzer.EndLimitingFactor, moonIllumination,
                                      moonSeparation, moonAvoidanceSeparation);
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
