using System.Globalization;

namespace NAVIGEST.macOS.Converters;

public class CurrencyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal d)
        {
            // Format as Currency (pt-PT) -> "1 234,56 €"
            return d.ToString("C2", new CultureInfo("pt-PT"));
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0m;

            // Remove currency symbol and whitespace
            // Also handle non-breaking spaces if any
            string clean = s.Replace("€", "").Replace(" ", "").Trim();
            
            // Replace dot with comma to support "12.34" -> "12,34"
            string normalized = clean.Replace(".", ",");

            if (decimal.TryParse(normalized, NumberStyles.Any, new CultureInfo("pt-PT"), out decimal result))
            {
                return result;
            }
        }
        return 0m;
    }
}
