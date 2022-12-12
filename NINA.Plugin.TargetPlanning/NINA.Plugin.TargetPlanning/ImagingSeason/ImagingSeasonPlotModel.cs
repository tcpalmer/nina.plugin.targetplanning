using NINA.Core.Utility.ColorSchema;
using NINA.Profile.Interfaces;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using TargetPlanning.NINAPlugin.Astrometry;

namespace TargetPlanning.NINAPlugin.ImagingSeason {

    public class ImagingSeasonPlotModel : ImagingSeasonBaseModel {

        public ImagingSeasonPlotModel(IProfile profile, PlanParameters planParams, IList<ImagingDayPlan> results) : base(profile, planParams, results) {

            PlotModel = new PlotModel {
                Background = ConvertColor(colorSchema.BackgroundColor),
                PlotAreaBackground = ConvertColor(colorSchema.BackgroundColor),
                PlotAreaBorderColor = ConvertColor(colorSchema.BorderColor),
                TextColor = ConvertColor(colorSchema.PrimaryColor),
            };

            PlotModel.Axes.Add(GetXAxis());

            // Imaging Hours
            PlotModel.Axes.Add(new TimeSpanAxis {
                Title = "Imaging Hours",
                Key = "Imaging Hours",
                StartPosition = 0.4,
                EndPosition = 1,
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

            List<ImagingSpan> spans = GetImagingSpans((List<ImagingDayPlan>)TrimmedResults);
            long MaxImagingMinutes = long.MinValue;
            foreach (ImagingSpan span in spans) {
                AreaSeries areaSeries = GetAreaSeries(colorSchema, span.Accepted);

                int end = span.End < TrimmedResults.Count - 1 ? span.End + 1 : TrimmedResults.Count - 2;
                for (int i = span.Start; i <= end; i++) {
                    ImagingDayPlan plan = TrimmedResults[i];
                    areaSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(plan.StartDay), TimeSpanAxis.ToDouble(TimeSpan.FromMinutes(plan.GetImagingMinutes()))));
                    long imagingMinutes = plan.GetImagingMinutes();
                    if (imagingMinutes > MaxImagingMinutes) {
                        MaxImagingMinutes = imagingMinutes;
                    }
                }

                PlotModel.Series.Add(areaSeries);
            }

            // Moon Separation and Illumination
            PlotModel.Axes.Add(new LinearAxis {
                Title = "Moon Illumination",
                Key = "Moon Illumination",
                StartPosition = 0,
                EndPosition = 0.35,
                AxisTitleDistance = 12,
                IsPanEnabled = false,
                IsZoomEnabled = false,
                AxislineColor = ConvertColor(colorSchema.PrimaryColor),
                Minimum = 0,
                Maximum = 100,
                MaximumPadding = 0.07,
                MinorStep = 10,
                Position = AxisPosition.Left,
                TextColor = ConvertColor(colorSchema.PrimaryColor),
                TicklineColor = ConvertColor(colorSchema.SecondaryColor),
            });

            PlotModel.Axes.Add(new LinearAxis {
                Title = "Moon Separation",
                Key = "Moon Separation",
                StartPosition = 0,
                EndPosition = 0.35,
                AxisTitleDistance = 12,
                IsPanEnabled = false,
                IsZoomEnabled = false,
                AxislineColor = ConvertColor(colorSchema.PrimaryColor),
                Minimum = 0,
                Maximum = 180,
                MinorStep = 10,
                Position = AxisPosition.Right,
                TextColor = ConvertColor(colorSchema.PrimaryColor),
                TicklineColor = ConvertColor(colorSchema.SecondaryColor),
            });

            LineSeries illumSeries = new LineSeries {
                Title = "Moon Illumination",
                Color = ConvertColor(Colors.Gold, 90),
                TrackerFormatString = "Illumination\n{2:MM/dd/yyyy}\n{4:0}%",
                YAxisKey = "Moon Illumination",
            };

            LineSeries separationSeries = new LineSeries {
                Title = "Moon Separation",
                Color = ConvertColor(Colors.IndianRed, 90),
                TrackerFormatString = "Separation\n{2:MM/dd/yyyy}\n{4:0}°",
                YAxisKey = "Moon Separation",
            };

            foreach (ImagingDayPlan plan in TrimmedResults) {
                illumSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(plan.StartDay), plan.MoonIllumination * 100));
                separationSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(plan.StartDay), plan.MoonSeparation));
            }

            PlotModel.Series.Add(illumSeries);
            PlotModel.Series.Add(separationSeries);

            // Title annotation box
            int startYear = TrimmedResults.First().StartDay.Year;
            StringBuilder sb = new StringBuilder();
            string name = planParams.Target.Name != null ? planParams.Target.Name : "User coords";
            sb.Append($"{name}\n");
            sb.Append(startYear.ToString());
            int endYear = TrimmedResults.Last().StartDay.Year;
            if (startYear != endYear) {
                sb.Append("-").Append(endYear.ToString());
            }

            TextAnnotation textAnnotation = new TextAnnotation {
                Text = sb.ToString(),
                Background = ConvertColor(colorSchema.BackgroundColor),
                Stroke = ConvertColor(colorSchema.BorderColor),
                StrokeThickness = 2,
                TextColor = ConvertColor(colorSchema.PrimaryColor),
                TextPosition = new DataPoint(DateTimeAxis.ToDouble(TrimmedResults.First().StartDay.AddDays(15)),
                                             TimeSpanAxis.ToDouble(TimeSpan.FromMinutes(MaxImagingMinutes * .87))),
            };

            PlotModel.Annotations.Add(textAnnotation);
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
                    TrackerFormatString = "{2:MM/dd/yyyy}\n{4}",
                    YAxisKey = "Imaging Hours"
                };
            }
            else {
                return new AreaSeries {
                    Color = ConvertColor(colorSchema.NotificationErrorColor),
                    Color2 = ConvertColor(colorSchema.NotificationErrorTextColor),
                    TrackerFormatString = "{2:MM/dd/yyyy}\n{4}",
                    YAxisKey = "Imaging Hours"
                };
            }
        }

        internal PlotModel GetPlotModel() {
            return PlotModel;
        }
    }

    public class ImagingSpan {
        public int Start { get; set; }
        public int End { get; set; }
        public bool Accepted { get; set; }
    }

}