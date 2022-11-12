using OxyPlot;
using System.Windows;
using System.Windows.Controls;

namespace TargetPlanning.NINAPlugin.AnnualChart {

    /// <summary>
    /// Interaction logic for AnnualPlanningChart.xaml
    /// </summary>
    public partial class AnnualPlanningChart : UserControl {

        public AnnualPlanningChart() {
            InitializeComponent();
        }

        public static DependencyProperty AnnualPlanningChartProperty = DependencyProperty.Register("AnnualPlanningChartModel", typeof(PlotModel), typeof(AnnualPlanningChart));

        public PlotModel AnnualPlanningChartModel {
            get => (PlotModel)GetValue(AnnualPlanningChartProperty);
            set => SetValue(AnnualPlanningChartProperty, value);
        }

        public static DependencyProperty ShowMoonProperty = DependencyProperty.Register("ShowMoon", typeof(bool), typeof(AnnualPlanningChart), new PropertyMetadata(true));

        public bool ShowMoon {
            get => (bool)GetValue(ShowMoonProperty);
            set => SetValue(ShowMoonProperty, value);
        }
    }
}
