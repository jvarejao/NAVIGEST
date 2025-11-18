using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace NAVIGEST.Android.Converters;

public sealed class BoolToFiltroLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isOpen)
        {
            return isOpen ? "\uD83D\uDD3C Filtros Avan\u00E7ados" : "\uD83D\uDD3D Filtros Avan\u00E7ados";
        }

        return "Filtros Avan\u00E7ados";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
