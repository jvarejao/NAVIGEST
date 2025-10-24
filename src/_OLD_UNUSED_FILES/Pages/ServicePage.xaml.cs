using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using AppLoginMaui.PageModels; // ou ViewModels, conforme est� o teu VM

namespace AppLoginMaui.Pages
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
                        await AppLoginMaui.AppShell.DisplayToastAsync("0 servi�os carregados.");
                }
                else
                {
                    await DisplayAlert("Erro", "BindingContext n�o � ServicePageModel.", "OK");
                }
            }
            catch (Exception ex)
            {
                AppLoginMaui.GlobalErro.TratarErro(ex, mostrarAlerta: false);
                await AppLoginMaui.AppShell.DisplayToastAsync("Falha ao carregar servi�os.");
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
