
using NINA.Astrometry;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using TargetPlanning.NINAPlugin.Astrometry;

namespace TargetPlanning.NINAPlugin {

    public class ImagingDayPlanViewAdapter {

        public string StatusMessage { get => plan.GetStatusMessage(); }
        public bool Status { get => GetStatus(); }

        public string StartImagingTime { get => DateFmt(plan.StartImagingTime); }
        public string EndImagingTime { get => DateFmt(plan.EndImagingTime); }
        public string ImagingTime { get => GetTimeHM(); }

        public string StartLimitingFactor { get => plan.StartLimitingFactor.Name; }
        public string EndLimitingFactor { get => plan.EndLimitingFactor.Name; }

        public string MoonIllumination { get => GetMoonIlluminationFormatted(); }
        public string MoonSeparation { get => GetMoonSeparationFormatted(); }

        private ImagingDayPlan plan;

        public ImagingDayPlanViewAdapter(ImagingDayPlan plan) {
            Validate.Assert.notNull(plan, "plan cannot be null");
            this.plan = plan;
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

        private string GetMoonIlluminationFormatted() {
            return String.Format("{0:F0}%", plan.MoonIllumination * 100);
        }

        private string GetMoonSeparationFormatted() {
            return String.Format("{0:F0}°", plan.MoonSeparation);
        }
    }

}
