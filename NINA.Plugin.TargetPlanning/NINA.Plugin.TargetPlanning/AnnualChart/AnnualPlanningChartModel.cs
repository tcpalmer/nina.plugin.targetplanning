
using OxyPlot;
using OxyPlot.Series;
using System;

namespace TargetPlanning.NINAPlugin.AnnualChart {

    public class AnnualPlanningChartModel {

        private PlotModel _chart;
        public PlotModel Chart {
            get => _chart;
            set {
                _chart = value;
            }
        }

        public AnnualPlanningChartModel() {
            Chart = new PlotModel { Title = "Example 1" };
            Chart.Series.Add(new FunctionSeries(Math.Cos, 0, 10, 0.1, "cos(x)"));
        }

    }

}
