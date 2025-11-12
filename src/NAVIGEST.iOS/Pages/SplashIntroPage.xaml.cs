using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;

namespace NAVIGEST.iOS.Pages
{
    public partial class SplashIntroPage : ContentPage
    {
        // Ajusta para a dura��o real do teu GIF (ms). Se o GIF for maior, aumenta este valor.
        private const int GifDurationMs = 3500;

        private bool _started;
        private const string INSTALLED_VERSION_KEY = "InstalledAppVersion";

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
                // ✅ Verificar se a app foi atualizada (versão no manifest diferente da guardada)
                string manifestVersion = AppInfo.Current.VersionString ?? "1.0.0";
                string savedVersion = Preferences.Get(INSTALLED_VERSION_KEY, null) ?? manifestVersion;
                
                if (manifestVersion != savedVersion)
                {
                    System.Diagnostics.Debug.WriteLine($"[SplashIntroPage] App foi atualizada: {savedVersion} → {manifestVersion}. Guardando nova versão.");
                    SaveInstalledVersion(manifestVersion);
                }

                // Ler o GIF do pacote - tentar múltiplos caminhos
                byte[]? bytes = null;
                var pathsToTry = new[] 
                { 
                    "Resources/Images/startup.gif",  // Path padrão do MAUI
                    "startup.gif"                     // Fallback
                };
                
                foreach (var path in pathsToTry)
                {
                    try
                    {
                        using var stream = await FileSystem.OpenAppPackageFileAsync(path);
                        if (stream != null)
                        {
                            using var ms = new MemoryStream();
                            await stream.CopyToAsync(ms);
                            bytes = ms.ToArray();
                            System.Diagnostics.Debug.WriteLine($"[SplashIntroPage] ✅ GIF carregado de: {path}");
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SplashIntroPage] Tentativa '{path}' falhou: {ex.Message}");
                    }
                }
                
                if (bytes == null)
                {
                    // Fallback imediato se o ficheiro não existir - não bloqueia o arranque
                    System.Diagnostics.Debug.WriteLine("[SplashIntroPage] ❌ GIF não encontrado em nenhum caminho");
                    await Shell.Current.GoToAsync("//WelcomePage");
                    return;
                }

#if IOS
                // C�digo iOS espec�fico (exemplo: anima��es, navega��o, layouts)
                try
                {
                    // Plataforma iOS: evitar criar data URI grande em mem�ria/CPU
                    // Mostrar imagem fallback nativa imediatamente enquanto o WebView inicializa
                    FallbackImage.IsVisible = true;

                    // Escrever para cache e usar file:// URL no WebView
                    var cachePath = Path.Combine(FileSystem.CacheDirectory, "startup.gif");
                    await File.WriteAllBytesAsync(cachePath, bytes);

                    var fileUrl = new Uri(cachePath).AbsoluteUri; // usa file://
                    await ShowUrlAndFadeInAsync(fileUrl);

                    // Esconde fallback depois de WebView vis�vel
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
                // Outras plataformas: mant�m abordagem com base64 injetada no HTML
                string base64 = string.Empty;
                try
                {
                    base64 = await Task.Run(() => Convert.ToBase64String(bytes));
                }
                catch
                {
                    // Se a convers�o falhar, segue para WelcomePage
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
  /* Mant�m o GIF vis�vel por completo sem cortar */
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

                // Espera a dura��o definida para o GIF antes de navegar
                await Task.Delay(GifDurationMs);

                // Vai para WelcomePage (como pediste)
                await Shell.Current.GoToAsync("//WelcomePage");
#endif
            }
            catch
            {
                // Se algo falhar, n�o bloqueia o arranque
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

                // Aguarda o primeiro frame; se demorar, segue ap�s 4s para n�o ficar preso
                await Task.WhenAny(tcs.Task, Task.Delay(3000));

                // Suaviza a entrada para evitar "flash" inicial
                await GifView.FadeTo(1, 180);
            }
            catch
            {
                // Ignorar � n�o deve bloquear o arranque
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

                await Task.WhenAny(tcs.Task, Task.Delay(3000));

                await GifView.FadeTo(1, 180);
            }
            catch
            {
                // ignore
            }
        }

        /// <summary>
        /// Obtém a versão instalada guardada em Preferences
        /// Se não existir, usa AppInfo.Current.VersionString e guarda
        /// </summary>
        private string GetInstalledVersion()
        {
            try
            {
                // Tentar ler versão guardada
                string? savedVersion = Preferences.Get(INSTALLED_VERSION_KEY, null);
                
                if (!string.IsNullOrEmpty(savedVersion))
                {
                    System.Diagnostics.Debug.WriteLine($"[SplashIntroPage] GetInstalledVersion: Versão guardada = {savedVersion}");
                    return savedVersion;
                }

                // Se não existir guardada, usar versão do app manifest
                string appVersion = AppInfo.Current.VersionString ?? "1.0.0";
                System.Diagnostics.Debug.WriteLine($"[SplashIntroPage] GetInstalledVersion: Primeira vez, usando versão app = {appVersion}");
                SaveInstalledVersion(appVersion);
                return appVersion;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SplashIntroPage] GetInstalledVersion: Erro = {ex.Message}");
                return AppInfo.Current.VersionString ?? "1.0.0";
            }
        }

        /// <summary>
        /// Guarda a versão instalada em Preferences
        /// </summary>
        private void SaveInstalledVersion(string version)
        {
            try
            {
                Preferences.Set(INSTALLED_VERSION_KEY, version);
                System.Diagnostics.Debug.WriteLine($"[SplashIntroPage] SaveInstalledVersion: Versão guardada = {version}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SplashIntroPage] SaveInstalledVersion: Erro = {ex.Message}");
            }
        }
    }
}
