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
using NAVIGEST.Shared.Services;
using NAVIGEST.Shared.Helpers;
using System.Diagnostics;

namespace NAVIGEST.Android.Pages
{
    public partial class SplashIntroPage : ContentPage
    {
        private const int GifDurationMs = 4500;
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

                // ✅ VERIFICAR ATUALIZAÇÕES enquanto mostra splash
#if ANDROID
                Log.Debug(LogTag, "Calling CheckForUpdatesAsync");
#endif
                await CheckForUpdatesAsync();

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

        protected override void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);
#if ANDROID
            Log.Info(LogTag, "OnNavigatedTo fired");
#endif
        }

        /// <summary>
        /// Verifica se há atualizações disponíveis no GitHub
        /// </summary>
        private async Task CheckForUpdatesAsync()
        {
            try
            {
#if ANDROID
                Log.Debug(LogTag, "CheckForUpdatesAsync: Iniciando verificação de atualizações");
#endif
                // Obter o serviço de atualizações do DI container
                var updateService = Application.Current?.Handler?.MauiContext?.Services
                    .GetService(typeof(IUpdateService)) as IUpdateService;

                if (updateService == null)
                {
#if ANDROID
                    Log.Warn(LogTag, "CheckForUpdatesAsync: UpdateService não está registado");
#endif
                    return;
                }

                // Buscar informações de atualização do GitHub
                var updateInfo = await updateService.GetLatestAsync();

                if (updateInfo == null)
                {
#if ANDROID
                    Log.Debug(LogTag, "CheckForUpdatesAsync: Nenhuma informação de atualização obtida");
#endif
                    return;
                }

                string currentVersion = AppInfo.Current.VersionString ?? "0.0.0";
                string latestVersion = updateInfo.Version ?? "0.0.0";

#if ANDROID
                Log.Debug(LogTag, $"CheckForUpdatesAsync: Versão atual={currentVersion}, Versão servidor={latestVersion}");
#endif

                // Comparar versões usando helper
                if (VersionComparer.IsLessThan(currentVersion, latestVersion))
                {
#if ANDROID
                    Log.Info(LogTag, $"CheckForUpdatesAsync: Atualização disponível! {currentVersion} → {latestVersion}");
#endif
                    await ShowUpdateAlertAsync(updateInfo);
                }
                else
                {
#if ANDROID
                    Log.Debug(LogTag, $"CheckForUpdatesAsync: App está atualizada");
#endif
                }
            }
            catch (Exception ex)
            {
#if ANDROID
                Log.Error(LogTag, $"CheckForUpdatesAsync: Erro ao verificar atualizações: {ex.Message}");
#endif
                Debug.WriteLine($"[SplashIntroPage] CheckForUpdatesAsync error: {ex}");
                // Continua silenciosamente - não bloqueia o arranque
            }
        }

        /// <summary>
        /// Mostra alert de atualização disponível
        /// </summary>
        private async Task ShowUpdateAlertAsync(Shared.Models.AppUpdateInfo updateInfo)
        {
            try
            {
                bool isMandatory = VersionComparer.IsLessThan(AppInfo.Current.VersionString, updateInfo.MinSupportedVersion ?? "0.0.0");

                string message = $"Nova versão disponível: {updateInfo.Version}\n\n{updateInfo.Notes ?? "Verifique as novidades!"}";
                
                if (isMandatory)
                {
                    message = $"⚠️ ATUALIZAÇÃO OBRIGATÓRIA ⚠️\n\n{message}";
                }

#if ANDROID
                Log.Info(LogTag, $"ShowUpdateAlertAsync: Mostrando alert (obrigatória={isMandatory})");
#endif

                // Mostrar alert com botões apropriados
                bool result;
                if (isMandatory)
                {
                    // Apenas botão de atualizar - sem opção de ignorar
                    result = await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        var currentPage = GetRootPage();
                        if (currentPage == null) return false;
                        
                        return await currentPage.DisplayAlert(
                            "Atualização Obrigatória",
                            message,
                            "Atualizar Agora",
                            null
                        );
                    });
                }
                else
                {
                    // Botão de atualizar e ignorar
                    result = await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        var currentPage = GetRootPage();
                        if (currentPage == null) return false;
                        
                        return await currentPage.DisplayAlert(
                            "Atualização Disponível",
                            message,
                            "Atualizar",
                            "Depois"
                        );
                    });
                }

                // Se clicou "Atualizar" ou é obrigatória
                if (result)
                {
#if ANDROID
                    Log.Info(LogTag, $"ShowUpdateAlertAsync: Abrindo URL de download: {updateInfo.DownloadUrl}");
#endif
                    try
                    {
                        await Launcher.OpenAsync(updateInfo.DownloadUrl);
                    }
                    catch (Exception ex)
                    {
#if ANDROID
                        Log.Error(LogTag, $"ShowUpdateAlertAsync: Erro ao abrir URL: {ex.Message}");
#endif
                    }
                }
            }
            catch (Exception ex)
            {
#if ANDROID
                Log.Error(LogTag, $"ShowUpdateAlertAsync: Erro ao mostrar alert: {ex.Message}");
#endif
                Debug.WriteLine($"[SplashIntroPage] ShowUpdateAlertAsync error: {ex}");
            }
        }

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
#if ANDROID
            Log.Info(LogTag, $"OnHandlerChanged. Handler={(Handler?.GetType().Name ?? "null")}");
#endif
        }

        private async Task<bool> TryShowGifAsync()
        {
            try
            {
#if ANDROID
                Log.Debug(LogTag, "TryShowGifAsync started");
#endif
                // Try to load animated GIF - startup.gif from Resources/Raw
                try
                {
                    Stream? stream = null;
                    
                    // Tenta startup.gif com os caminhos possíveis
                    var pathsToTry = new[] 
                    { 
                        "Resources/Raw/startup.gif",  // ← Caminho correto (MauiAsset LogicalName)
                        "startup.gif"                  // ← Fallback
                    };
                    
                    foreach (var path in pathsToTry)
                    {
                        try
                        {
                            stream = await FileSystem.OpenAppPackageFileAsync(path);
                            if (stream != null)
                            {
#if ANDROID
                                Log.Debug(LogTag, $"✅ GIF loaded from: {path}");
#endif
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
#if ANDROID
                            Log.Debug(LogTag, $"⚠ Path '{path}' not found: {ex.Message}");
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

        /// <summary>
        /// Obtém a página raiz (Window.Page) de forma segura
        /// </summary>
        private Page? GetRootPage()
        {
            try
            {
                if (Application.Current?.Windows == null || Application.Current.Windows.Count == 0)
                    return null;

                return Application.Current.Windows[0]?.Page ?? this;
            }
            catch
            {
                return this;
            }
        }
    }
}
