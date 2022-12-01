using NINA.Core.Utility.ColorSchema;
using NINA.Profile.Interfaces;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using TargetPlanning.NINAPlugin.Astrometry;
using AreaSeries = OxyPlot.Series.AreaSeries;
using DateTimeAxis = OxyPlot.Axes.DateTimeAxis;
using TimeSpanAxis = OxyPlot.Axes.TimeSpanAxis;

namespace TargetPlanning.NINAPlugin.ImagingSeason {

    public class ImagingSeasonPlotModel {

        public PlotModel PlotModel { get; private set; }
        public PlotController PlotController { get; private set; }

        public ImagingSeasonPlotModel(IProfile profile, PlanParameters planParams, IList<ImagingDayPlan> results) {
            ColorSchema colorSchema = profile.ColorSchemaSettings.ColorSchema;
            results = TrimStartEnd((List<ImagingDayPlan>)results);

            PlotController = new PlotController();

            PlotModel = new PlotModel {
                Background = ConvertColor(colorSchema.BackgroundColor),
                PlotAreaBackground = ConvertColor(colorSchema.BackgroundColor),
                PlotAreaBorderColor = ConvertColor(colorSchema.BorderColor),
                TextColor = ConvertColor(colorSchema.PrimaryColor),
            };

            PlotModel.Axes.Add(new DateTimeAxis {
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
            });

            PlotModel.Axes.Add(new TimeSpanAxis {
                Title = "Imaging Hours",
                AxisTitleDistance = 12,
                IntervalLength = 30,
                IsPanEnabled = false,
                IsZoomEnabled = false,
                AxislineColor = ConvertColor(colorSchema.PrimaryColor),
                MajorGridlineStyle = LineStyle.LongDash,
                MajorGridlineColor = ConvertColor(colorSchema.PrimaryColor, 60),
                Minimum = 0,
                MaximumPadding = 0.07,
                MinorStep = 900,
                Position = AxisPosition.Left,
                StringFormat = "h:mm",
                TextColor = ConvertColor(colorSchema.PrimaryColor),
                TicklineColor = ConvertColor(colorSchema.SecondaryColor),
            });

            List<ImagingSpan> spans = GetImagingSpans((List<ImagingDayPlan>)results);
            long MaxImagingMinutes = long.MinValue;
            foreach (ImagingSpan span in spans) {
                AreaSeries areaSeries = GetAreaSeries(colorSchema, span.Accepted);

                int end = span.End < results.Count - 1 ? span.End + 1 : results.Count - 2;
                for (int i = span.Start; i <= end; i++) {
                    ImagingDayPlan plan = results[i];
                    areaSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(plan.StartDay), TimeSpanAxis.ToDouble(TimeSpan.FromMinutes(plan.GetImagingMinutes()))));
                    long imagingMinutes = plan.GetImagingMinutes();
                    if (imagingMinutes > MaxImagingMinutes) {
                        MaxImagingMinutes = imagingMinutes;
                    }
                }

                PlotModel.Series.Add(areaSeries);
            }

            int startYear = results.First().StartDay.Year;
            StringBuilder sb = new StringBuilder();
            sb.Append($"{planParams.Target.Name}\n");
            sb.Append(startYear.ToString());
            int endYear = results.Last().StartDay.Year;
            if (startYear != endYear) {
                sb.Append("-").Append(endYear.ToString());
            }

            TextAnnotation textAnnotation = new TextAnnotation {
                Text = sb.ToString(),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                TextColor = ConvertColor(colorSchema.PrimaryColor),
                TextPosition = new DataPoint(DateTimeAxis.ToDouble(results.First().StartDay.AddDays(15)),
                                             TimeSpanAxis.ToDouble(TimeSpan.FromMinutes(MaxImagingMinutes * .87))),
            };

            PlotModel.Annotations.Add(textAnnotation);

            /* TODO: Add proper Moon markers.  Can scan plan.MoonIllumination for maximums.
            LineAnnotation lineAnnotation = new LineAnnotation {
                Text = "Full Moon",
                TextColor = ConvertColor(colorSchema.NotificationErrorColor),
                LineStyle = LineStyle.Solid,
                StrokeThickness = 2,
                X = DateTimeAxis.ToDouble(new DateTime(2022, 5, 16)),
                Type = LineAnnotationType.Vertical,
                Color = ConvertColor(colorSchema.NotificationErrorColor),
            };

            PlotModel.Annotations.Add(lineAnnotation);
            */
        }

        private OxyColor ConvertColor(Color color, byte alpha) {
            return OxyColor.FromArgb(alpha, color.R, color.G, color.B);
        }

        private OxyColor ConvertColor(Color color) {
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

        public static List<ImagingSpan> GetImagingSpans(List<ImagingDayPlan> list) {
            List<ImagingSpan> spans = new List<ImagingSpan>();

            bool accepted = list.First().IsAccepted();
            ImagingSpan span = new ImagingSpan();
            span.Start = 0;
            span.Accepted = accepted;

            for (int i = 0; i < list.Count; i++) {
                if (list[i].IsAccepted() != accepted) {
                    span.End = i - 1;
                    spans.Add(span);

                    accepted = list[i].IsAccepted();
                    span = new ImagingSpan();
                    span.Start = i;
                    span.Accepted = accepted;
                }
            }

            span.End = list.Count - 1;
            spans.Add(span);

            return spans;
        }

        public AreaSeries GetAreaSeries(ColorSchema colorSchema, bool accepted) {

            if (accepted) {
                return new AreaSeries {
                    Color = ConvertColor(colorSchema.SecondaryColor),
                    Color2 = ConvertColor(colorSchema.PrimaryColor),
                    TextColor = ConvertColor(colorSchema.PrimaryColor),
                };
            }
            else {
                return new AreaSeries {
                    Color = ConvertColor(colorSchema.NotificationErrorColor),
                    Color2 = ConvertColor(colorSchema.NotificationErrorTextColor),
                    TextColor = ConvertColor(colorSchema.NotificationErrorTextColor),
                };
            }
        }

    }

    public class ImagingSpan {
        public int Start { get; set; }
        public int End { get; set; }
        public bool Accepted { get; set; }
    }

}