using System;
using System.Diagnostics;
using Microsoft.Maui.Controls;
using NAVIGEST.Shared.Services;
using NAVIGEST.Shared.Models;
using NAVIGEST.Shared.Helpers;

namespace NAVIGEST.iOS.Pages
{
    public partial class AboutPage : ContentPage
    {
        private const string INSTALLED_VERSION_KEY = "InstalledAppVersion";
        
        public AboutPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // ✅ Mostrar versão instalada
            string installedVersion = Preferences.Get(INSTALLED_VERSION_KEY, AppInfo.Current.VersionString ?? "1.0.0");
            VersionLabel.Text = $"Versão {installedVersion}";

            // ✅ Mostrar plataforma
            PlatformLabel.Text = "iOS";

            // Limpar status anterior
            UpdateStatusLabel.IsVisible = false;
            UpdateStatusLabel.Text = "";
            CheckUpdatesButton.IsEnabled = true;
        }

        private async void OnCheckUpdatesClicked(object sender, EventArgs e)
        {
            CheckUpdatesButton.IsEnabled = false;
            UpdateStatusLabel.IsVisible = true;
            UpdateStatusLabel.Text = "Verificando...";

            try
            {
                // Obter o serviço de atualizações do DI container
                var updateService = Application.Current?.Handler?.MauiContext?.Services
                    .GetService(typeof(IUpdateService)) as IUpdateService;

                if (updateService == null)
                {
                    UpdateStatusLabel.Text = "⚠️ Erro ao verificar atualizações";
                    UpdateStatusLabel.TextColor = Colors.Red;
                    return;
                }

                // Buscar informações de atualização do GitHub
                var updateInfo = await updateService.GetLatestAsync();

                if (updateInfo == null)
                {
                    UpdateStatusLabel.Text = "⚠️ Não foi possível obter informações";
                    UpdateStatusLabel.TextColor = Colors.Orange;
                    return;
                }

                string currentVersion = GetInstalledVersion();
                string latestVersion = updateInfo.Version ?? "0.0.0";

                Debug.WriteLine($"[AboutPage] Versão atual={currentVersion}, Versão servidor={latestVersion}");

                // Comparar versões
                if (VersionComparer.IsLessThan(currentVersion, latestVersion))
                {
                    // Atualização disponível
                    bool isMandatory = VersionComparer.IsLessThan(currentVersion, updateInfo.MinSupportedVersion ?? "0.0.0");
                    
                    string message = $"Nova versão disponível: {updateInfo.Version}\n\n{updateInfo.Notes ?? "Verifique as novidades!"}";
                    
                    if (isMandatory)
                    {
                        message = $"⚠️ ATUALIZAÇÃO OBRIGATÓRIA ⚠️\n\n{message}";
                    }

                    string title = isMandatory ? "Atualização Obrigatória" : "Atualização Disponível";
                    string buttonAccept = isMandatory ? "Atualizar Agora" : "Atualizar";

                    bool result = await DisplayAlert(title, message, buttonAccept, "Depois");

                    if (result && !string.IsNullOrWhiteSpace(updateInfo.DownloadUrl))
                    {
                        try
                        {
                            if (updateInfo.DownloadUrl.StartsWith("http://") || updateInfo.DownloadUrl.StartsWith("https://"))
                            {
                                await MainThread.InvokeOnMainThreadAsync(async () =>
                                {
                                    try
                                    {
                                        await Launcher.OpenAsync(updateInfo.DownloadUrl);
                                    }
                                    catch
                                    {
                                        try
                                        {
                                            await Browser.Default.OpenAsync(new Uri(updateInfo.DownloadUrl), BrowserLaunchMode.SystemPreferred);
                                        }
                                        catch { }
                                    }
                                });
                            }
                        }
                        catch { }
                    }

                    UpdateStatusLabel.Text = $"✅ Atualização disponível: {latestVersion}";
                    UpdateStatusLabel.TextColor = Colors.Green;
                }
                else
                {
                    // App já está atualizada
                    UpdateStatusLabel.Text = "✅ Aplicação atualizada";
                    UpdateStatusLabel.TextColor = Colors.Green;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AboutPage] Erro ao verificar atualizações: {ex}");
                UpdateStatusLabel.Text = $"❌ Erro: {ex.Message}";
                UpdateStatusLabel.TextColor = Colors.Red;
            }
            finally
            {
                CheckUpdatesButton.IsEnabled = true;
            }
        }

        private string GetInstalledVersion()
        {
            try
            {
                string? savedVersion = Preferences.Get(INSTALLED_VERSION_KEY, null);
                if (!string.IsNullOrEmpty(savedVersion))
                    return savedVersion;

                string appVersion = AppInfo.Current.VersionString ?? "1.0.0";
                return appVersion;
            }
            catch
            {
                return AppInfo.Current.VersionString ?? "1.0.0";
            }
        }
    }
}
