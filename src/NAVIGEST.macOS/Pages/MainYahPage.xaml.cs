using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Platform;
using Microsoft.Extensions.DependencyInjection;
using NAVIGEST.macOS;
using static NAVIGEST.macOS.GlobalErro;
using NAVIGEST.macOS.Services;

namespace NAVIGEST.macOS.Pages
{
    public partial class MainYahPage : ContentPage
    {
        private bool _isMaximized = false;
        private bool _dragging = false;
        private View? _homeContent;
        private bool _initThemePicker;
        private TaskCompletionSource<bool>? _adminTcs;
        private bool _adminPwdShown;

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is MainYahPageViewModel vm)
            {
                vm.Refresh();
                await vm.CheckConnectionsAsync();
                
                // Forçar atualização manual da imagem para garantir que o logo carregado no login apareça
                try
                {
                    var logoBytes = UserSession.Current?.User?.CompanyLogo;
                    if (logoBytes != null && logoBytes.Length > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainYahPage] Manually setting logo source. Bytes: {logoBytes.Length}");
                        CompanyLogoImage.Source = ImageSource.FromStream(() => new System.IO.MemoryStream(logoBytes));
                    }
                    else
                    {
                        // Fallback para o logo padrão se não houver customizado
                        var theme = Application.Current?.RequestedTheme ?? AppTheme.Light;
                        CompanyLogoImage.Source = theme == AppTheme.Dark ? "yahcorbranco.png" : "yahcores.png";
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainYahPage] Error setting logo: {ex.Message}");
                }
            }
        }

        public MainYahPage()
        {
            try
            {
                InitializeComponent();
                SyncThemeVisual();
                _homeContent = ContentHost?.Content;
                NavigationPage.SetHasBackButton(this, false);
                var vm = new MainYahPageViewModel();
                vm.IsSidebarVisible = false; // Sidebar oculto por padrão
                BindingContext = vm;

                // Runtime platform adjustments to ensure mobile layout on iOS/Android
                try
                {
                    if (DeviceInfo.Platform == DevicePlatform.iOS || DeviceInfo.Platform == DevicePlatform.Android)
                    {
                        // Hide desktop-only sidebar if present and ensure overlay starts hidden
                        var sidebar = this.FindByName("Sidebar") as VisualElement;
                        var overlay = this.FindByName("SidebarOverlay") as VisualElement;
                        if (sidebar != null) sidebar.IsVisible = false;
                        if (overlay != null)
                        {
                            overlay.IsVisible = false;
                            // keep it off-screen until toggled
                            overlay.TranslationY = -1000;
                        }

                        // Hide window control area if exists
                        var winControls = this.FindByName("MaxRestoreIcon") as VisualElement;
                        if (winControls != null) winControls.IsVisible = false;
                    }
                }
                catch (Exception ex) { TratarErro(ex); }

#if WINDOWS || MACCATALYST
                if (MaxRestoreIcon != null) MaxRestoreIcon.Text = "\uf2d2";
#endif
#if !ANDROID && !IOS
                this.SizeChanged += (s, e) =>
                {
                    try
                    {
                        if (BindingContext is MainYahPageViewModel vm2)
                        {
                            bool narrow = this.Width < 860;
                            vm2.SetSidebarExpanded(!narrow);
                        }
                    }
                    catch (Exception ex) { TratarErro(ex); }
                };
#endif
            }
            catch (Exception ex) { TratarErro(ex); }
        }

#if WINDOWS
        [DllImport("user32.dll")] static extern IntPtr GetActiveWindow();
        [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")] static extern bool ReleaseCapture();
        [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")] static extern bool SystemParametersInfo(int uAction, int uParam, ref RECT lpvParam, int fuWinIni);
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int Left, Top, Right, Bottom; }
        const int SW_MINIMIZE = 6;
        const int SW_MAXIMIZE = 3;
        const int SW_RESTORE = 9;
        const int WM_SYSCOMMAND = 0x0112;
        const int SC_MOVE = 0xF010;
        const int HTCAPTION = 0x0002;
        const int SPI_GETWORKAREA = 0x0030;
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        const uint SWP_NOZORDER = 0x0004;

        private IntPtr GetWindowHandle()
        {
            var win = Application.Current?.Windows.Count > 0 ? Application.Current.Windows[0] : null;
            if (win?.Handler?.PlatformView is Microsoft.UI.Xaml.Window nativeWin)
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(nativeWin);
                return hwnd;
            }
            return GetActiveWindow();
        }
