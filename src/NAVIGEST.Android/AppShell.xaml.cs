using Microsoft.Maui.Controls;
using Syncfusion.Maui.Toolkit.SegmentedControl;
#if ANDROID
using Android.Util;
#endif

namespace NAVIGEST.Android;

public partial class AppShell : Shell
{
    private const string LogTag = "AppShell";
    public AppShell()
    {
        InitializeComponent();
        System.Diagnostics.Debug.WriteLine("[AppShell] ====== Inicializando AppShell ======");
        System.Diagnostics.Debug.WriteLine($"[AppShell] Items count: {this.Items.Count}");
        System.Diagnostics.Debug.WriteLine($"[AppShell] CurrentPage: {this.CurrentPage?.GetType().Name ?? "NULL"}");
#if ANDROID
        Log.Debug(LogTag, "AppShell ctor");
        Log.Debug(LogTag, $"Items count: {this.Items.Count}");
        Log.Debug(LogTag, $"CurrentPage: {this.CurrentPage?.GetType().Name ?? "NULL"}");
#endif
        
        // Registo de rotas principais
        Routing.RegisterRoute("Login", typeof(Pages.LoginPage));
        Routing.RegisterRoute(nameof(Pages.LoginPage), typeof(Pages.LoginPage));
        Routing.RegisterRoute("DbConfigPage", typeof(Pages.DbConfigPage));
        Routing.RegisterRoute("mainpage", typeof(Pages.MainYahPage));
        Routing.RegisterRoute("projects", typeof(Pages.ProjectListPage));
        Routing.RegisterRoute("manage", typeof(Pages.ManageMetaPage));

        // ✅ Registar SplashIntroPage corretamente
        Routing.RegisterRoute("splash", typeof(Pages.SplashIntroPage));
        Routing.RegisterRoute("welcome", typeof(Pages.WelcomePage));
    System.Diagnostics.Debug.WriteLine("[AppShell] Rotas registadas!");
#if ANDROID
    Log.Debug(LogTag, "Routes registered");
#endif

        // ✅ Forçar navegação inicial para SplashIntroPage (já é o conteúdo inicial, mas com fallback)
        this.Loaded += async (s, e) =>
        {
            System.Diagnostics.Debug.WriteLine("[AppShell] ====== Shell.Loaded disparado! ======");
            System.Diagnostics.Debug.WriteLine($"[AppShell] CurrentPage antes: {this.CurrentPage?.GetType().Name ?? "NULL"}");
#if ANDROID
            Log.Debug(LogTag, "Shell.Loaded raised");
            Log.Debug(LogTag, $"CurrentPage before: {this.CurrentPage?.GetType().Name ?? "NULL"}");
#endif
            try
            {
                // SplashIntroPage já deve estar visível, mas se não estiver, força
                if (CurrentPage is not Pages.SplashIntroPage)
                {
                    System.Diagnostics.Debug.WriteLine("[AppShell] ⚠️ CurrentPage NÃO é SplashIntroPage! Forçando navegação...");
                    System.Diagnostics.Debug.WriteLine($"[AppShell] CurrentPage é: {this.CurrentPage?.GetType().Name}");
#if ANDROID
                    Log.Warn(LogTag, "CurrentPage is not SplashIntroPage. Forcing navigation.");
                    Log.Warn(LogTag, $"CurrentPage is: {this.CurrentPage?.GetType().Name ?? "NULL"}");
#endif
                    await GoToAsync("splash", false);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[AppShell] ✅ CurrentPage JÁ é SplashIntroPage!");
#if ANDROID
                    Log.Debug(LogTag, "CurrentPage already SplashIntroPage");
#endif
                }
                System.Diagnostics.Debug.WriteLine("[AppShell] Pronto!");
#if ANDROID
                Log.Debug(LogTag, "Initial navigation ready");
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppShell] ❌ Erro: {ex.Message}\n{ex.StackTrace}");
#if ANDROID
                Log.Error(LogTag, $"Navigation error: {ex}");
#endif
            }
        };

    Navigated += OnShellNavigated;
    }

    protected override void OnHandlerChanged()
    {
    base.OnHandlerChanged();
#if ANDROID
    Log.Info(LogTag, $"OnHandlerChanged. Handler={(Handler?.GetType().Name ?? "null")}");
#endif
    }

    protected override void OnAppearing()
    {
    base.OnAppearing();
#if ANDROID
    Log.Info(LogTag, "OnAppearing");
#endif
    }

    private void OnShellNavigated(object? sender, ShellNavigatedEventArgs e)
    {
#if ANDROID
    Log.Info(LogTag, $"Navigated. Source={e?.Source} Current={e?.Current?.Location}");
#endif
    }

    private void SfSegmentedControl_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.SegmentedControl.SelectionChangedEventArgs e)
    {
        try
        {
            if (Application.Current != null)
                Application.Current.UserAppTheme = e.NewIndex == 0 ? AppTheme.Light : AppTheme.Dark;
        }
        catch { }
    }

    // Toasts delegados para GlobalToast
    public static Task DisplayToastAsync(string message) => GlobalToast.ShowAsync(message, ToastTipo.Info);
    public static Task DisplayToastAsync(string message, object param) => GlobalToast.ShowAsync(message, ToastTipo.Info);
    public static Task DisplayToastAsync(string message, object param, double duration) => GlobalToast.ShowAsync(message, ToastTipo.Info);
}
