using NINA.Astrometry;
using OxyPlot;
using OxyPlot.Axes;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Media;
using TargetPlanning.NINAPlugin.Astrometry;

namespace TargetPlanning.NINAPlugin.AnnualChart {

    public class AnnualPlanningChartModel {

        public string TargetName { get; private set; }
        public IList<DataPoint> TargetAltitudes { get; private set; }
        public IList<DataPoint> MoonAltitudes { get; private set; }

        public Color MoonColor { get; private set; }
        public double StartDate { get; private set; }
        public string AnnoText { get; private set; }
        public DataPoint AnnoPoint { get; private set; }

        public AnnualPlanningChartModel(ObserverInfo location, DeepSkyObject target, DateTime startTime, CancellationToken token) {

            MoonColor = Color.FromRgb(Colors.Gold.R, Colors.Gold.G, Colors.Gold.B);
            AnnoText = startTime.Year.ToString();
            DateTime dt = new DateTime(startTime.Year, 1, 12);
            AnnoPoint = new DataPoint(DateTimeAxis.ToDouble(dt), 82);
            StartDate = DateTimeAxis.ToDouble(startTime);

            TargetName = target.Name != null ? target.Name : "User coords";
            TargetAltitudes = new List<DataPoint>(366);
            MoonAltitudes = new List<DataPoint>(366);

            int daysInYear = DateTime.IsLeapYear(startTime.Year) ? 366 : 365;
            DateTime dateTime = new DateTime(startTime.Year, 1, 1, 0, 0, 0, DateTimeKind.Local);
            for (int i = 0; i < daysInYear; i++) {

                if (token.IsCancellationRequested) {
                    throw new OperationCanceledException();
                }

                HorizontalCoordinate hc = AstrometryUtils.GetHorizontalCoordinates(location, target.Coordinates, dateTime);
                TargetAltitudes.Add(new DataPoint(DateTimeAxis.ToDouble(dateTime), hc.Altitude));
                dateTime = dateTime.AddDays(1);
            }

            dateTime = new DateTime(startTime.Year, 1, 1, 0, 0, 0, DateTimeKind.Local);
            for (int i = 0; i < daysInYear; i++) {

                if (token.IsCancellationRequested) {
                    throw new OperationCanceledException();
                }

                double altitude = AstroUtil.GetMoonAltitude(dateTime, location);
                MoonAltitudes.Add(new DataPoint(DateTimeAxis.ToDouble(dateTime), altitude));
                dateTime = dateTime.AddDays(1);
            }
        }

    }

}
