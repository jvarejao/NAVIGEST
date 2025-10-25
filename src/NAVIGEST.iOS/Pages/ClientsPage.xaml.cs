#nullable enable
using System;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using NAVIGEST.iOS.Models;
using NAVIGEST.iOS.PageModels;
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

            #if IOS
            ConfigureKeyboardToolbar();
            #endif

            Dispatcher.Dispatch(async () => await EnsureLoadedAsync());
        }

        #if IOS
        private void ConfigureKeyboardToolbar()
        {
            var toolbar = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = LayoutOptions.End,
                Padding = new Thickness(0, 5, 10, 5),
                BackgroundColor = Color.FromArgb("#F7F7F7")
            };

            var doneButton = new Button
            {
                Text = "Concluído",
                FontSize = 16,
                TextColor = Color.FromArgb("#007AFF"),
                BackgroundColor = Colors.Transparent,
                Padding = new Thickness(10, 5)
            };

            doneButton.Clicked += (s, e) => SearchBar.Unfocus();
            toolbar.Children.Add(doneButton);
        }
        #endif

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await EnsureLoadedAsync();
            if (_isEditMode) ShowListView();
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
            SaveButton.Text = isNew ? "Adicionar" : "Atualizar";
            
            // Mostrar grid de ações (Pastas + Eliminar) apenas em modo edição
            if (FindByName("ActionButtonsGrid") is View actionGrid)
            {
                actionGrid.IsVisible = !isNew;
            }
        }

        // Tap na célula – abre edição
        private void OnClientCellTapped(object sender, EventArgs e)
        {
            try
            {
                if (sender is Grid grid && grid.BindingContext is Cliente cliente)
                {
                    if (BindingContext is ClientsPageModel vm &&
                        vm.SelectCommand?.CanExecute(cliente) == true)
                    {
                        vm.SelectCommand.Execute(cliente);
                    }
                    ShowFormView(isNew: false);
                }
            }
            catch (Exception ex)
            {
                GlobalErro.TratarErro(ex, mostrarAlerta: true);
            }
        }

        private void OnSearchBarSearchButtonPressed(object sender, EventArgs e) => SearchBar.Unfocus();
        private void OnSearchBarTextChanged(object sender, TextChangedEventArgs e) { }
        private void OnCollectionViewScrolled(object sender, ItemsViewScrolledEventArgs e) => SearchBar.Unfocus();

        // ===========================
        // BUTTON HANDLERS (diretos dos buttons)
        // ===========================
        private void OnPastasButtonClicked(object sender, EventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.CommandParameter is Cliente cliente)
                {
                    _ = HandlePastasAsync(cliente);
                }
            }
            catch (Exception ex)
            {
                _ = DisplayAlert("Erro", $"Erro ao abrir pastas: {ex.Message}", "OK");
            }
        }

        private void OnEditButtonClicked(object sender, EventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.CommandParameter is Cliente cliente)
                {
                    HandleEditar(cliente);
                }
            }
            catch (Exception ex)
            {
                _ = DisplayAlert("Erro", $"Erro ao editar: {ex.Message}", "OK");
            }
        }

        private void OnDeleteButtonClicked(object sender, EventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.CommandParameter is Cliente cliente)
                {
                    _ = HandleEliminarAsync(cliente);
                }
            }
            catch (Exception ex)
            {
                _ = DisplayAlert("Erro", $"Erro ao eliminar: {ex.Message}", "OK");
            }
        }

        // SwipeItemView Invoked event handler
        private void OnSwipeItemViewEditInvoked(object sender, EventArgs e)
        {
            try
            {
                if (sender is SwipeItemView siv && siv.CommandParameter is Cliente cliente)
                {
                    if (BindingContext is ClientsPageModel vm)
                    {
                        // Garantir que SelectedCliente foi setado
                        vm.SelectedCliente = cliente;
                        
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            ShowFormView(isNew: false);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _ = DisplayAlert("Erro", $"Erro ao abrir edição: {ex.Message}", "OK");
            }
        }

        private void HandleEditar(Cliente cliente)
        {
            try
            {
                if (BindingContext is ClientsPageModel vm &&
                    vm.SelectCommand?.CanExecute(cliente) == true)
                {
                    vm.SelectCommand.Execute(cliente);
                }
                ShowFormView(isNew: false);
            }
            catch (Exception ex)
            {
                GlobalErro.TratarErro(ex, mostrarAlerta: true);
            }
        }

        private async Task HandleEliminarAsync(Cliente cliente)
        {
            try
            {
                var confirm = await DisplayAlert("Eliminar Cliente",
                    $"Tem a certeza que deseja eliminar '{cliente.CLINOME}'?",
                    "Eliminar", "Cancelar");

                if (!confirm) return;

                if (BindingContext is ClientsPageModel vm)
                {
                    vm.SelectedCliente = cliente;

                    bool canParam = vm.DeleteCommand?.CanExecute(cliente) == true;
                    bool canNull  = vm.DeleteCommand?.CanExecute(null) == true;

                    await DisplayAlert("[DBG]", $"CanExec(param)={canParam} | CanExec(null)={canNull}", "OK");

                    if (canParam)
                    {
                        vm.DeleteCommand!.Execute(cliente);
                        await GlobalToast.ShowAsync("Cliente eliminado com sucesso.", ToastTipo.Sucesso, 2000);
                    }
                    else if (canNull)
                    {
                        vm.DeleteCommand!.Execute(null);
                        await GlobalToast.ShowAsync("Cliente eliminado com sucesso.", ToastTipo.Sucesso, 2000);
                    }
                    else
                    {
                        await DisplayAlert("Aviso", "Não foi possível eliminar (command bloqueado).", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                GlobalErro.TratarErro(ex, mostrarAlerta: true);
            }
        }

        private async Task HandlePastasAsync(Cliente cliente)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cliente.CLICODIGO))
                {
                    await DisplayAlert("Aviso", "Cliente sem código definido.", "OK");
                    return;
                }

                var uri = new Uri($"qfile://open?path=/mnt/remote/CLIENTES/{cliente.CLICODIGO}");
                var can = await Launcher.CanOpenAsync(uri);
                await DisplayAlert("[DBG]", $"CanOpen qfile = {can}", "OK");

                if (can)
                {
                    await Launcher.OpenAsync(uri);
                    await DisplayAlert("[DBG]", "OpenAsync(qfile) OK", "OK");
                }
                else
                {
                    await DisplayAlert("Qfile",
                        $"A abrir pasta do cliente {cliente.CLINOME}...\n\nCaminho: CLIENTES/{cliente.CLICODIGO}",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erro", $"Falhou abrir Qfile: {ex.Message}", "OK");
            }
        }

        // FAB
        private void OnAddClientTapped(object sender, EventArgs e)
        {
            try
            {
                if (BindingContext is ClientsPageModel vm &&
                    vm.NewCommand?.CanExecute(null) == true)
                {
                    vm.NewCommand.Execute(null);
                }
                ShowFormView(isNew: true);
            }
            catch (Exception ex)
            {
                GlobalErro.TratarErro(ex, mostrarAlerta: true);
            }
        }

        // Cancelar
        private void OnCancelEditTapped(object sender, EventArgs e)
        {
            if (BindingContext is ClientsPageModel vm &&
                vm.ClearCommand?.CanExecute(null) == true)
            {
                vm.ClearCommand.Execute(null);
            }
            ShowListView();
        }

        // Guardar
        private async void OnSaveClientTapped(object sender, EventArgs e)
        {
            try
            {
                if (BindingContext is not ClientsPageModel vm || vm.Editing is null) return;

                if (string.IsNullOrWhiteSpace(vm.Editing.CLINOME))
                {
                    await DisplayAlert("Aviso", "O nome do cliente é obrigatório.", "OK");
                    return;
                }

                bool isNew = string.IsNullOrWhiteSpace(vm.Editing.CLICODIGO);

                if (isNew)
                    await GlobalToast.ShowAsync("Cliente adicionado com sucesso! (Pasta a criar)", ToastTipo.Sucesso, 2000);
                else
                    await GlobalToast.ShowAsync("Cliente atualizado com sucesso!", ToastTipo.Sucesso, 2000);

                ShowListView();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erro", $"Erro ao guardar: {ex.Message}", "OK");
                GlobalErro.TratarErro(ex, mostrarAlerta: false);
            }
        }

        // Pastas do formulário
        private async void OnPastasFormTapped(object sender, EventArgs e)
        {
            try
            {
                if (BindingContext is not ClientsPageModel vm || vm.Editing is null) return;

                // Chamar diretamente o OnPastasAsync
                vm.SelectedCliente = vm.Editing;
                await vm.OnPastasAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erro", $"Erro ao abrir pastas: {ex.Message}", "OK");
                GlobalErro.TratarErro(ex, mostrarAlerta: false);
            }
        }

        // Eliminar do formulário
        private async void OnDeleteFromFormTapped(object sender, EventArgs e)
        {
            try
            {
                if (BindingContext is not ClientsPageModel vm || vm.Editing is null) return;

                var confirm = await DisplayAlert("Eliminar Cliente",
                    $"Tem a certeza que deseja eliminar '{vm.Editing.CLINOME}'?",
                    "Eliminar", "Cancelar");

                if (!confirm) return;

                // Chamar diretamente o OnDeleteAsync (que já está no ViewModel)
                vm.SelectedCliente = vm.Editing;
                await vm.OnDeleteAsync();
                
                ShowListView();
                await GlobalToast.ShowAsync("Cliente eliminado com sucesso!", ToastTipo.Sucesso, 2000);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erro", $"Erro ao eliminar: {ex.Message}", "OK");
                GlobalErro.TratarErro(ex, mostrarAlerta: false);
            }
        }
    }
}
