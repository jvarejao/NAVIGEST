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
        private const int GifDurationMs = 3500;       // duração do GIF
        private const int MaxSplashDurationMs = 5000; // tempo máximo antes do fallback
        private bool _started;
#if ANDROID
        private const string LogTag = "SplashIntroPage";
#endif

    public SplashIntroPage()
    {
#if ANDROID
        Log.Debug(LogTag, "Ctor invoked");
#endif
            // Handler local: ativa permissões de file:// e corrige fundo branco
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

                // Fundo preto imediato
                wv.SetBackgroundColor(AColor.Black);
            });

            InitializeComponent();

            // Carregar imagem de fallback
            try
            {
                FallbackImage.Source = ImageSource.FromFile("startup_fallback.png");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SplashIntroPage] Erro ao carregar fallback: {ex.Message}");
#if ANDROID
                Log.Warn(LogTag, $"Fallback image error: {ex.Message}");
#endif
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            System.Diagnostics.Debug.WriteLine("[SplashIntroPage] ✅ OnAppearing chamado!");
#if ANDROID
            Log.Debug(LogTag, "OnAppearing fired");
#endif
            if (_started) return;
            _started = true;

            try
            {
                System.Diagnostics.Debug.WriteLine("[SplashIntroPage] Mostrando fallback image...");
#if ANDROID
                Log.Debug(LogTag, "Displaying fallback image");
#endif
                // Mostrar imagem de fallback IMEDIATAMENTE enquanto carrega o WebView
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    FallbackImage.IsVisible = true;
                    FallbackImage.Opacity = 1;
                    System.Diagnostics.Debug.WriteLine("[SplashIntroPage] Fallback image visível!");
#if ANDROID
                    Log.Debug(LogTag, "Fallback image now visible");
#endif
                });

                // Tentar carregar o GIF no background
                System.Diagnostics.Debug.WriteLine("[SplashIntroPage] Iniciando TryShowGifAsync...");
#if ANDROID
                Log.Debug(LogTag, "Calling TryShowGifAsync");
#endif
                bool ok = await TryShowGifAsync();
#if ANDROID
                Log.Debug(LogTag, $"TryShowGifAsync completed. Success={ok}");
#endif

                // Esperar pelo tempo do GIF
                await Task.Delay(GifDurationMs);

                // Fade-out suave antes de ir para WelcomePage
                try { await this.FadeTo(0, 500, Easing.CubicOut); } catch { }

                System.Diagnostics.Debug.WriteLine("[SplashIntroPage] Navegando para welcome...");
#if ANDROID
                Log.Debug(LogTag, "Navigating to 'welcome'");
#endif
                await Shell.Current.GoToAsync("welcome");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SplashIntroPage] Erro: {ex.Message}");
#if ANDROID
                Log.Error(LogTag, $"Erro em OnAppearing: {ex}");
#endif
                await Shell.Current.GoToAsync("welcome");
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
        using var stream = await FileSystem.OpenAppPackageFileAsync("startup.gif");
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        var bytes = ms.ToArray();
#if ANDROID
    Log.Debug(LogTag, $"GIF bytes lidos: {bytes.Length}");
#endif

        // Cache o GIF em ficheiro
        string cacheDir = FileSystem.CacheDirectory;
        string gifPath = Path.Combine(cacheDir, "startup.gif");
        await File.WriteAllBytesAsync(gifPath, bytes);
#if ANDROID
    Log.Debug(LogTag, $"GIF escrito em cache: {gifPath}");
#endif

        string base64 = Convert.ToBase64String(bytes);
#if ANDROID
    Log.Debug(LogTag, "Base64 criado");
#endif

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

        // Mostrar WebView com fade in
        MainThread.BeginInvokeOnMainThread(() =>
        {
            GifView.BackgroundColor = Colors.Black;
            GifView.Source = htmlSource;
#if ANDROID
            Log.Debug(LogTag, "HtmlWebViewSource atribuído");
#endif
        });

        // Fade in do WebView (dá tempo para renderizar)
        await Task.Delay(300); // Pequeno delay para WebView inicializar
        await GifView.FadeTo(1, 500);
#if ANDROID
        Log.Debug(LogTag, "GifView visível (FadeTo concluído)");
#endif
        
        // Esconder fallback quando WebView está pronto
        MainThread.BeginInvokeOnMainThread(() =>
        {
            FallbackImage.IsVisible = false;
#if ANDROID
            Log.Debug(LogTag, "Fallback ocultado");
#endif
        });

        return true;
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"[TryShowGifAsync] Erro: {ex.Message}");
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
