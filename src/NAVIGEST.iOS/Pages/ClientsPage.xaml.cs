// File: NAVIGEST.iOS/Pages/ClientsPage.xaml.cs
#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using NAVIGEST.iOS.PageModels;
using NAVIGEST.iOS.Models;
using NAVIGEST.iOS; // GlobalToast / GlobalErro

namespace NAVIGEST.iOS.Pages
{
    public partial class ClientsPage : ContentPage
    {
        private bool _loadedOnce;

        public ClientsPage() : this(new ClientsPageModel()) { }

        public ClientsPage(ClientsPageModel vm)
        {
            BindingContext = vm;
            InitializeComponent();
            Dispatcher.Dispatch(async () => await EnsureLoadedAsync());
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await EnsureLoadedAsync();
        }

        private async Task EnsureLoadedAsync()
        {
            if (_loadedOnce) return;
            if (BindingContext is ClientsPageModel vm)
            {
                try { await vm.LoadAsync(force: true); }
                catch (Exception ex)
                {
                    GlobalErro.TratarErro(ex, mostrarAlerta: false);
                    await GlobalToast.ShowAsync("Falha ao carregar clientes.", ToastTipo.Erro, 2500);
                }
                _loadedOnce = true;
            }
        }

        // Seleção na lista principal
        private async void OnClientSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (BindingContext is ClientsPageModel vm && e.CurrentSelection?.FirstOrDefault() is Cliente cliente)
                {
                    // Selecionar cliente no ViewModel
                    if (vm.SelectCommand?.CanExecute(cliente) == true)
                    {
                        vm.SelectCommand.Execute(cliente);
                    }
                    
                    // Navegar para RegisterPage
                    await Navigation.PushAsync(new RegisterPage());
                }
            }
            catch (Exception ex) { GlobalErro.TratarErro(ex, mostrarAlerta: false); }
        }

        // Swipe Action: Editar
        private async void OnEditClientTapped(object sender, EventArgs e)
        {
            try
            {
                if (sender is Grid grid && grid.BindingContext is Cliente cliente)
                {
                    if (BindingContext is ClientsPageModel vm && vm.SelectCommand?.CanExecute(cliente) == true)
                    {
                        vm.SelectCommand.Execute(cliente);
                    }
                    await Navigation.PushAsync(new RegisterPage());
                }
            }
            catch (Exception ex)
            {
                GlobalErro.TratarErro(ex, mostrarAlerta: true);
            }
        }

        // Swipe Action: Pastas do Cliente
        private async void OnPastasClientTapped(object sender, EventArgs e)
        {
            try
            {
                if (sender is Grid grid && grid.BindingContext is Cliente cliente)
                {
                    if (BindingContext is ClientsPageModel vm)
                    {
                        // Selecionar o cliente primeiro
                        if (vm.SelectCommand?.CanExecute(cliente) == true)
                        {
                            vm.SelectCommand.Execute(cliente);
                        }
                        
                        // Executar comando Pastas
                        if (vm.PastasCommand?.CanExecute(null) == true)
                        {
                            vm.PastasCommand.Execute(null);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                GlobalErro.TratarErro(ex, mostrarAlerta: true);
            }
        }

        // Swipe Action: Eliminar
        private async void OnDeleteClientTapped(object sender, EventArgs e)
        {
            try
            {
                if (sender is Grid grid && grid.BindingContext is Cliente cliente)
                {
                    var confirm = await DisplayAlert(
                        "Eliminar Cliente",
                        $"Tem a certeza que deseja eliminar '{cliente.CLINOME}'?",
                        "Eliminar",
                        "Cancelar"
                    );

                    if (confirm && BindingContext is ClientsPageModel vm)
                    {
                        if (vm.DeleteCommand?.CanExecute(cliente) == true)
                        {
                            vm.DeleteCommand.Execute(cliente);
                            await GlobalToast.ShowAsync("Cliente eliminado com sucesso.", ToastTipo.Sucesso, 2000);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                GlobalErro.TratarErro(ex, mostrarAlerta: true);
            }
        }

        // Floating Action Button: Adicionar novo cliente
        private async void OnAddClientTapped(object sender, EventArgs e)
        {
            try
            {
                if (BindingContext is ClientsPageModel vm)
                {
                    if (vm.NewCommand?.CanExecute(null) == true)
                    {
                        vm.NewCommand.Execute(null);
                    }
                    await Navigation.PushAsync(new RegisterPage());
                }
            }
            catch (Exception ex)
            {
                GlobalErro.TratarErro(ex, mostrarAlerta: true);
            }
        }
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
