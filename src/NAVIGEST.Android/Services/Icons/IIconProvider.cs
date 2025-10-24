using Microsoft.Maui.Controls;

namespace NAVIGEST.Android.Services.Icons
{
    public interface IIconProvider
    {
        ImageSource Get(string logicalName, double size = 24);
    }
}

