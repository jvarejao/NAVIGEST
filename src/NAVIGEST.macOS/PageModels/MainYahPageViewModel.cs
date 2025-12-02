using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using NAVIGEST.macOS; // GlobalErro
using static NAVIGEST.macOS.GlobalErro;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace NAVIGEST.macOS.Pages
{
    public class MainYahPageViewModel : INotifyPropertyChanged
    {
        private bool _isSidebarExpanded = true;
        private double _sidebarWidth = 260;
        private bool _isConfigExpanded = false;
        private bool _isSidebarVisible;
        private bool _isAdminUnlocked;
        private bool _isFinancialUnlocked;

        public event PropertyChangedEventHandler? PropertyChanged;

        // User info (vindo de UserSession)
        public string UserName => UserSession.Current.User.Name;
        public string UserRole => UserSession.Current.User.Role;
        public ImageSource UserPhoto => UserSession.Current.User.Photo != null
            ? ImageSource.FromStream(() => new MemoryStream(UserSession.Current.User.Photo))
            : "user_placeholder.png";
        public ImageSource CompanyLogo
        {
            get
            {
                var logo = UserSession.Current.User.CompanyLogo;
                if (logo != null && logo.Length > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainYahPageViewModel] Returning custom logo ({logo.Length} bytes)");
                    Console.WriteLine($"[MainYahPageViewModel] Returning custom logo ({logo.Length} bytes)");
                    return ImageSource.FromStream(() => new MemoryStream(logo));
                }
                
                // Alterna entre yahcores.png (claro) e yahcorbranco.png (escuro)
                var theme = Application.Current?.RequestedTheme ?? AppTheme.Light;
                var defaultLogo = theme == AppTheme.Dark ? "yahcorbranco.png" : "yahcores.png";
                System.Diagnostics.Debug.WriteLine($"[MainYahPageViewModel] Returning default logo: {defaultLogo}");
                Console.WriteLine($"[MainYahPageViewModel] Returning default logo: {defaultLogo}");
                return defaultLogo;
            }
        }
        public string CompanyName => UserSession.Current.User.CompanyName;

        public MainYahPageViewModel()
        {
            try
            {
                // Desbloqueia automaticamente se já for ADMIN
                _isAdminUnlocked = UserSession.Current.User.IsAdmin;
                _isFinancialUnlocked = UserSession.Current.User.IsFinancial;
            }
            catch (Exception ex) { TratarErro(ex); }
        }

        public bool IsSidebarExpanded
        {
            get => _isSidebarExpanded;
            private set { if (_isSidebarExpanded != value) { _isSidebarExpanded = value; OnPropertyChanged(); } }
        }

        public double SidebarWidth
        {
            get => _sidebarWidth;
            private set { if (Math.Abs(_sidebarWidth - value) > 0.1) { _sidebarWidth = value; OnPropertyChanged(); } }
        }

        public bool IsConfigExpanded
        {
            get => _isConfigExpanded;
            set { if (_isConfigExpanded != value) { _isConfigExpanded = value; OnPropertyChanged(); } }
        }

        public bool IsSidebarVisible
        {
            get => _isSidebarVisible;
            set { if (_isSidebarVisible != value) { _isSidebarVisible = value; OnPropertyChanged(); } }
        }

        // Visibilidade de itens ADMIN no menu
        public bool IsAdminUnlocked
        {
            get => _isAdminUnlocked;
            set { if (_isAdminUnlocked != value) { _isAdminUnlocked = value; OnPropertyChanged(); } }
        }

        public bool IsFinancialUnlocked
        {
            get => _isFinancialUnlocked;
            set { if (_isFinancialUnlocked != value) { _isFinancialUnlocked = value; OnPropertyChanged(); } }
        }

        public bool IsHoursVisible => UserSession.Current.User.IsFinancial || UserSession.Current.User.IsGeneralSupervisor;

        private bool _isDbConnected;
        public bool IsDbConnected
        {
            get => _isDbConnected;
            set { _isDbConnected = value; OnPropertyChanged(); }
        }

        private bool _isFileServerConnected;
        public bool IsFileServerConnected
        {
            get => _isFileServerConnected;
            set { _isFileServerConnected = value; OnPropertyChanged(); }
        }

        public async Task CheckConnectionsAsync()
        {
            IsDbConnected = await Services.DatabaseService.TestConnectionAsync();
            IsFileServerConnected = await Services.FolderService.TestConnectionAsync();
        }

        public void Refresh()
        {
            System.Diagnostics.Debug.WriteLine($"[MainYahPageViewModel] Refresh called. Company: {CompanyName}, Logo bytes: {UserSession.Current.User.CompanyLogo?.Length ?? 0}");
            Console.WriteLine($"[MainYahPageViewModel] Refresh called. Company: {CompanyName}, Logo bytes: {UserSession.Current.User.CompanyLogo?.Length ?? 0}");
            OnPropertyChanged(nameof(UserName));
            OnPropertyChanged(nameof(UserRole));
            OnPropertyChanged(nameof(UserPhoto));
            OnPropertyChanged(nameof(CompanyLogo));
            OnPropertyChanged(nameof(CompanyName));
            OnPropertyChanged(nameof(IsHoursVisible));

            IsAdminUnlocked = UserSession.Current.User.IsAdmin;
            IsFinancialUnlocked = UserSession.Current.User.IsFinancial;
        }

        public void ToggleSidebar()
        {
            try { IsSidebarVisible = !IsSidebarVisible; } catch (Exception ex) { TratarErro(ex); }
        }

        public void SetSidebarExpanded(bool expanded)
        {
            try
            {
                IsSidebarExpanded = expanded;
                SidebarWidth = expanded ? 260 : 56;
            }
            catch (Exception ex) { TratarErro(ex); }
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
