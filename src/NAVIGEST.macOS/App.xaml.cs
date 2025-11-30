using NAVIGEST.macOS.Resources.Styles;
using Microsoft.Maui.Storage;

namespace NAVIGEST.macOS
{
    public partial class App : Application
    {
        private const string ThemePrefKey = "user_theme"; // valores: "Light" | "Dark" | "Auto"
        private bool _autoMode;

        public bool IsAutoTheme => _autoMode;

        public App()
        {
            InitializeComponent(); // Colors_Light já é carregado via App.xaml
            // MainPage = new AppShell(); // REMOVIDO para evitar conflito com CreateWindow

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
                    // fallback Light (já carregado no App.xaml)
                    _autoMode = false;
                    // Não precisa chamar SetTheme pois Light já está carregado
                    break;
            }
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());

            // Set initial size and constraints
            window.Width = 1280;
            window.Height = 900;
            window.MinimumWidth = 1024;
            window.MinimumHeight = 700;
            
            // Optional: Set maximum size if desired, but layout constraints should handle it
            // window.MaximumWidth = 1600; 

            return window;
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
            // Remove o dicionário de cores atual (identificamos pelo tipo)
            var toRemove = merged.Where(d => 
                d.GetType() == typeof(Resources.Styles.Colors_Light) || 
                d.GetType() == typeof(Resources.Styles.Colors_Dark)).ToList();
            foreach (var d in toRemove)
                merged.Remove(d);

            // Adiciona o dicionário correto de cores (instanciando diretamente a classe)
            ResourceDictionary colorDict = theme == AppTheme.Dark
                ? new Resources.Styles.Colors_Dark()
                : new Resources.Styles.Colors_Light();
            
            // Adiciona como primeiro item para que as cores estejam disponíveis para os outros estilos
            merged.Add(colorDict);

            if (persist && !_autoMode)
                SaveTheme(theme == AppTheme.Dark ? "Dark" : "Light");
        }

        private static void SaveTheme(string value)
        {
            try { Preferences.Set(ThemePrefKey, value); } catch { }
        }

        private bool ResourcesDirty(AppTheme theme)
        {
            // Verifica usando o tipo das classes de recursos
            bool hasDark = Resources.MergedDictionaries.Any(d => d.GetType() == typeof(Resources.Styles.Colors_Dark));
            bool hasLight = Resources.MergedDictionaries.Any(d => d.GetType() == typeof(Resources.Styles.Colors_Light));
            return (theme == AppTheme.Dark && !hasDark) || (theme == AppTheme.Light && !hasLight);
        }
    }
}