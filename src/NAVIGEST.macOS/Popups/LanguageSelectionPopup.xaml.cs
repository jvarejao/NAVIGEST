using CommunityToolkit.Maui.Views;
using System.Globalization;

namespace NAVIGEST.macOS.Popups;

public partial class LanguageSelectionPopup : Popup
{
    public LanguageSelectionPopup()
    {
        InitializeComponent();
    }

    private void OnPTClicked(object sender, EventArgs e) => SetLanguage("pt-PT");
    private void OnENClicked(object sender, EventArgs e) => SetLanguage("en-US");
    private void OnFRClicked(object sender, EventArgs e) => SetLanguage("fr-FR");
    private void OnESClicked(object sender, EventArgs e) => SetLanguage("es-ES");

    private void SetLanguage(string cultureCode)
    {
        var culture = new CultureInfo(cultureCode);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        
        // Save preference
        Preferences.Set("SelectedLanguage", cultureCode);

        Close(cultureCode);
    }

    private void OnCancelClicked(object sender, EventArgs e)
    {
        Close(null);
    }
}
