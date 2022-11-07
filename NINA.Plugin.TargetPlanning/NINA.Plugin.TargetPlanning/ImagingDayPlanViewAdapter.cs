using NINA.Astrometry;
using NINA.Core.Utility;
using System;
using TargetPlanning.NINAPlugin.Astrometry;

namespace TargetPlanning.NINAPlugin {

    public class ImagingDayPlanViewAdapter {

        private static readonly string OK_COLOR = "White";
        private static readonly string VIOLATION_COLOR = "Red";

        public string StatusMessage { get => plan.GetStatusMessage(); }
        public bool Status { get => GetStatus(); }

        public DateTime StartImagingTime { get => plan.StartImagingTime; }
        public DateTime EndImagingTime { get => plan.EndImagingTime; }

        public string ImagingTime { get => GetTimeHM(); }
        public string ImagingTimeColor { get => GetImagingTimeColor(); }

        public string StartLimitingFactor { get => plan.StartLimitingFactor.Name; }
        public string EndLimitingFactor { get => plan.EndLimitingFactor.Name; }

        public double MoonIllumination { get => plan.MoonIllumination * 100; }
        public string MoonIlluminationColor { get => GetMoonIlluminationColor(); }

        public double MoonSeparation { get => plan.MoonSeparation; }
        public string MoonSeparationColor { get => GetMoonSeparationColor(); }

        public DeepSkyObject Target { get => GetTarget(); }
        public NighttimeData PlanNighttimeData { get => GetPlanNighttimeData(); }

        private ImagingDayPlan plan;
        private ImagingDayPlanContext context;

        public ImagingDayPlanViewAdapter(ImagingDayPlan plan, ImagingDayPlanContext context) {
            Validate.Assert.notNull(plan, "plan cannot be null");
            Validate.Assert.notNull(context, "context cannot be null");

            this.plan = plan;
            this.context = context;
        }

        private bool GetStatus() {
            return !plan.StartLimitingFactor.Session;
        }

        private string GetTimeHM() {
            return Utils.MtoHM((int)plan.GetImagingMinutes());
        }

        private string GetImagingTimeColor() {
            return (plan.GetImagingMinutes() < context.PlanParameters.MinimumImagingTime) ? VIOLATION_COLOR : OK_COLOR;
        }

        private string GetMoonIlluminationColor() {
            if (context.PlanParameters.MaximumMoonIllumination == 0) {
                return OK_COLOR;
            }

            return (plan.MoonIllumination > context.PlanParameters.MaximumMoonIllumination) ? VIOLATION_COLOR : OK_COLOR;
        }

        private string GetMoonSeparationColor() {
            if (context.PlanParameters.MinimumMoonSeparation == 0) {
                return OK_COLOR;
            }

            return (plan.MoonSeparation < context.PlanParameters.MinimumMoonSeparation) ? VIOLATION_COLOR : OK_COLOR;
        }

        private NighttimeData GetPlanNighttimeData() {
            Logger.Debug("GetNighttimeData");
            NighttimeData data = context.NighttimeCalculator.Calculate(GetReferenceDate());
            AsyncObservableCollection<OxyPlot.DataPoint> points = data.NauticalTwilightDuration;
            Logger.Debug($"NT: {points.Count}");
            foreach (OxyPlot.DataPoint point in points) {
                Logger.Debug($"  NT-p: {point.X} {point.Y}");
            }
            return data;
        }

        private DeepSkyObject GetTarget() {
            Logger.Debug("GetTarget");
            DateTime refDate = GetReferenceDate();
            DeepSkyObject target = context.PlanParameters.Target;
            target.SetDateAndPosition(refDate, context.PlanParameters.ObserverInfo.Latitude, context.PlanParameters.ObserverInfo.Longitude);
            target.SetCustomHorizon(context.Profile.ActiveProfile.AstrometrySettings.Horizon);
            //target.Refresh(); SetCustomHorizon already does an update - no need for another
            return target;
        }

        private DateTime GetReferenceDate() {
            DateTime date = plan.StartImagingTime;
            return new DateTime(date.Year, date.Month, date.Day, 12, 0, 0, date.Kind);
        }
    }

}
