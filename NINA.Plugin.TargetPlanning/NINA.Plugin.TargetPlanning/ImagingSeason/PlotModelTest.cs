using NINA.Core.Utility;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace TargetPlanning.NINAPlugin.ImagingSeason {

    public class PlotModelTest : PlotModel {

        public PlotModelTest() : base() {
            Title = "PlotModel Test";
            PlotAreaBorderColor = OxyColors.Transparent;
            Axes.Add(new LinearAxis { Position = AxisPosition.Bottom });
            Axes.Add(new LinearAxis { Position = AxisPosition.Left });

            LineSeries ls = new LineSeries();
            ls.Title = "My Line Series";
            ls.MarkerType = MarkerType.Circle;
            ls.Points.Add(new DataPoint(0, 0));
            ls.Points.Add(new DataPoint(10, 18));
            ls.Points.Add(new DataPoint(20, 12));
            ls.Points.Add(new DataPoint(30, 8));
            ls.Points.Add(new DataPoint(40, 15));

            Series.Add(ls);

            // this.Series.Add(new FunctionSeries(Math.Cos, 0, 10, 0.1, "cos(x)"));
            Logger.Debug("NEW PlotModelTest");
        }
    }

}
