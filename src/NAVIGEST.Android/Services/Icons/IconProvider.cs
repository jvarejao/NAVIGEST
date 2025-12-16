using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace NAVIGEST.Android.Services.Icons
{
    public sealed class IconProvider : IIconProvider
    {
        public ImageSource Get(string logicalName, double size = 24)
        {
            try
            {
                return new FontImageSource
                {
                    FontFamily = "FA7Solid",
                    Glyph = logicalName,
                    Size = size,
                    Color = Colors.Black
                };
            }
            catch
            {
                return new FontImageSource
                {
                    FontFamily = "FA7Solid",
                    Glyph = "?",
                    Size = size,
                    Color = Colors.Black
                };
            }
        }
    }
}
