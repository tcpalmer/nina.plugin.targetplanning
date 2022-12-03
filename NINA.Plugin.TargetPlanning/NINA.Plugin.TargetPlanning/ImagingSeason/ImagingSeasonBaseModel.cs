using NINA.Core.Utility.ColorSchema;
using NINA.Profile.Interfaces;
using OxyPlot;
using OxyPlot.Axes;
using System.Collections.Generic;
using System.Windows.Media;
using TargetPlanning.NINAPlugin.Astrometry;

namespace TargetPlanning.NINAPlugin.ImagingSeason {

    public abstract class ImagingSeasonBaseModel {

        public PlotModel PlotModel { get; set; }
        public PlotController PlotController { get; private set; }

        protected IList<ImagingDayPlan> results { get; private set; }
        protected ColorSchema colorSchema { get; private set; }

        public ImagingSeasonBaseModel(IProfile profile, PlanParameters planParams, IList<ImagingDayPlan> results) {
            this.colorSchema = profile.ColorSchemaSettings.ColorSchema;
            this.results = TrimStartEnd((List<ImagingDayPlan>)results);

            PlotController = new PlotController();
        }

        protected Axis GetXAxis() {
            return new DateTimeAxis {
                IntervalType = DateTimeIntervalType.Months,
                IsPanEnabled = false,
                IsZoomEnabled = false,
                AxislineColor = ConvertColor(colorSchema.PrimaryColor),
                MajorGridlineStyle = LineStyle.LongDash,
                MajorGridlineColor = ConvertColor(colorSchema.PrimaryColor, 60),
                MinorIntervalType = DateTimeIntervalType.Weeks,
                Position = AxisPosition.Bottom,
                StringFormat = "MMM",
                TextColor = ConvertColor(colorSchema.PrimaryColor),
                TicklineColor = ConvertColor(colorSchema.SecondaryColor),
            };
        }

        protected OxyColor ConvertColor(Color color, byte alpha) {
            return OxyColor.FromArgb(alpha, color.R, color.G, color.B);
        }

        protected OxyColor ConvertColor(Color color) {
            return OxyColor.FromRgb(color.R, color.G, color.B);
        }

        public static IList<ImagingDayPlan> TrimStartEnd(List<ImagingDayPlan> list) {
            int firstAccepted = 0, lastAccepted = 0;

            for (int i = 0; i < list.Count; i++) {
                if (list[i].IsAccepted()) {
                    firstAccepted = i;
                    break;
                }
            }

            for (int i = list.Count - 1; i >= 0; i--) {
                if (list[i].IsAccepted()) {
                    lastAccepted = i;
                    break;
                }
            }

            if (lastAccepted == 0) {
                list.Clear();
                return list;
            }

            return list.GetRange(firstAccepted, lastAccepted - firstAccepted + 1);
        }
    }

}
