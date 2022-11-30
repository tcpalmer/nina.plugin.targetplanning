using System.Windows;
using System.Windows.Controls;

namespace TargetPlanning.NINAPlugin.ImagingSeason {

    /// <summary>
    /// Interaction logic for ImagingSeasonChart.xaml
    /// </summary>
    public partial class ImagingSeasonChart : UserControl {

        public ImagingSeasonChart() {
            InitializeComponent();
        }

        public static DependencyProperty ImagingSeasonChartProperty = DependencyProperty.Register("ImagingSeasonChartModel", typeof(ImagingSeasonChartModel), typeof(ImagingSeasonChart));

        public ImagingSeasonChartModel ImagingSeasonChartModel {
            get => (ImagingSeasonChartModel)GetValue(ImagingSeasonChartProperty);
            set {
                SetValue(ImagingSeasonChartProperty, value);
            }
        }
    }

}
