using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Threading;

namespace TargetPlanning.NINAPlugin.Astrometry {

    public class OptimalImagingSeason {

        public OptimalImagingSeason() {
        }

        public IList<ImagingDayPlan> GetOptimalSeason(PlanParameters planParams, CancellationToken token) {

            // Be sure this target rises at this location at all
            if (!AstrometryUtils.RisesAtLocation(planParams.ObserverInfo, planParams.Target.Coordinates)) {
                return null;
            }

            // The date of midnight transit is taken to be the approximate middle of the imaging season
            DateTime transitMidnightDate = AstrometryUtils.GetMidnightTransitDate(planParams.ObserverInfo, planParams.Target.Coordinates, planParams.StartDate.Year, token);
            Logger.Trace($"midnight transit date: {transitMidnightDate}");

            // If the target is circumpolar, heliacal rising/setting are undefined - so assume whole year
            if (AstrometryUtils.CircumpolarAtLocation(planParams.ObserverInfo, planParams.Target.Coordinates)) {
                DateTime startDate = transitMidnightDate.AddDays(-183);
                DateTime endDate = startDate.AddYears(1);

                Logger.Trace($"circumpolar start date: {startDate}");
                Logger.Trace($"circumpolar end date:   {endDate}");

                planParams.PlanDays = Math.Abs((endDate - startDate).Days);
                planParams.StartDate = startDate;
            }

            // Otherwise, we can use heliacal rising/setting dates to determine the limits of the imaging season
            else {
                HeliacalSolver solver = new HeliacalSolver(planParams.ObserverInfo, planParams.Target.Coordinates, transitMidnightDate, planParams.TwilightInclude);
                DateTime heliacalRisingDate = solver.GetHeliacalRisingDate(token);
                DateTime heliacalSettingDate = solver.GetHeliacalSettingDate(token);

                Logger.Trace($"heliacal rising date:  {heliacalRisingDate}");
                Logger.Trace($"heliacal setting date: {heliacalSettingDate}");

                planParams.PlanDays = Math.Abs((heliacalSettingDate - heliacalRisingDate).Days);
                planParams.StartDate = heliacalRisingDate;
            }

            return new PlanGenerator(planParams).Generate(token);
        }
    }

}
