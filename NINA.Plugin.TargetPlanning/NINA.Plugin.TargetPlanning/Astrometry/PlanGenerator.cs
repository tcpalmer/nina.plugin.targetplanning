using NINA.Astrometry;
using NINA.Astrometry.RiseAndSet;
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

            List<RiseAndSetEvent> twilightTimes = getTwiLightTimesList(PlanParameters.StartDate, PlanParameters.PlanDays, PlanParameters.ObserverInfo, token);

            using (MyStopWatch.Measure("planGenerate")) {
                for (int i = 0; i < twilightTimes.Count - 1; i++) {

                    if (token.IsCancellationRequested) {
                        throw new OperationCanceledException();
                    }

                    RiseAndSetEvent day1 = twilightTimes[i];
                    RiseAndSetEvent day2 = twilightTimes[i + 1];

                    // Get the presumptive start and end times for this 'imaging day'
                    DateTime startTime = (DateTime)day1.Set;
                    DateTime endTime = (DateTime)day2.Rise;

                    // Check the cache
                    ImagingDayPlan plan = imagingDayPlanCache.Get(startTime, endTime, PlanParameters);
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
                    DateTime midPointTime;
                    double moonIllumination;
                    double moonSeparation;
                    double moonAvoidanceSeparation = double.MinValue;

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

        private List<RiseAndSetEvent> getTwiLightTimesList(DateTime StartDate, int PlanDays, ObserverInfo location, CancellationToken token) {

            List<RiseAndSetEvent> list = new List<RiseAndSetEvent>(PlanDays + 1);
            DateTime date = StartDate;

            using (MyStopWatch.Measure("getTwiLightTimesList")) {

                for (int i = 0; i <= PlanDays; i++) {
                    if (token.IsCancellationRequested) {
                        throw new OperationCanceledException();
                    }

                    RiseAndSetEvent riseAndSetEvent = TwilightTimeCache.Get(date, location.Latitude, location.Longitude);
                    list.Add(riseAndSetEvent);
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
            sb.Append(MinimumMoonSeparation.ToString()).Append("_");
            sb.Append(MaximumMoonIllumination.ToString()).Append("_");
            sb.Append(MoonAvoidanceEnabled.ToString()).Append("_");
            sb.Append(MoonAvoidanceWidth.ToString());
            return sb.ToString();
        }
    }

}
