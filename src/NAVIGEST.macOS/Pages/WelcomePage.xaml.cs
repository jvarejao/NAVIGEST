using System.Collections.ObjectModel;
using System.IO;
using NAVIGEST.macOS.Services;
using Microsoft.Maui.ApplicationModel; // MainThread
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls;        // SolidColorBrush
using Microsoft.Maui.Storage;         // Preferences
using CommunityToolkit.Maui.Views;

namespace NAVIGEST.macOS.Pages;

public partial class WelcomePage : ContentPage
{
    private bool _isBusy;
    private bool _choosingCompany;                 // <— NOVO: evita duplo-disparo
    private CancellationTokenSource? _loadingCts;

    // chaves de memória da empresa escolhida
    private const string KeyCompanyCode = "company.code";
    private const string KeyCompanyName = "company.name";

    private bool _programmaticSelect; // evita disparar navegação ao pré-selecionar

    // Language
    public string CurrentLanguageFlag => NAVIGEST.macOS.Helpers.LanguageHelper.GetCurrentLanguageInfo().Flag;
    public string CurrentLanguageCode => NAVIGEST.macOS.Helpers.LanguageHelper.GetCurrentLanguageInfo().Code;

    public Command ChangeLanguageCommand => new Command(async () => 
    {
        var popup = new NAVIGEST.macOS.Popups.LanguageSelectionPopup();
        var result = await this.ShowPopupAsync(popup);
        if (result is string code)
        {
            await NAVIGEST.macOS.Helpers.LanguageHelper.ChangeLanguageAndRestart(code);
        }
    });

    // modelo para o picker (logo pronto)
    private sealed class CompanyDisplay
    {
        public string CodEmp { get; init; } = "";
        public string Empresa { get; init; } = "";
        public ImageSource? LogoSrc { get; init; }
    }

    private readonly ObservableCollection<CompanyDisplay> _companies = new();

