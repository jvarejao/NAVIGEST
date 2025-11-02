using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Handlers;
using Android.Webkit;
#if ANDROID
using Android.Util;
#endif
using AColor = Android.Graphics.Color;

namespace NAVIGEST.Android.Pages
{
    public partial class SplashIntroPage : ContentPage
    {
        private const int GifDurationMs = 3500;
        private const int MaxSplashDurationMs = 5000;
        private bool _started;
#if ANDROID
        private const string LogTag = "SplashIntroPage";
#endif

        public SplashIntroPage()
        {
#if ANDROID
            Log.Debug(LogTag, "Ctor invoked");
#endif
            WebViewHandler.Mapper.AppendToMapping("FixLocalFiles", (handler, view) =>
            {
                var wv = handler.PlatformView;
                if (wv is null) return;

                var s = wv.Settings;
                if (s is not null)
                {
                    s.AllowFileAccess = true;
                    s.AllowContentAccess = true;
                    s.AllowFileAccessFromFileURLs = true;
                    s.AllowUniversalAccessFromFileURLs = true;
                }
                wv.SetBackgroundColor(AColor.Black);
            });

            InitializeComponent();

            try
            {
                FallbackImage.Source = ImageSource.FromFile("startup_fallback.png");
            }
            catch (Exception ex)
            {
#if ANDROID
                Log.Warn(LogTag, $"Fallback image error: {ex.Message}");
#endif
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
#if ANDROID
            Log.Debug(LogTag, "OnAppearing fired");
#endif
            if (_started) return;
            _started = true;

            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    FallbackImage.IsVisible = true;
                    FallbackImage.Opacity = 1;
#if ANDROID
                    Log.Debug(LogTag, "Fallback image now visible");
#endif
                });

#if ANDROID
                Log.Debug(LogTag, "Calling TryShowGifAsync");
#endif
                bool ok = await TryShowGifAsync();
#if ANDROID
                Log.Debug(LogTag, $"TryShowGifAsync completed. Success={ok}");
#endif

                await Task.Delay(GifDurationMs);

                try { await this.FadeTo(0, 500, Easing.CubicOut); } catch { }

#if ANDROID
                Log.Debug(LogTag, "Navigating to 'WelcomePage'");
#endif
                await Shell.Current.GoToAsync("WelcomePage");
            }
            catch (Exception ex)
            {
#if ANDROID
                Log.Error(LogTag, $"Error in OnAppearing: {ex}");
#endif
                await Shell.Current.GoToAsync("WelcomePage");
            }
        }

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
#if ANDROID
            Log.Info(LogTag, $"OnHandlerChanged. Handler={(Handler?.GetType().Name ?? "null")}");
#endif
        }

        protected override void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);
#if ANDROID
            Log.Info(LogTag, "OnNavigatedTo fired");
#endif
        }

        private async Task<bool> TryShowGifAsync()
        {
            try
            {
#if ANDROID
                Log.Debug(LogTag, "TryShowGifAsync started");
#endif
                // Try to load animated GIF - if fails, skip (fallback image will show)
                try
                {
                    Stream? stream = null;
                    
                    // Tenta vários caminhos possíveis
                    var pathsToTry = new[] 
                    { 
                        "startup.gif",
                        "Resources/Raw/startup.gif",
                        "intro_720_15fps_slow.gif",
                        "Resources/Raw/intro_720_15fps_slow.gif"
                    };
                    
                    foreach (var path in pathsToTry)
                    {
                        try
                        {
                            stream = await FileSystem.OpenAppPackageFileAsync(path);
                            if (stream != null)
                            {
#if ANDROID
                                Log.Debug(LogTag, $"Successfully loaded GIF from: {path}");
#endif
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
#if ANDROID
                            Log.Debug(LogTag, $"Path '{path}' not found: {ex.Message}");
#endif
                        }
                    }

                    if (stream != null)
                    {
                        using (stream)
                        {
                            using var ms = new MemoryStream();
                            await stream.CopyToAsync(ms);
                            var bytes = ms.ToArray();
#if ANDROID
                            Log.Debug(LogTag, $"GIF bytes read: {bytes.Length}");
#endif

                            string base64 = Convert.ToBase64String(bytes);

                            var html = $@"
<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'/>
<meta name='viewport' content='width=device-width,initial-scale=1'>
<style>
html,body{{margin:0;padding:0;height:100%;background:#000;overflow:hidden;}}
img{{max-width:100vw;max-height:100vh;object-fit:contain;display:block;margin:auto;}}
</style>
</head>
<body>
<img src='data:image/gif;base64,{base64}' alt='intro'/>
</body>
</html>";

                            var htmlSource = new HtmlWebViewSource { Html = html };

                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                GifView.BackgroundColor = Colors.Black;
                                GifView.Source = htmlSource;
#if ANDROID
                                Log.Debug(LogTag, "HtmlWebViewSource assigned");
#endif
                            });

                            await Task.Delay(300);
                            await GifView.FadeTo(1, 500);
#if ANDROID
                            Log.Debug(LogTag, "GifView visible");
#endif

                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                FallbackImage.IsVisible = false;
#if ANDROID
                                Log.Debug(LogTag, "Fallback hidden");
#endif
                            });

                            return true;
                        }
                    }
                    else
                    {
#if ANDROID
                        Log.Debug(LogTag, "All GIF paths returned null, using fallback image");
#endif
                    }
                }
                catch (Exception ex)
                {
#if ANDROID
                    Log.Warn(LogTag, $"GIF loading failed, continuing with fallback: {ex.Message}");
#endif
                    // Continue without GIF - fallback image remains visible
                }

                return false; // Fallback image remains visible
            }
            catch (Exception ex)
            {
#if ANDROID
                Log.Error(LogTag, $"TryShowGifAsync exception: {ex}");
#endif
                return false;
            }
        }

        private async Task ShowHtmlAndFadeInAsync(string html)
        {
            var tcs = new TaskCompletionSource();
            void OnNavigated(object? s, WebNavigatedEventArgs e)
            {
                GifView.Navigated -= OnNavigated;
                tcs.TrySetResult();
            }

            GifView.Navigated += OnNavigated;
            GifView.Opacity = 0;
            GifView.Source = new HtmlWebViewSource { Html = html };

            await Task.WhenAny(tcs.Task, Task.Delay(3000));
            await GifView.FadeTo(1, 200);
        }

        private async Task ShowUrlAndFadeInAsync(string url)
        {
            var tcs = new TaskCompletionSource();
            void OnNavigated(object? s, WebNavigatedEventArgs e)
            {
                GifView.Navigated -= OnNavigated;
                tcs.TrySetResult();
            }

            GifView.Navigated += OnNavigated;
            GifView.Opacity = 0;
            GifView.Source = new UrlWebViewSource { Url = url };

            await Task.WhenAny(tcs.Task, Task.Delay(3000));
            await GifView.FadeTo(1, 200);
        }
    }
}
