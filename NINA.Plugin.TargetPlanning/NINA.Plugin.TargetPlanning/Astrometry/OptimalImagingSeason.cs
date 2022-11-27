using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Threading;

namespace TargetPlanning.NINAPlugin.Astrometry {

    public class OptimalImagingSeason {

        public OptimalImagingSeason() {
        }

        // TODO: this will likely return the model for the chart
        public string GetOptimalSeason(PlanParameters planParameters, CancellationToken token) {

            // Be sure this object rises at this location at all
            if (!AstrometryUtils.RisesAtLocation(planParameters.ObserverInfo, planParameters.Target.Coordinates)) {
                return null;
            }

            int originalPlanDays = planParameters.PlanDays;
            DateTime originalStartDate = planParameters.StartDate;

            // We use heliacal rising/setting dates to determine the limits of the imaging season
            DateTime transitMidnightDate = AstrometryUtils.GetMidnightTransitDate(planParameters.ObserverInfo, planParameters.Target.Coordinates, planParameters.StartDate.Year, token);
            HeliacalSolver solver = new HeliacalSolver(planParameters.ObserverInfo, planParameters.Target.Coordinates, transitMidnightDate);
            DateTime heliacalRisingDate = solver.GetHeliacalRisingDate(token);
            DateTime heliacalSettingDate = solver.GetHeliacalSettingDate(token);

            planParameters.PlanDays = Math.Abs((heliacalSettingDate - heliacalRisingDate).Days);

            heliacalRisingDate = new DateTime(planParameters.StartDate.Year, heliacalRisingDate.Month, heliacalRisingDate.Day);
            heliacalSettingDate = new DateTime(planParameters.StartDate.Year, heliacalSettingDate.Month, heliacalSettingDate.Day);

            planParameters.StartDate = heliacalRisingDate;

            IEnumerable<ImagingDayPlan> results = new PlanGenerator(planParameters).Generate(token);
            foreach (ImagingDayPlan plan in results) {
                bool rejected = plan.StartLimitingFactor.Session;
                Logger.Trace($"{plan.StartImagingTime}: {plan.GetImagingMinutes()} mins - {!rejected}");
            }

            // TODO: trim rejected days off each end

            planParameters.StartDate = originalStartDate;
            planParameters.PlanDays = originalPlanDays;

            // TODO: this will likely return the model for the chart
            return "FIX ME";
        }
    }

}
