using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace NAVIGEST.macOS.Converters
{
    public class BoolToColorConverter : BindableObject, IValueConverter
    {
        public static readonly BindableProperty TrueColorProperty = BindableProperty.Create(nameof(TrueColor), typeof(Color), typeof(BoolToColorConverter), Colors.Blue);
        public Color TrueColor
        {
            get => (Color)GetValue(TrueColorProperty);
            set => SetValue(TrueColorProperty, value);
        }

        public static readonly BindableProperty FalseColorProperty = BindableProperty.Create(nameof(FalseColor), typeof(Color), typeof(BoolToColorConverter), Colors.Transparent);
        public Color FalseColor
        {
            get => (Color)GetValue(FalseColorProperty);
            set => SetValue(FalseColorProperty, value);
        }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? TrueColor : FalseColor;
            }
            return FalseColor;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
