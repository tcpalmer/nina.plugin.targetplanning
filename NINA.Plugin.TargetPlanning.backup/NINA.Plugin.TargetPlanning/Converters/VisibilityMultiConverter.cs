using NINA.Core.Utility;
using System;
using System.Globalization;
using System.Windows.Data;

namespace TargetPlanning.NINAPlugin.Converters {

    public class VisibilityMultiConverter : IMultiValueConverter {

        /*
         * targetType: the expected Type of the returned object
         * parameter: value of the ConverterParameter attribute
         */

        object IMultiValueConverter.Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            if (values != null) {
                for (int i = 0; i < values.Length; i++) {
                    object o = values[i];
                    if (o != null) {
                        Logger.Debug($"*** VALUE {i}: {o.ToString()} {o.GetType().FullName}");
                    }
                    else {
                        Logger.Debug($"*** value {i} is null");
                    }
                }
            }
            else {
                Logger.Debug("*** values is null");
            }

            if (targetType != null)
                Logger.Debug($"*** TYPE: {targetType.FullName}");
            if (parameter != null)
                Logger.Debug($"*** PARM: {parameter.ToString()}");

            return System.Windows.Visibility.Visible;
        }

        object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
