using Foundation;
using UIKit;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace NAVIGEST.iOS
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp() 
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                LogException(e.ExceptionObject as Exception);
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                LogException(e.Exception);
            };

            return MauiProgram.CreateMauiApp();
        }

        private void LogException(Exception? ex)
        {
            if (ex == null) return;
            try
            {
                var path = Path.Combine(FileSystem.AppDataDirectory, "logs");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                var file = Path.Combine(path, "app.log");
                var msg = $"[{DateTime.Now}] CRASH: {ex}\n\n";
                File.AppendAllText(file, msg);
            }
            catch { /* ignore logging errors */ }
        }
    }
}
