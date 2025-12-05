using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Handlers;
using Android.Webkit;
#if ANDROID
using Android.Util;
using Android.App;
using AndroidContent = Android.Content;
using AndroidApp = Android.App.Application;
#endif
using AColor = Android.Graphics.Color;
using NAVIGEST.Shared.Services;
using NAVIGEST.Shared.Helpers;
using System.Diagnostics;
using Application = Microsoft.Maui.Controls.Application;
using NAVIGEST.Shared.Resources.Strings;

namespace NAVIGEST.Android.Pages
{
    public partial class SplashIntroPage : ContentPage
    {
        private const int GifDurationMs = 4500;
        private const int MaxSplashDurationMs = 5000;
        private bool _started;
        private const string INSTALLED_VERSION_KEY = "InstalledAppVersion";
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
            VersionLabel.Text = string.Format(AppResources.Splash_Version, AppInfo.Current.VersionString);

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
                // ✅ CRITICAL FIX: Sempre sincronizar versão instalada com Preferences
                // Isto garante que após atualizar a app, a versão fica registada
                string manifestVersion = AppInfo.Current.VersionString ?? "1.0.0";
                string? savedVersion = Preferences.Get(INSTALLED_VERSION_KEY, null);
                
#if ANDROID
                Log.Info(LogTag, $"Version check: Manifest={manifestVersion}, Saved={savedVersion ?? "null"}");
#endif
                
                // Se não houver versão guardada OU versões são diferentes → sincronizar
                if (string.IsNullOrEmpty(savedVersion) || manifestVersion != savedVersion)
                {
#if ANDROID
                    Log.Info(LogTag, $"App versão alterada: {savedVersion ?? "primeira vez"} → {manifestVersion}. Atualizando Preferences.");
#endif
                    // Força atualização persistent no Preferences com múltiplas tentativas
                    try
                    {
                        Preferences.Set(INSTALLED_VERSION_KEY, manifestVersion);
#if ANDROID
                        // Em Android, tenta usar SharedPreferences diretamente se disponível
                        var context = AndroidApp.Context;
                        if (context != null)
                        {
                            var prefs = context.GetSharedPreferences("com.navigatorcode.navigest_preferences", (AndroidContent.FileCreationMode)0);
                            if (prefs != null)
                            {
                                var editor = prefs.Edit();
                                if (editor != null)
                                {
                                    editor.PutString(INSTALLED_VERSION_KEY, manifestVersion);
                                    editor.Commit(); // Síncrono - força escrita no disco
                                    Log.Info(LogTag, $"✅ SharedPreferences sync completo. Versão guardada: {manifestVersion}");
                                }
                            }
                        }
                        else
                        {
                            Log.Info(LogTag, $"✅ Preferences atualizado (sem SharedPreferences direto). Versão: {manifestVersion}");
                        }
#endif
                    }
                    catch (Exception prefEx)
                    {
#if ANDROID
                        Log.Error(LogTag, $"Erro ao atualizar Preferences: {prefEx.Message}");
#endif
                    }
                }

                // ✅ Mostrar versão no SplashScreen
                string installedVersion = GetInstalledVersion();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    VersionLabel.Text = $"{AppResources.Splash_VersionPrefix} {installedVersion}";
                });

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

                string currentVersion = GetInstalledVersion();
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
        /// Mostra alert de atualização disponível - Popup totalmente modal
        /// </summary>
        private async Task ShowUpdateAlertAsync(Shared.Models.AppUpdateInfo updateInfo)
        {
            try
            {
                bool isMandatory = VersionComparer.IsLessThan(GetInstalledVersion(), updateInfo.MinSupportedVersion ?? "0.0.0");

                string message = $"{string.Format(AppResources.Update_NewVersionAvailable, updateInfo.Version)}\n\n{updateInfo.Notes ?? AppResources.Update_CheckNotes}";
                
                if (isMandatory)
                {
                    message = $"{AppResources.Update_MandatoryHeader}\n\n{message}";
                }

#if ANDROID
                Log.Info(LogTag, $"ShowUpdateAlertAsync: Mostrando alert (obrigatória={isMandatory})");
#endif

                // Usar DisplayAlert - é modal por padrão em MAUI
                string title = isMandatory ? AppResources.Update_TitleMandatory : AppResources.Update_TitleAvailable;
                string buttonAccept = isMandatory ? AppResources.Update_ButtonUpdateNow : AppResources.Update_ButtonUpdate;
                
                bool result = false;
                
                // Executar no main thread e esperar resultado
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var page = GetRootPage();
                    if (page != null)
                    {
                        if (isMandatory)
                        {
                            // Apenas botão de atualizar - sem opção de ignorar
                            result = await page.DisplayAlert(title, message, buttonAccept, AppResources.Update_ButtonExit);
                        }
                        else
                        {
                            // Botão de atualizar e ignorar
                            result = await page.DisplayAlert(title, message, buttonAccept, AppResources.Update_ButtonLater);
                        }
                    }
                });

