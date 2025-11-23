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
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                GlobalErro.TratarErro(ex ?? new Exception("Unknown AppDomain exception"), false);
            };

            global::Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += (s, e) =>
            {
                GlobalErro.TratarErro(e.Exception, false);
                e.Handled = true; // Try to prevent crash if possible, or at least log it first
            };
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
