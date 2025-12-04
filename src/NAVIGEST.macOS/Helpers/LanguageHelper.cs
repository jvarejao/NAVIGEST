using System.Globalization;

namespace NAVIGEST.macOS.Helpers;

public static class LanguageHelper
{
    public static (string Flag, string Code) GetCurrentLanguageInfo()
    {
        var current = CultureInfo.CurrentUICulture.Name;
        return current switch
        {
            "en-US" => ("🇺🇸", "EN"),
            "fr-FR" => ("🇫🇷", "FR"),
            "es-ES" => ("🇪🇸", "ES"),
            _ => ("🇵🇹", "PT") // Default to PT
        };
    }

    public static async Task ChangeLanguageAndRestart(string cultureCode)
    {
        Preferences.Set("SelectedLanguage", cultureCode);
        
        bool restart = await Application.Current.MainPage.DisplayAlert(
            "Reiniciar / Restart", 
            "A aplicação precisa de reiniciar para aplicar o novo idioma.\n\nThe application needs to restart to apply the new language.", 
            "Sim / Yes", 
            "Não / No");

        if (restart)
        {
            // "Soft" restart by reloading the main window content
            // This might not update x:Static resources that are already loaded in memory for singleton pages,
            // but it's the best we can do without a full process kill.
            // For a full process kill/restart, we'd need platform specific code.
            // Let's try to reload the AppShell.
            
            // Force culture update on current thread just in case
            var culture = new CultureInfo(cultureCode);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            Application.Current.Windows[0].Page = new AppShell();
        }
    }
}
