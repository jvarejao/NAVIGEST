using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using NAVIGEST.macOS.PageModels;

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
            
            // Forçar carregamento inicial caso o evento Loaded não dispare (ex: MainYahPage extrai o Content)
            Dispatcher.Dispatch(async () => await InitializeForHostAsync());
        }

        // Método chamado via Reflection pelo MainYahPage
        public async Task InitializeForHostAsync()
        {
            if (_loadedOnce) return;
            _loadedOnce = true;

            try
            {
                if (BindingContext is ServicePageModel m)
                {
                    await m.LoadAsync(force: true);
                }
            }
            catch (Exception ex)
            {
                NAVIGEST.macOS.GlobalErro.TratarErro(ex, mostrarAlerta: false);
            }
        }

        // <-- Handler exigido pelo XAML
        private async void OnPageLoaded(object sender, EventArgs e) => await InitializeForHostAsync();

        private void OnViewClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is Models.OrderInfoModel order && BindingContext is ServicePageModel vm)
            {
                vm.ViewCommand.Execute(order);
            }
        }

        private void OnEditClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is Models.OrderInfoModel order && BindingContext is ServicePageModel vm)
            {
                vm.EditCommand.Execute(order);
            }
        }

        private void OnDeleteClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is Models.OrderInfoModel order && BindingContext is ServicePageModel vm)
            {
                vm.DeleteCommand.Execute(order);
            }
        }
    }
}
