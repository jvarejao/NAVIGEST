using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using NAVIGEST.iOS;
using NAVIGEST.iOS.Services;
using static NAVIGEST.iOS.GlobalErro;

namespace NAVIGEST.iOS.Pages
{
    public partial class MainYahPage : ContentPage
    {
        private TaskCompletionSource<bool>? _adminTcs;
        private bool _adminPwdShown;

        public MainYahPage()
        {
            try
            {
                InitializeComponent();
                SyncThemeVisual();
                NavigationPage.SetHasBackButton(this, false);
                var vm = new MainYahPageViewModel();
                vm.IsSidebarVisible = false;
                BindingContext = vm;
                vm.RefreshUserContext();
                Debug.WriteLine("[MainYahPage] BindingContext initialized and session refreshed.");
            }
            catch (Exception ex) { TratarErro(ex); }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                if (BindingContext is MainYahPageViewModel vm)
                {
                    vm.RefreshUserContext();
                    Debug.WriteLine($"[MainYahPage] OnAppearing refreshed session for company '{vm.CompanyName}'.");
                }
            }
            catch (Exception ex) { TratarErro(ex); }
        }

        public void OnMaximizeRestoreClicked(object sender, EventArgs e)
        {
#if WINDOWS
            try
            {
                var hwnd = GetWindowHandle();
                if (!_isMaximized)
                {
                    ShowWindow(hwnd, SW_MAXIMIZE);
                    _isMaximized = true;
                    if (MaxRestoreIcon != null) MaxRestoreIcon.Text = "\uf2d2"; // restore icon

                    // --- Correção: garantir que não fica atrás da barra de tarefas ---
                    RECT workArea = new RECT();
                    if (SystemParametersInfo(SPI_GETWORKAREA, 0, ref workArea, 0))
                    {
                        int width = workArea.Right - workArea.Left;
                        int height = workArea.Bottom - workArea.Top;
                        SetWindowPos(hwnd, IntPtr.Zero, workArea.Left, workArea.Top, width, height, SWP_NOZORDER);
                    }
                }
                else
                {
                    ShowWindow(hwnd, SW_RESTORE);
                    _isMaximized = false;
                    if (MaxRestoreIcon != null) MaxRestoreIcon.Text = "\uf2d0"; // maximize icon
                }
            }
            catch (Exception ex) { TratarErro(ex); }
#endif
        }

#if WINDOWS
        public void OnTopBarPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            try
            {
                if (e.StatusType == GestureStatus.Started)
                {
                    _dragging = true;
                }
                else if (e.StatusType == GestureStatus.Running && _dragging)
                {
                    var hwnd = GetWindowHandle();
                    ReleaseCapture();
                    SendMessage(hwnd, WM_SYSCOMMAND, SC_MOVE + HTCAPTION, 0);
                    _dragging = false;
                }
                else if (e.StatusType == GestureStatus.Completed || e.StatusType == GestureStatus.Canceled)
                {
                    _dragging = false;
                }
            }
            catch (Exception ex) { TratarErro(ex); }
        }
#else
        public void OnTopBarPanUpdated(object sender, PanUpdatedEventArgs e) { }
