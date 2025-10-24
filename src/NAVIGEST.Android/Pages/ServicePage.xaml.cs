using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using NAVIGEST.Android.PageModels; // ou ViewModels, conforme est o teu VM

namespace NAVIGEST.Android.Pages
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
                        await NAVIGEST.Android.AppShell.DisplayToastAsync("0 servios carregados.");
                }
                else
                {
                    await DisplayAlert("Erro", "BindingContext no  ServicePageModel.", "OK");
                }
            }
            catch (Exception ex)
            {
                NAVIGEST.Android.GlobalErro.TratarErro(ex, mostrarAlerta: false);
                await NAVIGEST.Android.AppShell.DisplayToastAsync("Falha ao carregar servios.");
            }
        }

#if WINDOWS
        // Cdigo Windows especfico (exemplo: animaes, navegao, layouts)
#endif
#if ANDROID
        // Cdigo Android especfico (exemplo: animaes, navegao, layouts)
#endif
#if IOS
        // Cdigo iOS especfico (exemplo: animaes, navegao, layouts)
#endif
    }
}
