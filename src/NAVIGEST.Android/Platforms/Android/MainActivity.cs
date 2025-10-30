using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Util;

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
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            Log.Info("AppLifecycle", "MainActivity.OnCreate start");
            base.OnCreate(savedInstanceState);
            Log.Info("AppLifecycle", "MainActivity.OnCreate end");
        }
    }
}
