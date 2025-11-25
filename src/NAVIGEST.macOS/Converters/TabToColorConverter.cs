using System.Globalization;
using Microsoft.Maui.Controls;

namespace NAVIGEST.macOS.Converters
{
    public class TabToColorConverter : IValueConverter
    {
        public Color SelectedColor { get; set; } = Color.FromArgb("#6EC0FF"); // AppAccent
        public Color UnselectedColor { get; set; } = Colors.Transparent;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string selectedTab && parameter is string tabName)
            {
                return selectedTab == tabName ? SelectedColor : UnselectedColor;
            }
            return UnselectedColor;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TabToTextColorConverter : IValueConverter
    {
        public Color SelectedColor { get; set; } = Colors.White;
        public Color UnselectedColor { get; set; } = Colors.Gray;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string selectedTab && parameter is string tabName)
            {
                return selectedTab == tabName ? SelectedColor : UnselectedColor;
            }
            return UnselectedColor;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
