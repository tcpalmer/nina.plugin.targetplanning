using Accord.Math.Environments;
using NINA.Astrometry;
using NINA.Astrometry.RiseAndSet;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;

namespace TargetPlanning.NINAPlugin.Astrometry {

    public class PlanGenerator {
        private PlanParameters PlanParameters;

        public PlanGenerator(PlanParameters planParameters) {
            this.PlanParameters = planParameters;
        }

        public IEnumerable<ImagingDayPlan> Generate() {
            Logger.Debug($"Starting Target Planning for: {PlanParameters.StartDate}, {PlanParameters.PlanDays} days");
            Logger.Debug($"  Target: {PlanParameters.Target.Name} RA: {PlanParameters.Target.Coordinates.RA} Dec: {PlanParameters.Target.Coordinates.Dec}");
            Logger.Trace($"  Location Lat: {PlanParameters.ObserverInfo.Latitude} Long: {PlanParameters.ObserverInfo.Longitude}, Ele: {PlanParameters.ObserverInfo.Elevation}");

            List<ImagingDayPlan> imagingDayList = new List<ImagingDayPlan>();

            DeepSkyObject target = PlanParameters.Target;
            ObserverInfo location = PlanParameters.ObserverInfo;

            List<RiseAndSetEvent> twilightTimes = getTwiLightTimesList(PlanParameters.StartDate, PlanParameters.PlanDays, PlanParameters.ObserverInfo);

            for (int i = 0; i < twilightTimes.Count - 1; i++) {
                RiseAndSetEvent day1 = twilightTimes[i];
                RiseAndSetEvent day2 = twilightTimes[i + 1];

                // Get the presumptive start and end times for this 'imaging day'
                DateTime startTime = (DateTime)day1.Set;
                DateTime endTime = (DateTime)day2.Rise;

                TargetImagingCircumstances circumstances = new TargetImagingCircumstances(location,
                                                                                          target.Coordinates,
                                                                                          startTime, endTime,
                                                                                          PlanParameters.MinimumAltitude);
                int status = circumstances.Analyze();

                // Check if the target is visible at all
                if (status != TargetImagingCircumstances.STATUS_POTENTIALLY_VISIBLE) {
                    ImagingDayPlan plan = new ImagingDayPlan(startTime, endTime, startTime.AddMinutes(1), ImagingLimit.NotVisible,
                                                             ImagingLimit.NotVisible, 0, 0);
                    imagingDayList.Add(plan);
                    continue;
                }

                DateTime startImagingTime = circumstances.RiseAboveMinimumTime;
                DateTime endImagingTime = circumstances.SetBelowMinimumTime;
                ImagingCriteriaAnalyzer analyzer = new ImagingCriteriaAnalyzer(startImagingTime, endImagingTime);

                // Adjust for twilight
                analyzer.AdjustForTwilight(startTime, endTime);

                // Adjust for meridian proximity criteria
                DateTime transitTime = circumstances.TransitTime;
                analyzer.AdjustForMeridianProximity(transitTime, PlanParameters.MeridianTimeSpan);

                // Calculate moon metrics here so available if rejected early
                DateTime midPointTime = GetMidpointTime(analyzer.StartImagingTime, analyzer.EndImagingTime);
                double moonIllumination = AstrometryUtils.GetMoonIllumination(midPointTime);
                double moonSeparation = AstrometryUtils.GetMoonSeparationAngle(location, midPointTime, target.Coordinates);

                // Stop if already rejected
                if (analyzer.SessionIsRejected()) {
                    imagingDayList.Add(GetPlan(analyzer, transitTime, moonIllumination, moonSeparation));
                    continue;
                }

                // Accept/reject for moon illumination criteria
                analyzer.AdjustForMoonIllumination(moonIllumination, PlanParameters.MaximumMoonIllumination);

                // Stop if already rejected
                if (analyzer.SessionIsRejected()) {
                    imagingDayList.Add(GetPlan(analyzer, transitTime, moonIllumination, moonSeparation));
                    continue;
                }

                // Accept/reject for moon separation criteria
                analyzer.AdjustForMoonSeparation(moonSeparation, PlanParameters.MinimumMoonSeparation);

                // Stop if already rejected
                if (analyzer.SessionIsRejected()) {
                    imagingDayList.Add(GetPlan(analyzer, transitTime, moonIllumination, moonSeparation));
                    continue;
                }

                // Adjust for local horizon clip
                // TODO: horizon

                // Finally, accept/reject for minimum imaging time criteria
                analyzer.AdjustForMinimumImagingTime(PlanParameters.MinimumImagingTime);

                imagingDayList.Add(GetPlan(analyzer, transitTime, moonIllumination, moonSeparation));
            }

            Logger.Debug("Target Planning Complete");
            return imagingDayList;
        }

        private List<RiseAndSetEvent> getTwiLightTimesList(DateTime StartDate, int PlanDays, ObserverInfo location) {

            List<RiseAndSetEvent> list = new List<RiseAndSetEvent>(PlanDays + 1);
            DateTime date = StartDate;

            for (int i = 0; i <= PlanDays; i++) {
                list.Add(AstroUtil.GetNauticalNightTimes(date, location.Latitude, location.Longitude));
                date = date.AddDays(1);
            }

            return list;
        }

        private ImagingDayPlan GetPlan(ImagingCriteriaAnalyzer analyzer, DateTime transitTime, double moonIllumination,
                                       double moonSeparation) {
            return new ImagingDayPlan(analyzer.StartImagingTime, analyzer.EndImagingTime, transitTime,
                                      analyzer.StartLimitingFactor, analyzer.EndLimitingFactor, moonIllumination,
                                      moonSeparation);
        }

        public static DateTime GetMidpointTime(DateTime startImagingTime, DateTime endImagingTime) {
            long span = (long)endImagingTime.Subtract(startImagingTime).TotalSeconds;
            return startImagingTime.AddSeconds(span / 2);
        }
    }

    public class PlanParameters {
        public DeepSkyObject Target { get; set; }
        public ObserverInfo ObserverInfo { get; set; }
        public DateTime StartDate { get; set; }
        public int PlanDays { get; set; }
        public double MinimumAltitude { get; set; }
        public int MinimumImagingTime { get; set; }
        public double MinimumMoonSeparation { get; set; }
        public double MaximumMoonIllumination { get; set; }
        public int MeridianTimeSpan { get; set; }
    }

}
