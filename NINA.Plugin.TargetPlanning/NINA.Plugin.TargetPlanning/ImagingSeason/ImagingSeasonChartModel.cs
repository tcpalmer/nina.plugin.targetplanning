using NINA.Astrometry;
using OxyPlot;
using OxyPlot.Axes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TargetPlanning.NINAPlugin.Astrometry;

namespace TargetPlanning.NINAPlugin.ImagingSeason {

    public class ImagingSeasonChartModel {

        public string TargetName { get; private set; }
        public IList<DataPoint> ImagingMinutes { get; private set; }
        public IList<bool> Accepted { get; private set; }
        public string AnnoText { get; private set; }
        public DataPoint AnnoPoint { get; private set; }

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
                ImagingMinutes.Add(new DataPoint(DateTimeAxis.ToDouble(plan.StartDay), TimeSpanAxis.ToDouble(TimeSpan.FromMinutes(imagingMinutes))));
                Accepted.Add(accepted);
                if (imagingMinutes > _maxImagingMinutes) {
                    _maxImagingMinutes = imagingMinutes;
                }
            }

            int startYear = results.First().StartDay.Year;
            StringBuilder sb = new StringBuilder();
            sb.Append(startYear.ToString());
            int endYear = results.Last().StartDay.Year;
            if (startYear != endYear) {
                sb.Append("-").Append(endYear.ToString());
            }

            AnnoText = sb.ToString();
            DateTime dt = results.First().StartDay.AddDays(15);
            AnnoPoint = new DataPoint(DateTimeAxis.ToDouble(dt), TimeSpanAxis.ToDouble(TimeSpan.FromMinutes(MaxImagingMinutes * .87)));
        }
    }

}
