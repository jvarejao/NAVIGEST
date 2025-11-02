#if ANDROID
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using Android.Util;

namespace NAVIGEST.Android.Pages;

public partial class WelcomePage
{
    private const string LogTag = "AppLifecycle";

    // Código Android específico: navegação após escolha da empresa
    private async Task NavigateToLoginPageAndroidAsync()
    {
        try
        {
            Log.Debug(LogTag, "NavigateToLoginPageAndroidAsync started");
            
            await ShowMainContentAsync();
            await Task.Delay(50); // animação mínima
            
            Log.Debug(LogTag, "Before navigation to Login");
            
            if (MainThread.IsMainThread)
                await Shell.Current.GoToAsync("Login"); // Rota relativa
            else
                await MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync("Login"));
            
            Log.Debug(LogTag, "Navigation to Login completed");
        }
        catch (Exception ex)
        {
            Log.Error(LogTag, $"NavigateToLoginPageAndroidAsync failed: {ex.Message}");
            GlobalErro.TratarErro(ex);
            await ShowToastAsync("Erro ao navegar para Login.", false, 2000);
        }
    }
}
#endif
