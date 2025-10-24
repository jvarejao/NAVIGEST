using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace AppLoginMaui.Converters
{
    // Usa parâmetro no formato: lt:900, le:600, gt:1200, ge:800, eq:1024
    public class DoubleComparisonConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not double d || parameter is not string param || string.IsNullOrWhiteSpace(param))
                return false;

            var parts = param.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
                return false;

            var op = parts[0].ToLowerInvariant();
            if (!double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var target))
                return false;

            return op switch
            {
                "lt" => d < target,
                "le" => d <= target,
                "gt" => d > target,
                "ge" => d >= target,
                "eq" => Math.Abs(d - target) < 0.0001,
                _ => false
            };
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
