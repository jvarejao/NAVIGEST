using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;
using NAVIGEST.Android; // GlobalErro
using static NAVIGEST.Android.GlobalErro;

namespace NAVIGEST.Android.Pages
{
    public class MainYahPageViewModel : INotifyPropertyChanged
    {
        private bool _isSidebarExpanded = true;
        private double _sidebarWidth = 260;
    private bool _isConfigExpanded = false;
    private bool _isSidebarVisible;
    private bool _isAdminUnlocked;
    private string _userName = string.Empty;
    private string _userRole = string.Empty;
    private ImageSource _userPhoto = "user_placeholder.png";
    private ImageSource _companyLogo = "yahcores.png";
    private string _companyName = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        // User info (vindo de UserSession)
        public string UserName
        {
            get => _userName;
            private set
            {
                if (_userName != value)
                {
                    _userName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string UserRole
        {
            get => _userRole;
            private set
            {
                if (_userRole != value)
                {
                    _userRole = value;
                    OnPropertyChanged();
                }
            }
        }

        public ImageSource UserPhoto
        {
            get => _userPhoto;
            private set
            {
                if (!Equals(_userPhoto, value))
                {
                    _userPhoto = value;
                    OnPropertyChanged();
                }
            }
        }

        public ImageSource CompanyLogo
        {
            get => _companyLogo;
            private set
            {
                if (!Equals(_companyLogo, value))
                {
                    _companyLogo = value;
                    OnPropertyChanged();
                }
            }
        }

        public string CompanyName
        {
            get => _companyName;
            private set
            {
                if (_companyName != value)
                {
                    _companyName = value;
                    OnPropertyChanged();
                }
            }
        }

        public MainYahPageViewModel()
        {
            try
            {
                RefreshUserContext();
                if (Application.Current is Application app)
                {
                    app.RequestedThemeChanged += (_, __) =>
                    {
                        try { CompanyLogo = BuildCompanyLogoSource(UserSession.Current.User.CompanyLogo); }
                        catch (Exception ex) { TratarErro(ex); }
                    };
                }
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

        public void RefreshUserContext()
        {
            try
            {
                var sessionUser = UserSession.Current.User ?? new UserSession.UserData();

                if (string.IsNullOrWhiteSpace(sessionUser.CompanyName))
                {
                    sessionUser.CompanyName = Preferences.Default.Get("company.name", string.Empty) ?? string.Empty;
                }

                if (sessionUser.CompanyLogo is null || sessionUser.CompanyLogo.Length == 0)
                {
                    var logoBase64 = Preferences.Default.Get("company.logo", string.Empty) ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(logoBase64))
                    {
                        try
                        {
                            sessionUser.CompanyLogo = Convert.FromBase64String(logoBase64);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[Android/MainYahPageViewModel] Invalid company logo base64: {ex.Message}");
                        }
                    }
                }

                UserSession.Current.User = sessionUser;

                UserName = sessionUser.Name;
                UserRole = sessionUser.Role;
                UserPhoto = sessionUser.Photo is { Length: > 0 }
                    ? ImageSource.FromStream(() => new MemoryStream(sessionUser.Photo))
                    : "user_placeholder.png";
                CompanyLogo = BuildCompanyLogoSource(sessionUser.CompanyLogo);
                CompanyName = string.IsNullOrWhiteSpace(sessionUser.CompanyName) ? string.Empty : sessionUser.CompanyName;
                IsAdminUnlocked = string.Equals(sessionUser.Role, "ADMIN", StringComparison.OrdinalIgnoreCase);

                Debug.WriteLine($"[Android/MainYahPageViewModel] Session refreshed. User='{UserName}', Role='{UserRole}', Company='{CompanyName}', LogoBytes={(sessionUser.CompanyLogo?.Length ?? 0)}");
            }
            catch (Exception ex) { TratarErro(ex); }
        }

        private static ImageSource BuildCompanyLogoSource(byte[]? logoBytes)
        {
            if (logoBytes is { Length: > 0 })
                return ImageSource.FromStream(() => new MemoryStream(logoBytes));

            var theme = Application.Current?.RequestedTheme ?? AppTheme.Light;
            return theme == AppTheme.Dark ? "yahcorbranco.png" : "yahcores.png";
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
