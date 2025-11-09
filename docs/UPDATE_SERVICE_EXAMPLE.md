// ============================================================================
// EXEMPLO: MainPage.xaml.cs com Verificação de Atualizações
// ============================================================================
// 
// Este exemplo mostra como integrar a verificação de atualizações numa página.
// 
// Fluxo:
// 1. OnAppearing() é chamado quando a página aparece
// 2. CheckForUpdatesAsync() obtém informações de atualização do servidor
// 3. Se houver atualização obrigatória → mostra alert + abre URL
// 4. Se houver atualização opcional → mostra alert + opção de ignorar
// 5. Todos os erros são tratados com try...catch TratarErro()
// 
// ⚠️ IMPORTANTE: Este ficheiro é um EXEMPLO. Ajusta conforme necessário:
//    - Muda MainPage para a tua página de entrada (pode ser SplashIntroPage, WelcomePage, etc)
//    - Muda o ViewModel se usas (ViewModelBase, etc)
//    - Adapta os namespaces (NAVIGEST.Android → NAVIGEST.Shared, etc)
// ============================================================================

using NAVIGEST.Shared.Services;
using NAVIGEST.Shared.Helpers;
using NAVIGEST.Shared.Models;

#if ANDROID
using Android.Util;
#endif

namespace NAVIGEST.Android.Pages
{
    /// <summary>
    /// Exemplo de página com verificação de atualizações.
    /// 
    /// Funciona em todas as plataformas (Android, iOS, macOS, Windows).
    /// Cada plataforma abre o link de forma apropriada via Launcher.OpenAsync.
    /// </summary>
    public partial class MainPage : ContentPage
    {
        private readonly IUpdateService _updateService;

        public MainPage()
        {
            InitializeComponent();
            _updateService = ServiceHelper.GetService<IUpdateService>();
        }

        /// <summary>
        /// Chamado quando a página aparece.
        /// Executa verificação de atualizações em background.
        /// </summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Executa verificação em background (não bloqueia UI)
            _ = CheckForUpdatesAsync();
        }

        /// <summary>
        /// Verificação de atualizações com fluxo completo:
        /// 1. Obter versão atual
        /// 2. Obter info remota
        /// 3. Comparar versões
        /// 4. Mostrar popup apropriado (obrigatória ou opcional)
        /// 5. Se aceitar, abrir URL de download
        /// </summary>
        private async Task CheckForUpdatesAsync()
        {
            try
            {
#if ANDROID
                Log.Info("UpdateCheck", "Iniciando verificação de atualizações");
#endif

                // Obter versão atual
                var currentVersion = AppInfo.Current.VersionString;
#if ANDROID
                Log.Info("UpdateCheck", $"Versão atual: {currentVersion}");
#endif

                // Obter informações de atualização do servidor
                var updateInfo = await _updateService.GetLatestAsync();

                // Se não conseguir obter info (erro de rede, servidor down, etc), sai silenciosamente
                if (updateInfo == null)
                {
#if ANDROID
                    Log.Info("UpdateCheck", "Não foi possível obter informações de atualização");
#endif
                    return;
                }

#if ANDROID
                Log.Info("UpdateCheck", $"Versão no servidor: {updateInfo.Version}");
                Log.Info("UpdateCheck", $"Versão mínima suportada: {updateInfo.MinSupportedVersion}");
#endif

                // ============================================================
                // LÓGICA 1: Verificar se é atualização OBRIGATÓRIA
                // (versão atual < minSupportedVersion)
                // ============================================================
                if (VersionComparer.IsLessThan(currentVersion, updateInfo.MinSupportedVersion))
                {
#if ANDROID
                    Log.Info("UpdateCheck", "Atualização OBRIGATÓRIA detectada");
#endif

                    // Mostrar popup obrigatório com 1 botão
                    var root = GetRootPage();
                    if (root is not null)
                    {
                        var result = await root.DisplayAlert(
                            title: "Atualização Obrigatória",
                            message: $"Uma atualização importante está disponível.\n\nVersão atual: {currentVersion}\nVersão mínima: {updateInfo.MinSupportedVersion}\n\n{updateInfo.Notes}",
                            accept: "Atualizar",
                            cancel: null);

                        // Se clicou em "Atualizar" (sempre retorna true quando é 1 botão)
                        if (result || result == true)
                        {
                            await OpenUpdateLinkAsync(updateInfo.DownloadUrl);
                        }
                    }
                    return;
                }

                // ============================================================
                // LÓGICA 2: Verificar se é atualização OPCIONAL
                // (versão atual < versão no servidor, mas >= minSupportedVersion)
                // ============================================================
                if (VersionComparer.IsLessThan(currentVersion, updateInfo.Version))
                {
#if ANDROID
                    Log.Info("UpdateCheck", "Atualização OPCIONAL disponível");
#endif

                    // Mostrar popup com 2 botões
                    var root = GetRootPage();
                    if (root is not null)
                    {
                        var userAccepted = await root.DisplayAlert(
                            title: "Nova Versão Disponível",
                            message: $"Uma nova versão está disponível!\n\nVersão atual: {currentVersion}\nNova versão: {updateInfo.Version}\n\n{updateInfo.Notes}",
                            accept: "Atualizar",
                            cancel: "Depois");

                        // Se clicou em "Atualizar"
                        if (userAccepted)
                        {
#if ANDROID
                            Log.Info("UpdateCheck", "Utilizador aceitou atualização opcional");
#endif
                            await OpenUpdateLinkAsync(updateInfo.DownloadUrl);
                        }
                        else
                        {
#if ANDROID
                            Log.Info("UpdateCheck", "Utilizador recusou atualização opcional");
#endif
                        }
                    }
                    return;
                }

#if ANDROID
                Log.Info("UpdateCheck", "App está atualizado");
#endif

                // Versão atual está atualizada
            }
            catch (Exception ex)
            {
                // Sempre usar TratarErro para logging consistente
                GlobalErro.TratarErro(ex, mostrarAlerta: false);
            }
        }