#if ANDROID
                Log.Info(LogTag, $"ShowUpdateAlertAsync: Resultado={result} (true=Atualizar, false=Depois/Cancelado)");
#endif

                // Se clicou "Atualizar"
                if (result && !string.IsNullOrWhiteSpace(updateInfo.DownloadUrl))
                {
#if ANDROID
                    Log.Info(LogTag, $"ShowUpdateAlertAsync: Abrindo URL: {updateInfo.DownloadUrl}");
#endif
                    
                    try
                    {
                        // Validar URL e abrir
                        if (updateInfo.DownloadUrl.StartsWith("http://") || updateInfo.DownloadUrl.StartsWith("https://"))
                        {
                            // Usar MainThread para garantir que acontece no thread correto
                            await MainThread.InvokeOnMainThreadAsync(async () =>
                            {
                                try
                                {
                                    await Launcher.OpenAsync(updateInfo.DownloadUrl);
                                }
                                catch (Exception ex)
                                {
#if ANDROID
                                    Log.Error(LogTag, $"ShowUpdateAlertAsync: Launcher.OpenAsync falhou: {ex.Message}");
#endif
                                    // Fallback para Browser
                                    try
                                    {
                                        await Browser.Default.OpenAsync(new Uri(updateInfo.DownloadUrl), BrowserLaunchMode.SystemPreferred);
                                    }
                                    catch (Exception browserEx)
                                    {
#if ANDROID
                                        Log.Error(LogTag, $"ShowUpdateAlertAsync: Browser.Default.OpenAsync também falhou: {browserEx.Message}");
#endif
                                    }
                                }
                            });
                        }
                        else
                        {
#if ANDROID
                            Log.Error(LogTag, $"ShowUpdateAlertAsync: URL não começa com http(s): {updateInfo.DownloadUrl}");
#endif
                        }
                    }
                    catch (Exception ex)
                    {
#if ANDROID
                        Log.Error(LogTag, $"ShowUpdateAlertAsync: Erro geral ao processar URL: {ex.Message}");
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
#if ANDROID
                    Log.Debug(LogTag, $"GetInstalledVersion: Versão guardada = {savedVersion}");
#endif
                    return savedVersion;
                }

                // Se não existir guardada, usar versão do app manifest
                string appVersion = AppInfo.Current.VersionString ?? "1.0.0";
#if ANDROID
                Log.Debug(LogTag, $"GetInstalledVersion: Primeira vez, usando versão app = {appVersion}");
#endif
                SaveInstalledVersion(appVersion);
                return appVersion;
            }
            catch (Exception ex)
            {
#if ANDROID
                Log.Error(LogTag, $"GetInstalledVersion: Erro = {ex.Message}");
#endif
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
#if ANDROID
                Log.Debug(LogTag, $"SaveInstalledVersion: Versão guardada = {version}");
#endif
            }
            catch (Exception ex)
            {
#if ANDROID
                Log.Error(LogTag, $"SaveInstalledVersion: Erro = {ex.Message}");
#endif
            }
        }
    }
}