    public WelcomePage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_isBusy) return;
        _isBusy = true;

        try
        {
            // 1) Teste de ligação
            LoadingLabel.Text = NAVIGEST.Shared.Resources.Strings.AppResources.Welcome_Connecting;
            await ShowLoadingAsync(true);

            bool ok;
            try
            {
                ok = await DatabaseService.TestConnectionAsync();
            }
            catch (Exception ex)
            {
                await ShowLoadingAsync(false);
                await ShowToastAsync(string.Format(NAVIGEST.Shared.Resources.Strings.AppResources.Welcome_ConnectionFailed, ex.Message), success: false, ms: 1800);
                await NavigateToConfigAsync();
                return;
            }

            await ShowLoadingAsync(false);

            if (!ok)
            {
                await ShowToastAsync(NAVIGEST.Shared.Resources.Strings.AppResources.Welcome_ConnectionError, success: false, ms: 1400);
                await NavigateToConfigAsync();
                return;
            }

            // 2) Ligou — pedir escolha da empresa
            await LoadCompaniesAsync();
        }
        finally
        {
            _isBusy = false;
        }
    }

    // ----------------- Carregar lista de empresas ativas -----------------
    private async Task LoadCompaniesAsync()
    {
        var items = await DatabaseService.GetActiveCompaniesAsync();

        _companies.Clear();
        foreach (var c in items)
        {
            ImageSource? src = (c.Logotipo is { Length: > 0 })
                ? ImageSource.FromStream(() => new MemoryStream(c.Logotipo))
                : "yahcores.png";

            _companies.Add(new CompanyDisplay
            {
                CodEmp = c.CodEmp,
                Empresa = c.Empresa ?? c.CodEmp,
                LogoSrc = src
            });
        }

        if (_companies.Count == 0)
        {
            await ShowToastAsync("Nenhuma empresa ativa encontrada.", success: false, ms: 2000);
            await NavigateToConfigAsync();
            return;
        }

        // Mostra o painel de seleção (mesmo que haja 1)
        CompanyList.ItemsSource = _companies;
        CompanyPickerPanel.IsVisible = true;
        CompanyPickerPanel.Opacity = 0;
        await CompanyPickerPanel.FadeTo(1, 150, Easing.CubicOut);

        // Pré-seleciona (visual) a última empresa escolhida, sem navegar
        var lastCod = Preferences.Default.Get<string?>(KeyCompanyCode, null);
        if (!string.IsNullOrWhiteSpace(lastCod))
        {
            var item = _companies.FirstOrDefault(x => x.CodEmp == lastCod);
            if (item != null)
            {
                _programmaticSelect = true;
                CompanyList.SelectedItem = item; // realce visual
                _programmaticSelect = false;
            }
        }

        // Esconde conteúdo principal até escolher
        LogoImage.Opacity = 0; LogoImage.Scale = 0.8;
        TituloLabel.Opacity = 0;
        DescricaoLabel.Opacity = 0;
    }

    // ----------------- Clique numa empresa (SELECTION CHANGED) -----------------
    private async void OnCompanySelected(object? sender, SelectionChangedEventArgs e)
    {
        if (_programmaticSelect) return;  // ignore preselect
        var chosen = e.CurrentSelection?.FirstOrDefault() as CompanyDisplay;
        if (chosen == null) return;

        await StartHandleCompanyAsync(chosen, sender as CollectionView);
    }

    // ----------------- Clique numa empresa (TAP — mesmo item já selecionado) -----------------
    private async void OnCompanyTapped(object? sender, TappedEventArgs e)
    {
        var grid = sender as BindableObject;
        var chosen = grid?.BindingContext as CompanyDisplay;
        if (chosen == null) return;

        await StartHandleCompanyAsync(chosen, CompanyList);
    }

    private async Task StartHandleCompanyAsync(CompanyDisplay chosen, CollectionView? list)
    {
        if (_choosingCompany) return;          // guarda anti duplo clique
        _choosingCompany = true;

        try
        {
            Preferences.Default.Set(KeyCompanyCode, chosen.CodEmp);
            Preferences.Default.Set(KeyCompanyName, chosen.Empresa);
            await CompanyPickerPanel.FadeTo(0, 120, Easing.CubicIn);
            CompanyPickerPanel.IsVisible = false;
            var theme = Application.Current?.RequestedTheme ?? AppTheme.Light;
            LogoImage.Source = chosen.LogoSrc;
            var labelColor = theme == AppTheme.Dark ? Colors.White : Colors.Black;
            TituloLabel.TextColor = labelColor;
            DescricaoLabel.TextColor = labelColor;
            TituloLabel.Text = $"Bem-vindo à {chosen.Empresa}";
            DescricaoLabel.Text = "A carregar…";
            await ShowMainContentAsync();
            await Task.WhenAll(
                LogoImage.ScaleTo(1.03, 240),
                LogoImage.ScaleTo(1.00, 240)
            );
            // Separação por plataforma
            #if WINDOWS
            await NavigateToLoginPageWindowsAsync();
            #elif ANDROID
            await NavigateToLoginPageAndroidAsync();
            #elif IOS
            await NavigateToLoginPageiOSAsync();
            #elif MACCATALYST
            await NavigateToLoginPageMacOSAsync();
            #else
            // Comum: fallback
            if (MainThread.IsMainThread)
                await Shell.Current.GoToAsync("//Login");
            else
                await MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync("//Login"));
            #endif
        }
        catch
        {
            await ShowMainContentAsync();
        }
        finally
        {
            if (list != null) list.SelectedItem = null; // limpa seleção visual
            _choosingCompany = false;
        }
    }

    private async Task ShowMainContentAsync()
    {
        try
        {
            if (LogoImage.Opacity == 0) await LogoImage.FadeTo(1, 400);
            if (LogoImage.Scale < 1) await LogoImage.ScaleTo(1, 260, Easing.CubicOut);
            if (TituloLabel.Opacity == 0) await TituloLabel.FadeTo(1, 260);
            if (DescricaoLabel.Opacity == 0) await DescricaoLabel.FadeTo(1, 260);

        }
        catch { }
    }

    // Botão "Continuar" (mantido)
    private async void OnStartClicked(object sender, EventArgs e)
    {
        await NavigateToAsync("//Login");
    }

    private async void OnOpenConfigClicked(object sender, EventArgs e)
    {
        await NavigateToConfigAsync();
    }

    // ----------------- Navegação (mantida) -----------------
    private async Task NavigateToConfigAsync()
    {
        await ShowLoadingAsync(false);

        if (!await NavigateToAsync("config.db"))
            if (!await NavigateToAsync("//config.db"))
            {
                try
                {
                    if (MainThread.IsMainThread)
                    {
                        await Navigation.PushAsync(new Pages.DbConfigPage());
                    }
                    else
                    {
                        await MainThread.InvokeOnMainThreadAsync(() => Navigation.PushAsync(new Pages.DbConfigPage()));
                    }
                }
                catch { }
            }
    }

    private async Task<bool> NavigateToAsync(string route)
    {
        try
        {
            // If already on main thread, call directly; otherwise dispatch to UI thread.
            Task navTask;
            if (MainThread.IsMainThread)
            {
                navTask = Shell.Current.GoToAsync(route);
            }
            else
            {
                // Captura a task returned by GoToAsync executed on main thread
                navTask = MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync(route));
            }

            // Wait with timeout to avoid indefinite hang
            var completed = await Task.WhenAny(navTask, Task.Delay(3000));
            if (completed != navTask)
            {
                System.Diagnostics.Debug.WriteLine("Navigation to '{0}' timed out.", route);
                return false;
            }

            await navTask; // propagate exceptions
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Navigation failed: " + ex.Message);
            return false;
        }
    }

    // ----------------- Overlay de Loading (mantido) -----------------
    // Refactor: avoid passing async lambdas to MainThread.InvokeOnMainThreadAsync to prevent deadlocks.
    private Task ShowLoadingAsync(bool show)
    {
        try
        {
            if (MainThread.IsMainThread)
                return UpdateLoadingUiAsync(show);
            else
                return MainThread.InvokeOnMainThreadAsync(() => UpdateLoadingUiAsync(show));
        }
        catch
        {
            return Task.CompletedTask;
        }
    }

    private async Task UpdateLoadingUiAsync(bool show)
    {
        try
        {
            if (show)
            {
                _loadingCts?.Cancel();
                _loadingCts = new CancellationTokenSource();

                LoadingOverlay.IsVisible = true;
                LoadingOverlay.Opacity = 0;
                await LoadingOverlay.FadeTo(1, 150, Easing.CubicIn);
            }
            else
            {
                _loadingCts?.Cancel();
                if (LoadingOverlay.IsVisible)
                {
                    await LoadingOverlay.FadeTo(0, 120);
                    LoadingOverlay.IsVisible = false;
                }
            }
        }
        catch { }
    }

    // ----------------- Toast unificado -----------------
    private Task ShowToastAsync(string message, bool success, int ms = 1600)
    {
        // Mantém assinatura antiga mas delega para GlobalToast
        var tipo = success ? ToastTipo.Sucesso : ToastTipo.Erro;
        return GlobalToast.ShowAsync(message, tipo, ms);
    }
}

#if WINDOWS
// Código Windows específico (exemplo: animações, navegação, layouts)
#endif
#if ANDROID
// Código Android específico (exemplo: animações, navegação, layouts)
#endif
#if IOS
// Código iOS específico (exemplo: animações, navegação, layouts)
#endif
