using System.Globalization;
using Microsoft.Maui.Controls;

namespace NAVIGEST.Android.Converters;

/// <summary>
/// Conversor que transforma uma string vazia ou null em false (visibilidade)
/// Útil para mostrar/ocultar elementos baseado se uma string tem conteúdo
/// </summary>
public class StringNullOrEmptyToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            return !string.IsNullOrEmpty(str);
        }
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
