using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace NAVIGEST.iOS.Converters;

public class HorasExtraConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is float f)
            return f > 0;
        if (value is double d)
            return d > 0;
        if (value is decimal dec)
            return dec > 0;
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
