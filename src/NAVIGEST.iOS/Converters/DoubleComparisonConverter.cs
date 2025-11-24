using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace NAVIGEST.iOS.Converters
{
    public class DoubleComparisonConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is string param && param.StartsWith("gt:"))
            {
                if (double.TryParse(param.Substring(3), NumberStyles.Any, CultureInfo.InvariantCulture, out double threshold))
                {
                    if (value is double d) return d > threshold;
                    if (value is float f) return f > threshold;
                    if (value is int i) return i > threshold;
                    if (value is decimal dec) return dec > (decimal)threshold;
                }
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
