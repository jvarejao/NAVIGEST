using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;

namespace AppLoginMaui.Pages
{
    public partial class SplashIntroPage : ContentPage
    {
        // Ajusta para a duração real do teu GIF (ms). Se o GIF for maior, aumenta este valor.
        private const int GifDurationMs = 3500;

        private bool _started;

        public SplashIntroPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (_started) return;
            _started = true;

            try
            {
                // Lê o GIF do pacote
                byte[] bytes;
                try
                {
                    using var stream = await FileSystem.OpenAppPackageFileAsync("startup.gif");
                    using var ms = new MemoryStream();
                    await stream.CopyToAsync(ms);
                    bytes = ms.ToArray();
                }
                catch
                {
                    // Fallback imediato se o ficheiro não existir — não bloqueia o arranque
                    await Shell.Current.GoToAsync("//WelcomePage");
                    return;
                }

#if IOS
                // Código iOS específico (exemplo: animações, navegação, layouts)
                try
                {
                    // Plataforma iOS: evitar criar data URI grande em memória/CPU
                    // Mostrar imagem fallback nativa imediatamente enquanto o WebView inicializa
                    FallbackImage.IsVisible = true;

                    // Escrever para cache e usar file:// URL no WebView
                    var cachePath = Path.Combine(FileSystem.CacheDirectory, "startup.gif");
                    await File.WriteAllBytesAsync(cachePath, bytes);

                    var fileUrl = new Uri(cachePath).AbsoluteUri; // usa file://
                    await ShowUrlAndFadeInAsync(fileUrl);

                    // Esconde fallback depois de WebView visível
                    FallbackImage.IsVisible = false;

                    await Task.Delay(GifDurationMs);
                    await Shell.Current.GoToAsync("//WelcomePage");
                    return;
                }
                catch
                {
                    await Shell.Current.GoToAsync("//WelcomePage");
                    return;
                }
#endif

#if ANDROID || WINDOWS
                // Outras plataformas: mantêm abordagem com base64 injetada no HTML
                string base64 = string.Empty;
                try
                {
                    base64 = await Task.Run(() => Convert.ToBase64String(bytes));
                }
                catch
                {
                    // Se a conversão falhar, segue para WelcomePage
                    await Shell.Current.GoToAsync("//WelcomePage");
                    return;
                }

                var html = $@"
<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'/>
<meta name='viewport' content='width=device-width,initial-scale=1,maximum-scale=1,user-scalable=no'>
<style>
  html,body{{margin:0;padding:0;height:100%;width:100%;background:#000;overflow:hidden;}}
  .wrap{{position:fixed;inset:0;display:flex;align-items:center;justify-content:center;background:#000;}}
  /* Mantém o GIF visível por completo sem cortar */
  img{{max-width:100vw;max-height:100vh;object-fit:contain;display:block;}}
</style>
</head>
<body>
  <div class='wrap'>
    <img src='data:image/gif;base64,{base64}' alt='intro'/>
  </div>
</body>
</html>";

                await ShowHtmlAndFadeInAsync(html);

                // Espera a duração definida para o GIF antes de navegar
                await Task.Delay(GifDurationMs);

                // Vai para WelcomePage (como pediste)
                await Shell.Current.GoToAsync("//WelcomePage");
#endif
            }
            catch
            {
                // Se algo falhar, não bloqueia o arranque
                await Shell.Current.GoToAsync("//WelcomePage");
            }
        }

        private async Task ShowHtmlAndFadeInAsync(string html)
        {
            try
            {
                var tcs = new TaskCompletionSource();

                void OnNavigated(object? s, WebNavigatedEventArgs e)
                {
                    GifView.Navigated -= OnNavigated;
                    tcs.TrySetResult();
                }

                GifView.Navigated += OnNavigated;

                // Set source on UI thread
                GifView.Opacity = 0;
                GifView.Source = new HtmlWebViewSource { Html = html };

                // Aguarda o primeiro frame; se demorar, segue após 4s para não ficar preso
                await Task.WhenAny(tcs.Task, Task.Delay(4000));

                // Suaviza a entrada para evitar "flash" inicial
                await GifView.FadeTo(1, 180);
            }
            catch
            {
                // Ignorar — não deve bloquear o arranque
            }
        }

        // load a file:// URL into the WebView and fade in (used only on iOS)
        private async Task ShowUrlAndFadeInAsync(string url)
        {
            try
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

                await Task.WhenAny(tcs.Task, Task.Delay(4000));

                await GifView.FadeTo(1, 180);
            }
            catch
            {
                // ignore
            }
        }
    }
}
