using System;
using System.Globalization;
using System.IO;
using Microsoft.Maui.Controls;

namespace NAVIGEST.iOS.Converters
{
    public class BytesToImageSourceConverter : IValueConverter
    {
        // Converte byte[] -> ImageSource (para mostrar foto)
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is byte[] bytes && bytes.Length > 0)
                {
                    return ImageSource.FromStream(() => new MemoryStream(bytes));
                }
            }
            catch
            {
                // ignora e devolve placeholder
            }

            // fallback se não houver imagem
            return "user_placeholder.png";
        }

        // Não precisamos do caminho inverso (ImageSource -> byte[])
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}

