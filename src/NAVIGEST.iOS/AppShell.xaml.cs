// File: AppShell.xaml.cs
using Microsoft.Maui.Controls;
using Syncfusion.Maui.Toolkit.SegmentedControl;

namespace NAVIGEST.iOS;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("Login", typeof(Pages.LoginPage));
        Routing.RegisterRoute(nameof(Pages.LoginPage), typeof(Pages.LoginPage));
        Routing.RegisterRoute("DbConfigPage", typeof(Pages.DbConfigPage));
        Routing.RegisterRoute("mainpage", typeof(Pages.MainYahPage));
        Routing.RegisterRoute("projects", typeof(Pages.ProjectListPage));
        Routing.RegisterRoute("manage", typeof(Pages.ManageMetaPage));
    }

    private void SfSegmentedControl_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.SegmentedControl.SelectionChangedEventArgs e)
    {
        try { if (Application.Current != null) Application.Current.UserAppTheme = e.NewIndex == 0 ? AppTheme.Light : AppTheme.Dark; } catch { }
    }

    // Toasts delegados para GlobalToast (mantendo assinaturas usadas no código)
    public static Task DisplayToastAsync(string message) => GlobalToast.ShowAsync(message, ToastTipo.Info);
    public static Task DisplayToastAsync(string message, object param) => GlobalToast.ShowAsync(message, ToastTipo.Info);
    public static Task DisplayToastAsync(string message, object param, double duration) => GlobalToast.ShowAsync(message, ToastTipo.Info);
}
