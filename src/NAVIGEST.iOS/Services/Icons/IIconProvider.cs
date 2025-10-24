using Microsoft.Maui.Controls;

namespace NAVIGEST.iOS.Services.Icons
{
    public interface IIconProvider
    {
        ImageSource Get(string logicalName, double size = 24);
    }
}

