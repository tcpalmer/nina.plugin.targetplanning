using NINA.Astrometry;
using NINA.Core.Model;
using System;
using System.IO;
using TargetPlanning.NINAPlugin.Astrometry;

namespace TargetPlanning.NINAPlugin {

    public class ImagingDayPlanViewAdapter {

        public string StatusMessage { get => plan.GetStatusMessage(); }
        public bool Status { get => GetStatus(); }

        public DateTime StartImagingTime { get => plan.StartImagingTime; }
        public DateTime EndImagingTime { get => plan.EndImagingTime; }
        public DateTime TransitTime { get => plan.TransitTime; }

        public string ImagingTime { get => GetTimeHM(); }
        public string ImagingTimeColor { get => GetImagingTimeColor(); }

        public string StartLimitingFactor { get => plan.StartLimitingFactor.Name; }
        public string EndLimitingFactor { get => plan.EndLimitingFactor.Name; }

        public double MoonIllumination { get => plan.MoonIllumination * 100; }
        public string MoonIlluminationColor { get => GetMoonIlluminationColor(); }

        public double MoonSeparation { get => plan.MoonSeparation; }
        public string MoonSeparationColor { get => GetMoonSeparationColor(); }

        public double MoonAvoidanceSeparation { get => plan.MoonAvoidanceSeparation; }
        public string MoonAvoidanceSeparationDisplay { get => GetMoonAvoidanceSeparationDisplay(); }
        public string MoonAvoidanceSeparationColor { get => GetMoonAvoidanceSeparationColor(); }

        public DeepSkyObject Target { get => GetTarget(); }
        public NighttimeData NighttimeData { get => GetPlanNighttimeData(); }

        private ImagingDayPlan plan;
        private ImagingDayPlanContext context;
        private DeepSkyObject target;

        private string primaryColor;
        private string errorColor;

        public ImagingDayPlanViewAdapter(ImagingDayPlan plan, ImagingDayPlanContext context) {
            Validate.Assert.notNull(plan, "plan cannot be null");
            Validate.Assert.notNull(context, "context cannot be null");

            this.plan = plan;
            this.context = context;

            DeepSkyObject dso = context.PlanParameters.Target;
            this.target = new DeepSkyObject(dso.Id, dso.Coordinates, null, GetCustomHorizon());

            primaryColor = context.Profile.ActiveProfile.ColorSchemaSettings.ColorSchema.PrimaryColor.ToString();
            errorColor = context.Profile.ActiveProfile.ColorSchemaSettings.ColorSchema.NotificationErrorColor.ToString();
        }

        private bool GetStatus() {
            return !plan.StartLimitingFactor.Session;
        }

        private string GetTimeHM() {
            return Utils.MtoHM((int)plan.GetImagingMinutes());
        }

        private string GetImagingTimeColor() {
            return (plan.GetImagingMinutes() < context.PlanParameters.MinimumImagingTime) ? errorColor : primaryColor;
        }

        private string GetMoonIlluminationColor() {
            if (context.PlanParameters.MaximumMoonIllumination == 0) {
                return primaryColor;
            }

            return (plan.MoonIllumination > context.PlanParameters.MaximumMoonIllumination) ? errorColor : primaryColor;
        }

        private string GetMoonSeparationColor() {
            if (context.PlanParameters.MinimumMoonSeparation == 0) {
                return primaryColor;
            }

            return (plan.MoonSeparation < context.PlanParameters.MinimumMoonSeparation) ? errorColor : primaryColor;
        }

        private string GetMoonAvoidanceSeparationDisplay() {
            if (!context.PlanParameters.MoonAvoidanceEnabled || plan.MoonAvoidanceSeparation == double.MinValue) {
                return "n/a";
            }

            return String.Format("{0:F0}°", plan.MoonAvoidanceSeparation);
        }

        private string GetMoonAvoidanceSeparationColor() {
            if (!context.PlanParameters.MoonAvoidanceEnabled) {
                return primaryColor;
            }

            return (plan.MoonAvoidanceSeparation < context.PlanParameters.MinimumMoonSeparation) ? errorColor : primaryColor;
        }

        private NighttimeData GetPlanNighttimeData() {
            return context.NighttimeCalculator.Calculate(GetReferenceDate());
        }

        private DeepSkyObject GetTarget() {
            target.SetDateAndPosition(GetReferenceDate(), context.PlanParameters.ObserverInfo.Latitude, context.PlanParameters.ObserverInfo.Longitude);
            target.Refresh();
            return target;
        }

        private DateTime GetReferenceDate() {
            return NighttimeCalculator.GetReferenceDate(plan.StartDay).AddDays(1);
        }

        private CustomHorizon GetCustomHorizon() {
            HorizonDefinition hd = context.PlanParameters.HorizonDefinition;
            return hd.IsCustom() ? context.Profile.ActiveProfile.AstrometrySettings.Horizon : GetConstantHorizon(hd.GetFixedMinimumAltitude());
        }

        private CustomHorizon GetConstantHorizon(double altitude) {
            string alt = String.Format("{0:F0}", altitude);
            string horizonDefinition = $"0 {alt}" + Environment.NewLine
            + $"90 {alt}" + Environment.NewLine
            + $"180 {alt}" + Environment.NewLine
            + $"270 {alt}";

            using (var sr = new StringReader(horizonDefinition)) {
                return CustomHorizon.FromReader_Standard(sr);
            }
        }
    }

}
