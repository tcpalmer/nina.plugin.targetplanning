using NINA.Astrometry;
using OxyPlot;
using OxyPlot.Axes;
using System;
using System.Collections.Generic;
using TargetPlanning.NINAPlugin.Astrometry;

namespace TargetPlanning.NINAPlugin.ImagingSeason {

    public class ImagingSeasonChartModel {

        public string TargetName { get; private set; }
        public IList<DataPoint> ImagingMinutes { get; private set; }
        public IList<bool> Accepted { get; private set; }

        private long _maxImagingMinutes;
        public long MaxImagingMinutes {
            get => (long)(_maxImagingMinutes * 1.1);
        }

        public ImagingSeasonChartModel(DeepSkyObject target, IEnumerable<ImagingDayPlan> results) {
            TargetName = target.Name != null ? target.Name : "manually entered";
            ImagingMinutes = new List<DataPoint>(366);
            Accepted = new List<bool>(366);
            _maxImagingMinutes = long.MinValue;

            foreach (ImagingDayPlan plan in results) {
                bool accepted = !plan.StartLimitingFactor.Session;
                long imagingMinutes = accepted ? plan.GetImagingMinutes() : 0;
                DateTime displayImagingMinutes = new DateTime(plan.StartDay.Year, plan.StartDay.Month, plan.StartDay.Day).AddMinutes(imagingMinutes);
                ImagingMinutes.Add(new DataPoint(DateTimeAxis.ToDouble(plan.StartDay), imagingMinutes));
                Accepted.Add(accepted);
                if (imagingMinutes > _maxImagingMinutes) {
                    _maxImagingMinutes = imagingMinutes;
                }
            }
        }
    }

}
