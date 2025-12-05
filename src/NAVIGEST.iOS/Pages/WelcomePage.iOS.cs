#if IOS
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
namespace NAVIGEST.iOS.Pages;
public partial class WelcomePage
{
    // C�digo iOS espec�fico: navega��o r�pida ap�s escolha da empresa
    private async Task NavigateToLoginPageiOSAsync()
    {
        try
        {
            await ShowMainContentAsync();
            await Task.Delay(50); // anima��o m�nima
            if (MainThread.IsMainThread)
                await Shell.Current.GoToAsync("Login"); // Corrigido: rota relativa
            else
                await MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync("Login"));
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex);
            await ShowToastAsync(NAVIGEST.Shared.Resources.Strings.AppResources.WelcomePage_LoginNavError, false, 2000);
        }
    }
}
#endif
