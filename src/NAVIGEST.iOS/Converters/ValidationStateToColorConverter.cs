using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using NAVIGEST.iOS.Behaviors;

namespace NAVIGEST.iOS.Converters;

public sealed class ValidationStateToColorConverter : IValueConverter
{
    private static readonly Color FocusColor = Color.FromArgb("#2563EB");
    private static readonly Color ValidColor = Color.FromArgb("#22C55E");
    private static readonly Color InvalidColor = Color.FromArgb("#EF4444");
    private static readonly Color NeutralColor = Color.FromArgb("#D1D5DB");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ValidationState state)
        {
            return state switch
            {
                ValidationState.Focused => FocusColor,
                ValidationState.Valid => ValidColor,
                ValidationState.Invalid => InvalidColor,
                _ => NeutralColor
            };
        }

        return NeutralColor;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