        /// <summary>
        /// Abre o link de atualização.
        /// 
        /// Usa Launcher.OpenAsync que funciona em todas as plataformas:
        /// - Android: Abre APK direto ou Google Play Store
        /// - iOS: Abre App Store via URL scheme
        /// - macOS: Abre App Store ou link genérico
        /// - Windows: Abre browser com link de download
        /// 
        /// Nota: Cada plataforma tem forma diferente de fazer:
        /// - Google Play: https://play.google.com/store/apps/details?id=com.tuaempresa.navigest
        /// - App Store: https://apps.apple.com/app/navigest/idXXXXXXXXXX
        /// - Link direto: https://seu-servidor.com/download/app.apk
        /// </summary>
        private async Task OpenUpdateLinkAsync(string downloadUrl)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(downloadUrl))
                {
#if ANDROID
                    Log.Warn("UpdateCheck", "DownloadUrl vazio");
#endif
                    return;
                }

#if ANDROID
                Log.Info("UpdateCheck", $"Abrindo URL: {downloadUrl}");
#endif

                // Launcher.OpenAsync funciona em todas as plataformas
                // Abre com o handler padrão (browser, store app, etc)
                await Launcher.Default.OpenAsync(new Uri(downloadUrl));
            }
            catch (Exception ex)
            {
                GlobalErro.TratarErro(ex);
            }
        }

        /// <summary>
        /// Obtém a página raiz da hierarquia de navegação.
        /// Compatível com NavigationPage, FlyoutPage, Shell.
        /// </summary>
        private static Page? GetRootPage()
        {
            if (Application.Current?.MainPage is NavigationPage navPage)
                return navPage.RootPage;

            if (Application.Current?.MainPage is FlyoutPage flyoutPage)
                return flyoutPage.Detail;

            return Application.Current?.MainPage;
        }
    }
}

// ============================================================================
// INTEGRAÇÃO COM PÁGINA EXISTENTE
// ============================================================================
// 
// Se já tens MainPage.xaml.cs, apenas ADICIONA ao OnAppearing():
//
// protected override async void OnAppearing()
// {
//     base.OnAppearing();
//     
//     // Teu código existente aqui...
//     
//     // ✅ Adiciona isto:
//     _ = CheckForUpdatesAsync();
// }
//
// E COPIA os métodos CheckForUpdatesAsync() e OpenUpdateLinkAsync().
//
// ============================================================================
// FICHEIRO JSON NO GITHUB
// ============================================================================
// 
// Cria um ficheiro no teu repositório GitHub:
// Caminho recomendado: /updates/version.json
// 
// Conteúdo exemplo:
// {
//   "version": "1.0.5",
//   "minSupportedVersion": "1.0.0",
//   "downloadUrl": "https://play.google.com/store/apps/details?id=com.tuaempresa.navigest",
//   "notes": "Correções de bugs e melhorias de performance."
// }
//
// URL Raw do GitHub (para AddHttpClient):
// https://raw.githubusercontent.com/jvarejao/NAVIGEST/main/updates/version.json
//
// Substituir em UpdateService.cs:
// private const string GitHubJsonUrl = "https://raw.githubusercontent.com/jvarejao/NAVIGEST/main/updates/version.json";
//
// ============================================================================
