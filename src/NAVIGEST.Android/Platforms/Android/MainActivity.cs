using Android.App;
using Android.Content.PM;
using Android.OS;

namespace NAVIGEST.Android
{
    [Activity(
        Theme = "@style/Maui.SplashTheme",
        MainLauncher = true,
        LaunchMode = LaunchMode.SingleTop,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
            ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density |
            ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden)]
    public class MainActivity : MauiAppCompatActivity
    {
    }
}
