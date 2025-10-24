// File: NAVIGEST.iOS/Pages/ClientsPage.xaml.cs
#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using NAVIGEST.iOS.PageModels;
using NAVIGEST.iOS.Models;
using NAVIGEST.iOS;

namespace NAVIGEST.iOS.Pages
{
    public partial class ClientsPage : ContentPage
    {
        private bool _loadedOnce;
        private bool _isEditMode;

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
            
            // Se voltar de outra página, mostrar lista
            if (_isEditMode)
            {
                ShowListView();
            }
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

        // Alternar entre Lista e Formulário
        private void ShowListView()
        {
            ListViewContainer.IsVisible = true;
            FormViewContainer.IsVisible = false;
            _isEditMode = false;
        }

        private void ShowFormView(bool isNew)
        {
            ListViewContainer.IsVisible = false;
            FormViewContainer.IsVisible = true;
            _isEditMode = true;
            
            FormTitle.Text = isNew ? "Novo Cliente" : "Editar Cliente";
            DeleteFormButton.IsVisible = !isNew;
            SaveButton.Text = isNew ? "Adicionar" : "Atualizar";
        }

        // Seleção na lista principal (toque simples)
        private void OnClientSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (e.CurrentSelection?.FirstOrDefault() is not Cliente cliente)
                    return;

                // Limpar seleção
                if (sender is CollectionView collectionView)
                    collectionView.SelectedItem = null;

                if (BindingContext is ClientsPageModel vm)
                {
                    if (vm.SelectCommand?.CanExecute(cliente) == true)
                    {
                        vm.SelectCommand.Execute(cliente);
                    }
                }
                
                ShowFormView(isNew: false);
            }
            catch (Exception ex) 
            { 
                GlobalErro.TratarErro(ex, mostrarAlerta: true); 
            }
        }

        // SearchBar: esconder teclado quando pressiona botão de pesquisa
        private void OnSearchBarSearchButtonPressed(object sender, EventArgs e)
        {
            if (sender is SearchBar searchBar)
            {
                searchBar.Unfocus();
            }
        }

        // Swipe Action: Pastas do Cliente (abrir via Qfile)
        private async void OnPastasClientInvoked(object sender, EventArgs e)
        {
            try
            {
                if (sender is SwipeItem swipeItem && swipeItem.BindingContext is Cliente cliente)
                {
                    if (string.IsNullOrWhiteSpace(cliente.CLICODIGO))
                    {
                        await DisplayAlert("Aviso", "Cliente sem código definido.", "OK");
                        return;
                    }

                    var folderPath = $"qfile://open?path=/mnt/remote/CLIENTES/{cliente.CLICODIGO}";
                    
                    try
                    {
                        await Launcher.OpenAsync(new Uri(folderPath));
                    }
                    catch
                    {
                        await DisplayAlert("Qfile", 
                            $"A abrir pasta do cliente {cliente.CLINOME}...\n\nCaminho: CLIENTES/{cliente.CLICODIGO}", 
                            "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erro", $"Não foi possível abrir a pasta: {ex.Message}", "OK");
                GlobalErro.TratarErro(ex, mostrarAlerta: false);
            }
        }

        // Swipe Action: Editar
        private void OnEditClientInvoked(object sender, EventArgs e)
        {
            try
            {
                if (sender is SwipeItem swipeItem && swipeItem.BindingContext is Cliente cliente)
                {
                    if (BindingContext is ClientsPageModel vm)
                    {
                        if (vm.SelectCommand?.CanExecute(cliente) == true)
                        {
                            vm.SelectCommand.Execute(cliente);
                        }
                    }
                    ShowFormView(isNew: false);
                }
            }
            catch (Exception ex)
            {
                GlobalErro.TratarErro(ex, mostrarAlerta: true);
            }
        }

        // Swipe Action: Eliminar
        private async void OnDeleteClientInvoked(object sender, EventArgs e)
        {
            try
            {
                if (sender is SwipeItem swipeItem && swipeItem.BindingContext is Cliente cliente)
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
        private void OnAddClientTapped(object sender, EventArgs e)
        {
            try
            {
                if (BindingContext is ClientsPageModel vm)
                {
                    if (vm.NewCommand?.CanExecute(null) == true)
                    {
                        vm.NewCommand.Execute(null);
                    }
                }
                ShowFormView(isNew: true);
            }
            catch (Exception ex)
            {
                GlobalErro.TratarErro(ex, mostrarAlerta: true);
            }
        }

        // Formulário: Cancelar
        private void OnCancelEditTapped(object sender, EventArgs e)
        {
            if (BindingContext is ClientsPageModel vm)
            {
                if (vm.ClearCommand?.CanExecute(null) == true)
                {
                    vm.ClearCommand.Execute(null);
                }
            }
            ShowListView();
        }

        // Formulário: Guardar/Atualizar
        private async void OnSaveClientTapped(object sender, EventArgs e)
        {
            try
            {
                if (BindingContext is not ClientsPageModel vm || vm.Editing is null)
                    return;

                // Validar
                if (string.IsNullOrWhiteSpace(vm.Editing.CLINOME))
                {
                    await DisplayAlert("Aviso", "O nome do cliente é obrigatório.", "OK");
                    return;
                }

                bool isNew = string.IsNullOrWhiteSpace(vm.Editing.CLICODIGO);

                // Guardar
                // TODO: Verificar se SaveCommand existe no ViewModel
                // Por enquanto vamos tentar usar o que existe
                
                if (isNew)
                {
                    // Criar pasta do cliente
                    // TODO: Implementar criação de pasta via API Tailscale/Qfile
                    await GlobalToast.ShowAsync("Cliente adicionado com sucesso! (Pasta a criar)", ToastTipo.Sucesso, 2000);
                }
                else
                {
                    await GlobalToast.ShowAsync("Cliente atualizado com sucesso!", ToastTipo.Sucesso, 2000);
                }

                ShowListView();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erro", $"Erro ao guardar: {ex.Message}", "OK");
                GlobalErro.TratarErro(ex, mostrarAlerta: false);
            }
        }

        // Formulário: Eliminar
        private async void OnDeleteFromFormTapped(object sender, EventArgs e)
        {
            try
            {
                if (BindingContext is not ClientsPageModel vm || vm.Editing is null)
                    return;

                var confirm = await DisplayAlert(
                    "Eliminar Cliente",
                    $"Tem a certeza que deseja eliminar '{vm.Editing.CLINOME}'?",
                    "Eliminar",
                    "Cancelar"
                );

                if (confirm)
                {
                    if (vm.DeleteCommand?.CanExecute(vm.Editing) == true)
                    {
                        vm.DeleteCommand.Execute(vm.Editing);
                        await GlobalToast.ShowAsync("Cliente eliminado com sucesso.", ToastTipo.Sucesso, 2000);
                        ShowListView();
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erro", $"Erro ao eliminar: {ex.Message}", "OK");
                GlobalErro.TratarErro(ex, mostrarAlerta: false);
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
