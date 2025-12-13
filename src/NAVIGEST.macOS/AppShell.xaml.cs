// File: AppShell.xaml.cs
using Microsoft.Maui.Controls;
using Syncfusion.Maui.Toolkit.SegmentedControl;

namespace NAVIGEST.macOS;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        // Routing.RegisterRoute("Login", typeof(Pages.LoginPage)); // Agora é ShellContent
        // Routing.RegisterRoute(nameof(Pages.LoginPage), typeof(Pages.LoginPage)); // Já está no XAML como "Login"
        
        // Routing.RegisterRoute("mainpage", typeof(Pages.MainYahPage)); // Já está no XAML
        
        Routing.RegisterRoute("projects", typeof(Pages.ProjectListPage));
        Routing.RegisterRoute("manage", typeof(Pages.ManageMetaPage));
        
        // Definições
        Routing.RegisterRoute("SettingsPage", typeof(Pages.SettingsPage));
        // Routing.RegisterRoute("FileServerSetupPage", typeof(Pages.FileServerSetupPage)); // Duplicado
        Routing.RegisterRoute("config.utilizadores", typeof(Pages.RegisterPage));
        Routing.RegisterRoute("config.db", typeof(Pages.DbConfigPage));
        Routing.RegisterRoute("config.fileserver", typeof(Pages.FileServerSetupPage));
        Routing.RegisterRoute("config.servicestatus", typeof(Pages.ServiceStatusPage));
    }

    private void SfSegmentedControl_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.SegmentedControl.SelectionChangedEventArgs e)
    {
        try { Application.Current.UserAppTheme = e.NewIndex == 0 ? AppTheme.Light : AppTheme.Dark; } catch { }
    }

    // Toasts delegados para GlobalToast (mantendo assinaturas usadas no código)
    public static Task DisplayToastAsync(string message) => GlobalToast.ShowAsync(message, ToastTipo.Info);
    public static Task DisplayToastAsync(string message, object param) => GlobalToast.ShowAsync(message, ToastTipo.Info);
    public static Task DisplayToastAsync(string message, object param, double duration) => GlobalToast.ShowAsync(message, ToastTipo.Info);
}
