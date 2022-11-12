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

        public static DependencyProperty AnnualPlanningChartProperty = DependencyProperty.Register("AnnualPlanningChartModel", typeof(AnnualPlanningChartModel), typeof(AnnualPlanningChart));

        public AnnualPlanningChartModel AnnualPlanningChartModel {
            get => (AnnualPlanningChartModel)GetValue(AnnualPlanningChartProperty);
            set => SetValue(AnnualPlanningChartProperty, value);
        }

    }
}
