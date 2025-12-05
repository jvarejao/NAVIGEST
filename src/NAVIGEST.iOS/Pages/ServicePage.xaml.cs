using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using NAVIGEST.Shared.Resources.Strings;
using NAVIGEST.iOS.PageModels; // ou ViewModels, conforme est o teu VM

namespace NAVIGEST.iOS.Pages
{
    public partial class ServicePage : ContentPage
    {
        private bool _loadedOnce;

        public ServicePage() : this(new ServicePageModel()) { }

        public ServicePage(ServicePageModel vm)
        {
            InitializeComponent();
            BindingContext = vm ?? new ServicePageModel();
        }

        // <-- Handler exigido pelo XAML
        private async void OnPageLoaded(object sender, EventArgs e)
        {
            if (_loadedOnce) return;
            _loadedOnce = true;

            try
            {
                if (BindingContext is ServicePageModel m)
                {
                    await m.LoadAsync(force: true);
                    if (m.Orders.Count == 0)
                        await NAVIGEST.iOS.AppShell.DisplayToastAsync(AppResources.ServicePage_ZeroLoaded);
                }
                else
                {
                    await DisplayAlert(AppResources.Common_Error, AppResources.ServicePage_ErrorBinding, AppResources.Common_OK);
                }
            }
            catch (Exception ex)
            {
                NAVIGEST.iOS.GlobalErro.TratarErro(ex, mostrarAlerta: false);
                await NAVIGEST.iOS.AppShell.DisplayToastAsync(AppResources.ServicePage_LoadFailure);
            }
        }

#if WINDOWS
        // C�digo Windows espec�fico (exemplo: anima��es, navega��o, layouts)
#endif
#if ANDROID
        // C�digo Android espec�fico (exemplo: anima��es, navega��o, layouts)
#endif
#if IOS
        // C�digo iOS espec�fico (exemplo: anima��es, navega��o, layouts)
#endif
    }
}
