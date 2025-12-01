#if MACCATALYST
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;

namespace NAVIGEST.macOS.Pages;

public partial class WelcomePage
{
    // Código macOS específico: navegação rápida após escolha da empresa
    private async Task NavigateToLoginPageMacOSAsync()
    {
        try
        {
            await ShowMainContentAsync();
            await Task.Delay(50); // animação mínima
            if (MainThread.IsMainThread)
                await Shell.Current.GoToAsync("//Login"); // Rota absoluta para ShellContent
            else
                await MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync("//Login"));
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex);
            await ShowToastAsync("Erro ao navegar para Login.", false, 2000);
        }
    }
}
#endif
