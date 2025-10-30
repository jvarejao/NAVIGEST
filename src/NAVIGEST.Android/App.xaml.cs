using NAVIGEST.Android.Resources.Styles;
using Microsoft.Maui.Storage;
#if ANDROID
using Android.Util;
#endif

namespace NAVIGEST.Android
{
    public partial class App : Application
    {
        private const string ThemePrefKey = "user_theme"; // valores: "Light" | "Dark" | "Auto"
        private bool _autoMode;

        public bool IsAutoTheme => _autoMode;
#if ANDROID
        private const string LogTag = "AppLifecycle";
#endif

        public App()
        {
            Console.WriteLine("[App] Console before InitializeComponent");
#if ANDROID
            Log.Info(LogTag, "ctor entered - before InitializeComponent");
#endif
            InitializeComponent();
            System.Diagnostics.Debug.WriteLine("[App] Inicializando App...");
            Console.WriteLine("[App] Console ctor hit");
#if ANDROID
            Log.Info(LogTag, "ctor after InitializeComponent");
#endif

            var stored = Preferences.Get(ThemePrefKey, string.Empty);
            switch (stored)
            {
                case "Dark":
                    _autoMode = false;
                    SetTheme(AppTheme.Dark, persist: false);
                    break;
                case "Light":
                    _autoMode = false;
                    SetTheme(AppTheme.Light, persist: false);
                    break;
                case "Auto":
                    EnableAutoTheme(persist: false);
                    break;
                default:
                    _autoMode = false;
                    SetTheme(AppTheme.Light, persist: false);
                    break;
            }
#if ANDROID
            Log.Info(LogTag, "ctor finished theme setup");
#endif
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
#if ANDROID
            Log.Info(LogTag, "CreateWindow invoked");
#endif
            var shell = new AppShell();
#if ANDROID
            Log.Info(LogTag, "CreateWindow returning AppShell");
#endif
            return new Window(shell);
        }

        public void EnableAutoTheme(bool persist = true)
        {
            if (_autoMode) { if (persist) SaveTheme("Auto"); return; }
            _autoMode = true;
            Application.Current!.RequestedThemeChanged += OnRequestedThemeChanged;
            ApplySystemTheme();
            if (persist) SaveTheme("Auto");
        }

        public void DisableAutoTheme()
        {
            if (!_autoMode) return;
            _autoMode = false;
            Application.Current!.RequestedThemeChanged -= OnRequestedThemeChanged;
        }

        private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e)
        {
            if (_autoMode)
                ApplySystemTheme();
        }

        private void ApplySystemTheme()
        {
            var sys = Application.Current?.RequestedTheme ?? AppTheme.Light;
            // Aplica real mas não persiste (Auto controla)
            SetTheme(sys, persist: false);
        }

        public void SetTheme(AppTheme theme, bool persist = true)
        {
            // Se for chamado manualmente, saímos do modo auto (a UI pode voltar a ativar se necessário)
            if (!_autoMode && UserAppTheme == theme && !ResourcesDirty(theme))
            {
                if (persist) SaveTheme(theme == AppTheme.Dark ? "Dark" : "Light");
                return;
            }

            if (_autoMode)
            {
                // Em modo auto, ignorar persist (persistido como Auto) e aplicar base actual
                if (UserAppTheme == theme && !ResourcesDirty(theme)) return;
            }

            UserAppTheme = theme;

            var merged = Resources.MergedDictionaries;
            // Remove apenas o dicionário de cores, preservando DesignSystem e Buttons
            var toRemove = merged.Where(d => d is Colors_Light || d is Colors_Dark).ToList();
            foreach (var d in toRemove)
                merged.Remove(d);

            // Adiciona o dicionário correto de cores
            if (theme == AppTheme.Dark)
                merged.Add(new Colors_Dark());
            else
                merged.Add(new Colors_Light());

            if (persist && !_autoMode)
                SaveTheme(theme == AppTheme.Dark ? "Dark" : "Light");
        }

        private static void SaveTheme(string value)
        {
            try { Preferences.Set(ThemePrefKey, value); } catch { }
        }

        private bool ResourcesDirty(AppTheme theme)
        {
            bool hasDark = Resources.MergedDictionaries.Any(d => d is Colors_Dark);
            bool hasLight = Resources.MergedDictionaries.Any(d => d is Colors_Light);
            return (theme == AppTheme.Dark && !hasDark) || (theme == AppTheme.Light && !hasLight);
        }
    }
}
