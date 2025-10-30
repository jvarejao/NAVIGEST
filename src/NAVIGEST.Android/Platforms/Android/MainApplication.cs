using System;
using Android.App;
using Android.Runtime;
#if ANDROID
using Android.Util;
#endif

namespace NAVIGEST.Android
{
    [Application]
    public class MainApplication : MauiApplication
    {
#if ANDROID
        private const string LogTag = "AppLifecycle";
#endif

        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
#if ANDROID
            Log.Info(LogTag, "MainApplication ctor");
#endif
        }

        protected override MauiApp CreateMauiApp()
        {
#if ANDROID
            Log.Info(LogTag, "MainApplication.CreateMauiApp invoked");
#endif
            return MauiProgram.CreateMauiApp();
        }
    }
}
