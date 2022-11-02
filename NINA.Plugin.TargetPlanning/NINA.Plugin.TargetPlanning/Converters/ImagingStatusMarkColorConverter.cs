
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows;
using System;

namespace TargetPlanning.NINAPlugin.Converters {

    public class ImagingStatusMarkColorConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            bool status = (bool)value;
            return status ? "Green" : "Red"; // DarkSeaGreen / IndianRed
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }

}
