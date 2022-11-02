
using System;
using TargetPlanning.NINAPlugin.Astrometry;

namespace TargetPlanning.NINAPlugin {

    public class ImagingDayPlanViewAdapter {

        private static readonly string OK_COLOR = "White";
        private static readonly string VIOLATION_COLOR = "Red";

        public string StatusMessage { get => plan.GetStatusMessage(); }
        public bool Status { get => GetStatus(); }

        public string StartImagingTime { get => DateFmt(plan.StartImagingTime); }
        public string EndImagingTime { get => DateFmt(plan.EndImagingTime); }

        public string ImagingTime { get => GetTimeHM(); }
        public string ImagingTimeColor { get => GetImagingTimeColor(); }

        public string StartLimitingFactor { get => plan.StartLimitingFactor.Name; }
        public string EndLimitingFactor { get => plan.EndLimitingFactor.Name; }

        public string MoonIllumination { get => GetMoonIlluminationFormatted(); }
        public string MoonIlluminationColor { get => GetMoonIlluminationColor(); }

        public string MoonSeparation { get => GetMoonSeparationFormatted(); }
        public string MoonSeparationColor { get => GetMoonSeparationColor(); }

        private ImagingDayPlan plan;
        private PlanParameters planParameters;

        public ImagingDayPlanViewAdapter(ImagingDayPlan plan, PlanParameters planParameters) {
            Validate.Assert.notNull(plan, "plan cannot be null");
            Validate.Assert.notNull(planParameters, "planParameters cannot be null");

            this.plan = plan;
            this.planParameters = planParameters;
        }

        private bool GetStatus() {
            return !plan.StartLimitingFactor.Session;
        }

        private string DateFmt(DateTime dt) {
            return dt.ToString("MM/dd/yyyy HH:mm:ss");
        }

        private string GetTimeHM() {
            return Utils.MtoHM((int)plan.GetImagingMinutes());
        }

        private string GetImagingTimeColor() {
            return (plan.GetImagingMinutes() < planParameters.MinimumImagingTime) ? VIOLATION_COLOR : OK_COLOR;
        }

        private string GetMoonIlluminationFormatted() {
            return String.Format("{0:F0}%", plan.MoonIllumination * 100);
        }

        private string GetMoonIlluminationColor() {
            if (planParameters.MaximumMoonIllumination == 0) {
                return OK_COLOR;
            }

            return (plan.MoonIllumination > planParameters.MaximumMoonIllumination) ? VIOLATION_COLOR : OK_COLOR;
        }

        private string GetMoonSeparationFormatted() {
            return String.Format("{0:F0}°", plan.MoonSeparation);
        }

        private string GetMoonSeparationColor() {
            if (planParameters.MinimumMoonSeparation == 0) {
                return OK_COLOR;
            }

            return (plan.MoonSeparation < planParameters.MinimumMoonSeparation) ? VIOLATION_COLOR : OK_COLOR;
        }

    }

}