#endif

        private void SyncThemeVisual()
        {
            try
            {
                if (Application.Current is not App app) return;
                var theme = Application.Current?.UserAppTheme ?? AppTheme.Unspecified;

                if (this.FindByName("ThemeModePickerMobile") is Picker pickerMobile)
                {
                    if (app.IsAutoTheme) pickerMobile.SelectedIndex = 0;
                    else if (theme == AppTheme.Light) pickerMobile.SelectedIndex = 1;
                    else pickerMobile.SelectedIndex = 2;
                }

                if (this.FindByName("ThemeModeIconMobile") is Label iconMobile)
                {
                    string glyph = app.IsAutoTheme ? "\uf042" : (theme == AppTheme.Dark ? "\uf185" : "\uf186");
                    iconMobile.Text = glyph;
                }
            }
            catch (Exception ex) { TratarErro(ex); }
        }

        private void OnThemeModeChangedMobile(object sender, EventArgs e)
        {
            if (this.FindByName("ThemeModePickerMobile") is not Picker picker) return;
            try
            {
                if (Application.Current is not App app) return;
                switch (picker.SelectedIndex)
                {
                    case 0: app.EnableAutoTheme(); break;
                    case 1: app.DisableAutoTheme(); app.SetTheme(AppTheme.Light); break;
                    case 2: app.DisableAutoTheme(); app.SetTheme(AppTheme.Dark); break;
                }

                if (this.FindByName("ThemeModeIconMobile") is Label icon)
                {
                    var theme = Application.Current?.UserAppTheme ?? AppTheme.Unspecified;
                    string glyph = app.IsAutoTheme ? "\uf042" : (theme == AppTheme.Dark ? "\uf185" : "\uf186");
                    icon.Text = glyph;
                }
            }
            catch (Exception ex) { TratarErro(ex); }
        }

        // === UTIL: fechar sidebar em mobile após seleção ===
        void CloseSidebarMobileIfNeeded()
        {
            try
            {
                if (SidebarOverlay.IsVisible)
                    _ = HideSidebarOverlayAsync();
                if (BindingContext is MainYahPageViewModel vm)
                    vm.IsSidebarVisible = false;
            }
            catch (Exception ex) { TratarErro(ex); }
        }

        // Adiciona método para animar o sidebar overlay em Android/iOS
        public async Task ShowSidebarOverlayAsync()
        {
            SidebarOverlay.IsVisible = true;
            await SidebarOverlay.FadeTo(1, 1); // Garante layout
            var height = SidebarOverlay.Height;
            if (height <= 0)
            {
                var size = SidebarOverlay.Measure(SidebarOverlay.Width, double.PositiveInfinity);
                height = size.Height;
            }
            SidebarOverlay.TranslationY = -height;
            await SidebarOverlay.TranslateTo(0, 0, 300, Easing.CubicOut);
            if (BindingContext is MainYahPageViewModel vm)
                vm.IsSidebarVisible = true;
        }

        public async Task HideSidebarOverlayAsync()
        {
            await SidebarOverlay.TranslateTo(0, -SidebarOverlay.Height, 300, Easing.CubicIn);
            SidebarOverlay.IsVisible = false;
            if (BindingContext is MainYahPageViewModel vm)
                vm.IsSidebarVisible = false;
        }

        private void OnToggleSidebarTapped(object sender, EventArgs e)
        {
            try
            {
                if (BindingContext is MainYahPageViewModel vm)
                {
                    if (!SidebarOverlay.IsVisible)
                        _ = ShowSidebarOverlayAsync();
                    else
                        _ = HideSidebarOverlayAsync();
                }
            }
            catch (Exception ex) { TratarErro(ex); }
        }

        private async void OnMenuTapped(object sender, EventArgs e)
        {
            try
            {
                if (sender is Border border &&
                    border.GestureRecognizers.Count > 0 &&
                    border.GestureRecognizers[0] is TapGestureRecognizer tap &&
                    tap.CommandParameter is string route)
                {
                    await HideSidebarOverlayAsync();
                    switch (route)
                    {
                        case "config.utilizadores":
                            {
                                try
                                {
                                    var page = new RegisterPage();
                                    var pageContent = page.Content;
                                    if (BindingContext is MainYahPageViewModel vm3)
                                    {
                                        vm3.IsConfigExpanded = false;
                                        vm3.IsAdminUnlocked = true;
                                    }
                                    if (pageContent is not null)
                                    {
                                        pageContent.BindingContext = page.BindingContext ?? pageContent.BindingContext;
                                        ShowContent(pageContent);
                                        try
                                        {
                                            var mi = page.GetType().GetMethod("InitializeForHostAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                                            if (mi != null && mi.Invoke(page, null) is Task task) await task;
                                        }
                                        catch (Exception ex) { TratarErro(ex); }
                                    }
                                    else await DisplayToastAsync("RegisterPage sem conteúdo.");
                                }
                                catch (Exception ex) { TratarErro(ex); }
                                CloseSidebarMobileIfNeeded();
                                break;
                            }
                        case "config.db":
                            {
                                try
                                {
                                    var page = new DbConfigPage();
                                    var pageContent = page.Content;
                                    if (BindingContext is MainYahPageViewModel vm4) vm4.IsConfigExpanded = false;
                                    if (pageContent is not null)
                                    {
                                        pageContent.BindingContext = page.BindingContext ?? pageContent.BindingContext;
                                        ShowContent(pageContent);
                                    }
                                    else await DisplayToastAsync("DbConfigPage sem conteúdo.");
                                }
                                catch (Exception ex) { TratarErro(ex); }
                                CloseSidebarMobileIfNeeded();
                                break;
                            }
                        case "clientes":
                            {
                                try
                                {
                                    var page = new ClientsPage();
                                    var pageContent = page.Content;
                                    if (BindingContext is MainYahPageViewModel vm5) vm5.IsConfigExpanded = false;
                                    if (pageContent is not null)
                                    {
                                        pageContent.BindingContext = page.BindingContext ?? pageContent.BindingContext;
                                        ShowContent(pageContent);
                                        try
                                        {
                                            var mi = page.GetType().GetMethod("InitializeForHostAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                                            if (mi != null && mi.Invoke(page, null) is Task task) await task;
                                        }
                                        catch (Exception ex) { TratarErro(ex); }
                                    }
                                    else await DisplayToastAsync("ClientsPage sem conteúdo.");
                                }
                                catch (Exception ex) { TratarErro(ex); }
                                CloseSidebarMobileIfNeeded();
                                break;
                            }
                        case "produtos":
                            {
                                try
                                {
                                    var page = new ProductsPage();
                                    var pageContent = page.Content;
                                    if (BindingContext is MainYahPageViewModel vm6) vm6.IsConfigExpanded = false;
                                    if (pageContent is not null)
                                    {
                                        pageContent.BindingContext = page.BindingContext ?? pageContent.BindingContext;
                                        ShowContent(pageContent);
                                        try
                                        {
                                            var mi = page.GetType().GetMethod("InitializeForHostAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                                            if (mi != null && mi.Invoke(page, null) is Task task) await task;
                                        }
                                        catch { }
                                    }
                                    else await DisplayToastAsync("ProductsPage sem conteúdo.");
                                }
                                catch (Exception ex) { TratarErro(ex); }
                                CloseSidebarMobileIfNeeded();
                                break;
                            }
                        case "servicos":
                            {
                                try
                                {
                                    var page = new ServicePage();
                                    var pageContent = page.Content;
                                    if (BindingContext is MainYahPageViewModel vm7) vm7.IsConfigExpanded = false;
                                    if (pageContent is not null)
                                    {
                                        pageContent.BindingContext = page.BindingContext ?? pageContent.BindingContext;
                                        ShowContent(pageContent);
                                        try
                                        {
                                            var mi = page.GetType().GetMethod("InitializeForHostAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                                            if (mi != null && mi.Invoke(page, null) is Task task) await task;
                                        }
                                        catch (Exception ex) { TratarErro(ex); }
                                    }
                                    else await DisplayToastAsync("ServicePage sem conteúdo.");
                                }
                                catch (Exception ex) { TratarErro(ex); }
                                CloseSidebarMobileIfNeeded();
                                break;
                            }
                        case "horas":
                            {
                                try
                                {
                                    // Resolve a página via DI (respeita o teu construtor com VM)
                                    var services = this.Handler?.MauiContext?.Services;
                                    var page = services?.GetService<HoursEntryPage>();

                                    if (page == null)
                                    {
                                        // fallback defensivo (não deve acontecer se DI estiver correto)
                                        page = new HoursEntryPage(new NAVIGEST.iOS.ViewModels.HoursEntryViewModel());
                                    }

                                    var pageContent = page.Content;

                                    if (BindingContext is MainYahPageViewModel vmX)
                                    {
                                        vmX.IsConfigExpanded = false; // manter coerência com os outros casos
                                    }

                                    if (pageContent is not null)
                                    {
                                        // manter o padrão que já usas nas outras páginas
                                        pageContent.BindingContext = page.BindingContext ?? pageContent.BindingContext;
                                        ShowContent(pageContent);

                                        // opcional: invocar OnAppearing se o resto do código o faz noutros casos
                                        try
                                        {
                                            var mi = page.GetType().GetMethod("OnAppearing",
                                                System.Reflection.BindingFlags.Instance |
                                                System.Reflection.BindingFlags.Public |
                                                System.Reflection.BindingFlags.NonPublic);
                                            mi?.Invoke(page, null);
                                        }
                                        catch { /* silencioso como nos outros casos */ }
                                    }
                                    else
                                    {
                                        await DisplayToastAsync("HoursEntryPage sem conteúdo.");
                                    }
                                }
                                catch (Exception ex) { TratarErro(ex); }

                                CloseSidebarMobileIfNeeded();
                                break;
                            }
                        case "swipe":
                            {
                                try
                                {
                                    var page = new SwipeProofPage();
                                    var pageContent = page.Content;
                                    if (BindingContext is MainYahPageViewModel vm8) vm8.IsConfigExpanded = false;
                                    if (pageContent is not null)
                                    {
                                        pageContent.BindingContext = page.BindingContext ?? pageContent.BindingContext;
                                        ShowContent(pageContent);
                                    }
                                    else await DisplayToastAsync("SwipeProofPage sem conteúdo.");
                                }
                                catch (Exception ex) { TratarErro(ex); }
                                CloseSidebarMobileIfNeeded();
                                break;
                            }

                    }
                }
            }
            catch (Exception ex) { TratarErro(ex); }
        }

        private async void ShowContent(View? view)
        {
            try
            {
                var old = ContentHost?.Content;
                if (old != null) await old.FadeTo(0, 120);
                if (ContentHost != null) ContentHost.Content = view;
                if (view != null) { view.Opacity = 0; await view.FadeTo(1, 120); }
            }
            catch (Exception ex) { TratarErro(ex); }
        }

        private async void OnConfigTapped(object sender, EventArgs e)
        {
            try
            {
                if (BindingContext is not MainYahPageViewModel vm) return;
                bool isCurrentAdmin = string.Equals(vm.UserRole, "ADMIN", StringComparison.OrdinalIgnoreCase);
                if (vm.IsConfigExpanded)
                {
                    vm.IsConfigExpanded = false;
                    if (!isCurrentAdmin) vm.IsAdminUnlocked = false;
                    return;
                }
                if (isCurrentAdmin)
                {
                    vm.IsAdminUnlocked = true; vm.IsConfigExpanded = true; return;
                }
                bool ok = await ShowAdminOverlayAsync();
                if (!ok)
                {
                    await DisplayAlert("Acesso negado", "Precisa de privilégios ADMIN.", "OK");
                    return;
                }
                vm.IsAdminUnlocked = true; vm.IsConfigExpanded = true;
            }
            catch (Exception ex) { TratarErro(ex); }
        }

        private async Task<bool> ShowAdminOverlayAsync()
        {
            try
            {
                _adminTcs = new TaskCompletionSource<bool>();
                AdminErrorLabel.IsVisible = false;
                AdminUserEntry.Text = string.Empty;
                AdminPassEntry.Text = string.Empty;
                _adminPwdShown = false; AdminPassEntry.IsPassword = true;
                if (AdminEyeIcon is FontImageSource fi) fi.Glyph = "\uf06e";
                AdminOverlay.IsVisible = true; await Task.Delay(50); AdminUserEntry.Focus();
                return await _adminTcs.Task;
            }
            catch (Exception ex) { TratarErro(ex); return false; }
        }

        private void OnAdminTogglePwd(object sender, EventArgs e)
        {
            try
            {
                _adminPwdShown = !_adminPwdShown; AdminPassEntry.IsPassword = !_adminPwdShown;
                if (AdminEyeIcon is FontImageSource fi) fi.Glyph = _adminPwdShown ? "\uf070" : "\uf06e";
            }
            catch (Exception ex) { TratarErro(ex); }
        }

        private void OnAdminCancel(object sender, EventArgs e)
        {
            try { AdminOverlay.IsVisible = false; _adminTcs?.TrySetResult(false); }
            catch (Exception ex) { TratarErro(ex); }
        }

        private async void OnAdminOk(object sender, EventArgs e)
        {
            try
            {
                AdminErrorLabel.IsVisible = false;
                var user = AdminUserEntry.Text?.Trim() ?? "";
                var pass = AdminPassEntry.Text ?? "";
                if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
                { AdminErrorLabel.Text = "Indica utilizador e palavra-passe."; AdminErrorLabel.IsVisible = true; return; }
                var loginResult = await DatabaseService.CheckLoginAsync(user, pass);
                if (!loginResult.Ok)
                {
                    AdminErrorLabel.Text = "Credenciais inválidas.";
                    AdminErrorLabel.IsVisible = true;
                    return;
                }

                var tipo = loginResult.Tipo ?? string.Empty;
                if (!string.Equals(tipo, "ADMIN", StringComparison.OrdinalIgnoreCase)) { AdminErrorLabel.Text = "Precisa de privilégios ADMIN."; AdminErrorLabel.IsVisible = true; return; }
                AdminOverlay.IsVisible = false; _adminTcs?.TrySetResult(true);
            }
            catch (Exception ex) { TratarErro(ex); }
        }

        private Task DisplayToastAsync(string message) => DisplayAlert("Info", message, "OK");
    }
}
