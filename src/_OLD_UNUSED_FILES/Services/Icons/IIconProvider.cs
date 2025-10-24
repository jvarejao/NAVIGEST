using Microsoft.Maui.Controls;

namespace AppLoginMaui.Services.Icons
{
    public interface IIconProvider
    {
        ImageSource Get(string logicalName, double size = 24);
    }
}

