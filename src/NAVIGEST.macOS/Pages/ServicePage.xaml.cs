using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using NAVIGEST.macOS.PageModels; // ou ViewModels, conforme está o teu VM

namespace NAVIGEST.macOS.Pages
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
                        await NAVIGEST.macOS.AppShell.DisplayToastAsync("0 serviços carregados.");
                }
                else
                {
                    await DisplayAlert("Erro", "BindingContext não é ServicePageModel.", "OK");
                }
            }
            catch (Exception ex)
            {
                NAVIGEST.macOS.GlobalErro.TratarErro(ex, mostrarAlerta: false);
                await NAVIGEST.macOS.AppShell.DisplayToastAsync("Falha ao carregar serviços.");
            }
        }

#if WINDOWS
        // Código Windows específico (exemplo: animações, navegação, layouts)
#endif
#if ANDROID
        // Código Android específico (exemplo: animações, navegação, layouts)
#endif
#if IOS
        // Código iOS específico (exemplo: animações, navegação, layouts)
#endif
    }
}
