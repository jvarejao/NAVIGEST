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
                // Se existir logo personalizado, usar sempre
                if (UserSession.Current.User.CompanyLogo != null)
                    return ImageSource.FromStream(() => new MemoryStream(UserSession.Current.User.CompanyLogo));
                // Alterna entre yahcores.png (claro) e yahcorbranco.png (escuro)
                var theme = Application.Current?.RequestedTheme ?? AppTheme.Light;
                return theme == AppTheme.Dark ? "yahcorbranco.png" : "yahcores.png";
            }
        }
        public string CompanyName => UserSession.Current.User.CompanyName;

        public MainYahPageViewModel()
        {
            try
            {
                // Desbloqueia automaticamente se já for ADMIN
                _isAdminUnlocked = string.Equals(UserRole, "ADMIN", StringComparison.OrdinalIgnoreCase);
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
