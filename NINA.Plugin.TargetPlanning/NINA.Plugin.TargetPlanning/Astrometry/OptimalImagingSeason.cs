using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Threading;
using TargetPlanning.NINAPlugin.ImagingSeason;

namespace TargetPlanning.NINAPlugin.Astrometry {

    public class OptimalImagingSeason {

        public OptimalImagingSeason() {
        }

        public ImagingSeasonChartModel GetOptimalSeason(PlanParameters planParams, CancellationToken token) {

            // Be sure this object rises at this location at all
            if (!AstrometryUtils.RisesAtLocation(planParams.ObserverInfo, planParams.Target.Coordinates)) {
                return null;
            }

            // TODO: if circumpolar, can't use heliacal rising/setting - assume all year?

            // We use heliacal rising/setting dates to determine the limits of the imaging season
            DateTime transitMidnightDate = AstrometryUtils.GetMidnightTransitDate(planParams.ObserverInfo, planParams.Target.Coordinates, planParams.StartDate.Year, token);
            HeliacalSolver solver = new HeliacalSolver(planParams.ObserverInfo, planParams.Target.Coordinates, transitMidnightDate);
            DateTime heliacalRisingDate = solver.GetHeliacalRisingDate(token);
            DateTime heliacalSettingDate = solver.GetHeliacalSettingDate(token);

            Logger.Trace($"midnight transit: {transitMidnightDate}");
            Logger.Trace($"heliacal rising:  {heliacalRisingDate}");
            Logger.Trace($"heliacal setting: {heliacalSettingDate}");

            planParams.PlanDays = Math.Abs((heliacalSettingDate - heliacalRisingDate).Days);
            planParams.StartDate = heliacalRisingDate;

            IEnumerable<ImagingDayPlan> results = new PlanGenerator(planParams).Generate(token);
            foreach (ImagingDayPlan plan in results) {
                bool rejected = plan.StartLimitingFactor.Session;
                Logger.Trace($"{plan.StartImagingTime}: {plan.GetImagingMinutes()} mins - {!rejected}");
            }

            // TODO: trim rejected days off each end

            // TODO: create a proper model
            ImagingSeasonChartModel model = new ImagingSeasonChartModel(planParams.Target, results);

            return model;
        }
    }

}