#endif

        public void OnMinimizeClicked(object sender, EventArgs e)
        {
#if WINDOWS
            try
            {
                var hwnd = GetWindowHandle();
                ShowWindow(hwnd, SW_MINIMIZE);
            }
            catch (Exception ex) { TratarErro(ex); }
#endif
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
                var app = (App?)Application.Current;
                var theme = Application.Current?.UserAppTheme;

                // --- DESKTOP BUTTONS ---
                var btnAuto = this.FindByName("ThemeBtnAuto") as Border;
                var btnLight = this.FindByName("ThemeBtnLight") as Border;
                var btnDark = this.FindByName("ThemeBtnDark") as Border;

                if (btnAuto != null && btnLight != null && btnDark != null)
                {
                    // Cores
                    var activeColor = Color.FromArgb("#FF9500"); // Orange
                    var inactiveColor = Colors.Transparent; // Ou SidebarItem se acessível via código, mas Transparent funciona bem no sidebar
                    
                    // Estados
                    bool isAuto = app?.IsAutoTheme == true;
                    bool isLight = !isAuto && theme == AppTheme.Light;
                    bool isDark = !isAuto && theme == AppTheme.Dark;

                    btnAuto.BackgroundColor = isAuto ? activeColor : inactiveColor;
                    btnLight.BackgroundColor = isLight ? activeColor : inactiveColor;
                    btnDark.BackgroundColor = isDark ? activeColor : inactiveColor;
                    
                    // Ajustar cor do ícone para contraste se ativo
                    if (btnAuto.Content is Label lA) lA.TextColor = isAuto ? Colors.White : (Color)Application.Current.Resources["SidebarAccent"];
                    if (btnLight.Content is Label lL) lL.TextColor = isLight ? Colors.White : (Color)Application.Current.Resources["SidebarAccent"];
                    if (btnDark.Content is Label lD) lD.TextColor = isDark ? Colors.White : (Color)Application.Current.Resources["SidebarAccent"];
                }

                // Ícone Geral
                var iconObj = this.FindByName("ThemeModeIcon");
                if (iconObj is Label icon)
                {
                    string glyph = app?.IsAutoTheme == true ? "\uf042" : (theme == AppTheme.Dark ? "\uf185" : "\uf186");
                    icon.Text = glyph;
                }

                // --- MOBILE PICKER ---
                var pickerMobile = this.FindByName("ThemeModePickerMobile") as Picker;
                if (pickerMobile != null)
                {
                    if (app.IsAutoTheme) pickerMobile.SelectedIndex = 0;
                    else if (theme == AppTheme.Light) pickerMobile.SelectedIndex = 1;
                    else pickerMobile.SelectedIndex = 2;
                }
                var iconMobile = this.FindByName("ThemeModeIconMobile") as Label;
                if (iconMobile != null)
                {
                    string glyph = app.IsAutoTheme ? "\uf042" : (theme == AppTheme.Dark ? "\uf185" : "\uf186");
                    iconMobile.Text = glyph;
                }
            }
            catch { }
        }

        private void OnThemeButtonTapped(object sender, EventArgs e)
        {
            try
            {
                string? mode = null;

                // 1. Try TappedEventArgs
                if (e is TappedEventArgs tappedArgs && tappedArgs.Parameter is string p)
                {
                    mode = p;
                }
                // 2. Fallback: Check GestureRecognizers on sender
                else if (sender is View v)
                {
                    foreach (var g in v.GestureRecognizers)
                    {
                        if (g is TapGestureRecognizer tap && tap.CommandParameter is string cmdParam)
                        {
                            mode = cmdParam;
                            break;
                        }
                    }
                }

                if (mode != null)
                {
                    var app = (App)Application.Current;
                    if (app != null)
                    {
                        switch (mode)
                        {
                            case "Auto": app.EnableAutoTheme(); break;
                            case "Light": app.DisableAutoTheme(); app.SetTheme(AppTheme.Light); break;
                            case "Dark": app.DisableAutoTheme(); app.SetTheme(AppTheme.Dark); break;
                        }
                        SyncThemeVisual();
                    }
                }
            }
            catch (Exception ex) { TratarErro(ex); }
        }

        private void OnThemeModeChanged(object sender, EventArgs e)
        {
            // Mantido apenas se houver referências legadas, mas o Desktop agora usa OnThemeButtonTapped
        }

        private void OnThemeModeChangedMobile(object sender, EventArgs e)
        {
#if ANDROID || IOS
            var pickerObj = this.FindByName("ThemeModePickerMobile");
            if (pickerObj is not Picker picker) return;
            try
            {
                var app = (App)Application.Current;
                switch (picker.SelectedIndex)
                {
                    case 0: app.EnableAutoTheme(); break;
                    case 1: app.DisableAutoTheme(); app.SetTheme(AppTheme.Light); break;
                    case 2: app.DisableAutoTheme(); app.SetTheme(AppTheme.Dark); break;
                }
                // Atualiza ícone do tema
                var iconObj = this.FindByName("ThemeModeIconMobile");
                if (iconObj is Label icon)
                {
                    var theme = Application.Current.UserAppTheme;
                    string glyph = app.IsAutoTheme ? "\uf042" : (theme == AppTheme.Dark ? "\uf185" : "\uf186");
                    icon.Text = glyph;
                }
            }
            catch (Exception ex) { TratarErro(ex); }
#endif
        }

        // === UTIL: fechar sidebar em mobile após seleção ===
        void CloseSidebarMobileIfNeeded()
        {
#if ANDROID || IOS
            try
            {
                if (BindingContext is MainYahPageViewModel vm)
                    vm.IsSidebarVisible = false;
            }
            catch (Exception ex) { TratarErro(ex); }
#endif
        }

        // Adiciona método para animar o sidebar overlay em Android/iOS
        public async Task ShowSidebarOverlayAsync()
        {
#if ANDROID || IOS
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
#endif
        }

        public async Task HideSidebarOverlayAsync()
        {
#if ANDROID || IOS
            await SidebarOverlay.TranslateTo(0, -SidebarOverlay.Height, 300, Easing.CubicIn);
            SidebarOverlay.IsVisible = false;
#else
            await Task.CompletedTask;
#endif
        }

        private void OnToggleSidebarTapped(object sender, EventArgs e)
        {
            try
            {
                if (BindingContext is MainYahPageViewModel vm)
                {
#if ANDROID || IOS
                    if (!SidebarOverlay.IsVisible)
                        _ = ShowSidebarOverlayAsync();
                    else
                        _ = HideSidebarOverlayAsync();
#else
                    vm.IsSidebarVisible = !vm.IsSidebarVisible;
#endif
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
                    // Fecha sidebar ao clicar em qualquer menu
#if ANDROID || IOS
                    await HideSidebarOverlayAsync();
#else
                    if (BindingContext is MainYahPageViewModel vm2)
                        vm2.IsSidebarVisible = false;
#endif
                    switch (route)
                    {
                        case "settings":
                            {
                                try
                                {
                                    await Shell.Current.GoToAsync("SettingsPage");
                                }
                                catch (Exception ex) { TratarErro(ex); }
                                CloseSidebarMobileIfNeeded();
                                break;
                            }
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
                                    else await DisplayToastAsync(string.Format(NAVIGEST.macOS.Resources.Strings.AppResources.Main_NoContent, "RegisterPage"));
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
                                    else await DisplayToastAsync(string.Format(NAVIGEST.macOS.Resources.Strings.AppResources.Main_NoContent, "DbConfigPage"));
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
                                    else await DisplayToastAsync(string.Format(NAVIGEST.macOS.Resources.Strings.AppResources.Main_NoContent, "ClientsPage"));
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
                                    else await DisplayToastAsync(string.Format(NAVIGEST.macOS.Resources.Strings.AppResources.Main_NoContent, "ProductsPage"));
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
                                    else await DisplayToastAsync(string.Format(NAVIGEST.macOS.Resources.Strings.AppResources.Main_NoContent, "ServicePage"));
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
                                        page = new HoursEntryPage(new NAVIGEST.macOS.ViewModels.HorasColaboradorViewModel());
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
                                        await DisplayToastAsync(string.Format(NAVIGEST.macOS.Resources.Strings.AppResources.Main_NoContent, "HoursEntryPage"));
                                    }
                                }
                                catch (Exception ex) { TratarErro(ex); }

                                CloseSidebarMobileIfNeeded();
                                break;
                            }
                        case "testchart":
                            {
                                try
                                {
                                    var page = new TestChartPage();
                                    // TestChartPage is a ContentView, so we can use it directly as content
                                    if (BindingContext is MainYahPageViewModel vmTest) vmTest.IsConfigExpanded = false;
                                    
                                    // Since it's a ContentView, we don't need to extract .Content
                                    ShowContent(page);
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

        private async void OnLogoutTapped(object sender, EventArgs e)
        {
            try
            {
                bool confirm = await DisplayAlert(
                    NAVIGEST.macOS.Resources.Strings.AppResources.Main_LogoutTitle,
                    NAVIGEST.macOS.Resources.Strings.AppResources.Main_LogoutMessage,
                    NAVIGEST.macOS.Resources.Strings.AppResources.Main_LogoutConfirm,
                    NAVIGEST.macOS.Resources.Strings.AppResources.Common_Cancel);
                if (confirm)
                {
                    CloseSidebarMobileIfNeeded();
                    // Navegar para a página de Login e limpar a pilha de navegação
                    await Shell.Current.GoToAsync("//Login");
                }
            }
            catch (Exception ex) { TratarErro(ex); }
        }

        private async void OnCloseTapped(object sender, EventArgs e)
        {
            try
            {
#if WINDOWS
                Application.Current?.Quit();
#else
                await DisplayAlert(
                    NAVIGEST.macOS.Resources.Strings.AppResources.Main_CloseTitle,
                    NAVIGEST.macOS.Resources.Strings.AppResources.Main_CloseMessage,
                    NAVIGEST.macOS.Resources.Strings.AppResources.Common_OK);
#endif
                CloseSidebarMobileIfNeeded();
            }
            catch (Exception ex) { TratarErro(ex); }
        }

        private void OnCloseClicked(object sender, EventArgs e)
        {
            try { OnCloseTapped(sender, e); }
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
                    await DisplayAlert(
                        NAVIGEST.macOS.Resources.Strings.AppResources.Main_AccessDenied,
                        NAVIGEST.macOS.Resources.Strings.AppResources.Main_AdminRequired,
                        NAVIGEST.macOS.Resources.Strings.AppResources.Common_OK);
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
                { 
                    AdminErrorLabel.Text = NAVIGEST.macOS.Resources.Strings.AppResources.Main_AdminEnterCredentials; 
                    AdminErrorLabel.IsVisible = true; 
                    return; 
                }
                (bool ok, string _nome, string tipo) = await DatabaseService.CheckLoginAsync(user, pass);
                if (!ok) 
                { 
                    AdminErrorLabel.Text = NAVIGEST.macOS.Resources.Strings.AppResources.Login_InvalidCredentials; 
                    AdminErrorLabel.IsVisible = true; 
                    return; 
                }
                if (!string.Equals(tipo, "ADMIN", StringComparison.OrdinalIgnoreCase)) 
                { 
                    AdminErrorLabel.Text = NAVIGEST.macOS.Resources.Strings.AppResources.Main_AdminRequired; 
                    AdminErrorLabel.IsVisible = true; 
                    return; 
                }
                AdminOverlay.IsVisible = false; _adminTcs?.TrySetResult(true);
            }
            catch (Exception ex) { TratarErro(ex); }
        }

        private Task DisplayToastAsync(string message) => DisplayAlert("Info", message, "OK");
    }
}
