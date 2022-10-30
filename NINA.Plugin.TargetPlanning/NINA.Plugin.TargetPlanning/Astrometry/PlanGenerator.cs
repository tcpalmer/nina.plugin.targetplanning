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

        public IEnumerable<ImagingDay> Generate() {
            Logger.Debug($"Starting Target Planning for: {PlanParameters.StartDate}, {PlanParameters.PlanDays} days");
            Logger.Debug($"  Target: {PlanParameters.Target.Name} RA: {PlanParameters.Target.Coordinates.RA} Dec: {PlanParameters.Target.Coordinates.Dec}");
            Logger.Trace($"  Location Lat: {PlanParameters.ObserverInfo.Latitude} Long: {PlanParameters.ObserverInfo.Longitude}, Ele: {PlanParameters.ObserverInfo.Elevation}");

            DeepSkyObject target = PlanParameters.Target;
            ObserverInfo location = PlanParameters.ObserverInfo;

            List<ImagingDay> result = new List<ImagingDay>();
            List<RiseAndSetEvent> twilightTimes = getTwiLightTimesList(PlanParameters.StartDate, PlanParameters.PlanDays, PlanParameters.ObserverInfo);

            for (int i = 0; i < twilightTimes.Count-1; i++) {
                RiseAndSetEvent day1 = twilightTimes[i];
                RiseAndSetEvent day2 = twilightTimes[i+1];

                // Get the presumptive start and end times for this 'imaging day'
                DateTime startTime = (DateTime) day1.Set;
                DateTime endTime = (DateTime) day2.Rise;

                // TODO: Moon: AstroUtil.GetMoonPositionAngle()
                // TODO: Moon: AstroUtil.CalculateMoonIllumination()

                /* TODO: ...
            TargetImagingCircumstances circumstances = new TargetImagingCircumstances(environment.getLocation(),
                                                                                      environment.getTarget(),
                                                                                      startTime, endTime,
                                                                                      environment.getMinimumAltitude());
            int status = circumstances.analyze();

            // Check if the target is visible at all
            if (status != TargetImagingCircumstances.STATUS_POTENTIALLY_VISIBLE) {
                ImagingDayPlan plan = new ImagingDayPlan(null, null, null, ImagingDayPlan.ImagingLimit.NOT_VISIBLE,
                                                         ImagingDayPlan.ImagingLimit.NOT_VISIBLE, 0, 0);
                plan.setStartDay(startTime);
                plan.setEndDay(endTime);

                imagingDayList.add(plan);
                continue;
            }

            ZonedDateTime startImagingTime = circumstances.getRiseAboveMinimumTime();
            ZonedDateTime endImagingTime = circumstances.getSetBelowMinimumTime();
            ImagingCriteriaAnalyzer analyzer = new ImagingCriteriaAnalyzer(startImagingTime, endImagingTime);

            // Adjust for twilight
            analyzer.adjustForTwilight(startTime, endTime);

            // Adjust for meridian proximity criteria
            ZonedDateTime transitTime = circumstances.getTransitTime();
            analyzer.adjustForMeridianProximity(transitTime, environment.getMeridianProximityTime());

            // Calculate moon metrics here so available if rejected early
            ZonedDateTime midPointTime = getMidpointTime(analyzer.getStartImagingTime(), analyzer.getEndImagingTime());
            double moonIllumination = Astrometry.getMoonIllumination(midPointTime);
            double moonSeparation = Math.toDegrees(
                    Astrometry.getMoonSeparationAngle(environment.getLocation(), midPointTime,
                                                      environment.getTarget()));

            // Stop if already rejected
            if (analyzer.sessionIsRejected()) {
                imagingDayList.add(getPlan(analyzer, transitTime, moonIllumination, moonSeparation));
                continue;
            }

            // Accept/reject for moon illumination criteria
            analyzer.adjustForMoonIllumination(moonIllumination, environment.getMaximumMoonIllumination());

            // Stop if already rejected
            if (analyzer.sessionIsRejected()) {
                imagingDayList.add(getPlan(analyzer, transitTime, moonIllumination, moonSeparation));
                continue;
            }

            // Accept/reject for moon separation criteria
            analyzer.adjustForMoonSeparation(moonSeparation, environment.getMinimumMoonSeparation());

            // Stop if already rejected
            if (analyzer.sessionIsRejected()) {
                imagingDayList.add(getPlan(analyzer, transitTime, moonIllumination, moonSeparation));
                continue;
            }

            // Adjust for local horizon clip
            // TODO: horizon

            // Finally, accept/reject for minimum imaging time criteria
            analyzer.adjustForMinimumImagingTime(environment.getMinimumImagingTime());

            imagingDayList.add(getPlan(analyzer, transitTime, moonIllumination, moonSeparation));
                 */

                result.Add(new ImagingDay(startTime, endTime, PlanParameters.ObserverInfo, PlanParameters.Target.Coordinates));
            }

            Logger.Debug("Target Planning Complete");
            return result;
        }

        private List<RiseAndSetEvent> getTwiLightTimesList(DateTime StartDate, int PlanDays, ObserverInfo location) {

            List<RiseAndSetEvent> list = new List<RiseAndSetEvent>(PlanDays+1);
            DateTime date = StartDate;

            for (int i = 0; i <= PlanDays; i++) {
                list.Add(AstroUtil.GetNauticalNightTimes(date, location.Latitude, location.Longitude));
                date = date.AddDays(1);
            }

            return list;
        }
    }

    public class PlanParameters {
        public DeepSkyObject Target { get; set; }
        public ObserverInfo ObserverInfo { get; set; }
        public DateTime StartDate { get; set; }
        public int PlanDays { get; set; }
        public double MinimumAltitude { get; set; }
        public int MinimumTime { get; set; }
        public double MinimumMoonSeparation { get; set; }
        public double MaximumMoonIllumination { get; set; }
        public int MeridianTimeSpan { get; set; }
    }

}
