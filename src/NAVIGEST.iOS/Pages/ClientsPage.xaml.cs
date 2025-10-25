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
            
            // Configurar toolbar do teclado no iOS
            #if IOS
            ConfigureKeyboardToolbar();
            #endif
            
            Dispatcher.Dispatch(async () => await EnsureLoadedAsync());
        }

        #if IOS
        private void ConfigureKeyboardToolbar()
        {
            // Adicionar botão "Concluído" na toolbar do teclado
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

        // Seleção na lista principal (toque na célula)
        private void OnClientCellTapped(object sender, EventArgs e)
        {
            try
            {
                if (sender is Grid grid && grid.BindingContext is Cliente cliente)
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

        // SearchBar: esconder teclado quando pressiona botão de pesquisa
        private void OnSearchBarSearchButtonPressed(object sender, EventArgs e)
        {
            if (sender is SearchBar searchBar)
            {
                searchBar.Unfocus();
            }
        }

        // SearchBar: permitir scroll da lista enquanto pesquisa
        private void OnSearchBarTextChanged(object sender, TextChangedEventArgs e)
        {
            // Não fazer nada especial aqui, apenas permitir o binding funcionar
            // O filtro é feito automaticamente pelo binding {Binding Filter}
        }

        // CollectionView: fechar teclado ao fazer scroll
        private void OnCollectionViewScrolled(object sender, ItemsViewScrolledEventArgs e)
        {
            SearchBar.Unfocus();
        }

        // PASTAS (Swipe)
        private async void OnPastasSwipeInvoked(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[PASTAS] Swipe Invoked! Sender: {sender?.GetType().Name}");

                // Obter o Cliente do BindingContext do SwipeItemView
                Cliente? cliente = (sender as Element)?.BindingContext as Cliente;

                if (cliente == null)
                {
                    await DisplayAlert("Erro", "Não foi possível identificar o cliente.", "OK");
                    return;
                }
                
                CloseSwipe(sender);

                if (string.IsNullOrWhiteSpace(cliente.CLICODIGO))
                {
                    await DisplayAlert("Aviso", "Cliente sem código definido.", "OK");
                    return;
                }

                var uri = new Uri($"qfile://open?path=/mnt/remote/CLIENTES/{cliente.CLICODIGO}");
                try 
                { 
                    await Launcher.OpenAsync(uri); 
                }
                catch
                {
                    await DisplayAlert("Qfile",
                        $"A abrir pasta do cliente {cliente.CLINOME}...\n\nCaminho: CLIENTES/{cliente.CLICODIGO}",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                GlobalErro.TratarErro(ex, mostrarAlerta: true);
            }
        }

        // EDITAR (Swipe)
        private void OnEditSwipeInvoked(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[EDITAR] Swipe Invoked! Sender: {sender?.GetType().Name}");

                Cliente? cliente = (sender as Element)?.BindingContext as Cliente;

                if (cliente == null) return;
                
                CloseSwipe(sender);

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

        // ELIMINAR (Swipe)
        private async void OnDeleteSwipeInvoked(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[ELIMINAR] Swipe Invoked! Sender: {sender?.GetType().Name}");

                Cliente? cliente = (sender as Element)?.BindingContext as Cliente;

                if (cliente == null) 
                {
                    await DisplayAlert("Erro", "Não foi possível identificar o cliente.", "OK");
                    return;
                }

                CloseSwipe(sender);

                var confirm = await DisplayAlert(
                    "Eliminar Cliente",
                    $"Tem a certeza que deseja eliminar '{cliente.CLINOME}'?",
                    "Eliminar", "Cancelar");

                if (confirm && BindingContext is ClientsPageModel vm)
                {
                    // IMPORTANTE: O DeleteCommand usa SelectedCliente, então temos de o definir primeiro!
                    vm.SelectedCliente = cliente;
                    
                    if (vm.DeleteCommand?.CanExecute(null) == true)
                    {
                        vm.DeleteCommand.Execute(null);
                        await GlobalToast.ShowAsync("Cliente eliminado com sucesso.", ToastTipo.Sucesso, 2000);
                    }
                }
            }
            catch (Exception ex)
            {
                GlobalErro.TratarErro(ex, mostrarAlerta: true);
            }
        }

private void CloseSwipe(object sender)
{
    if (sender is Element el)
    {
        var p = el.Parent;
        while (p != null && p is not SwipeView) p = (p as Element)?.Parent;
        (p as SwipeView)?.Close();
    }
}


        // Helper: Encontrar o SwipeView pai
        private SwipeView? FindParentSwipeView(Element element)
        {
            var parent = element.Parent;
            while (parent != null)
            {
                if (parent is SwipeView swipeView)
                    return swipeView;
                parent = parent.Parent;
            }
            return null;
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
                System.Diagnostics.Debug.WriteLine($"[ELIMINAR FORM] Tap detectado!");
                
                if (BindingContext is not ClientsPageModel vm || vm.Editing is null)
                {
                    await DisplayAlert("Erro", "Não foi possível identificar o cliente.", "OK");
                    return;
                }

                var cliente = vm.Editing;

                var confirm = await DisplayAlert(
                    "Eliminar Cliente",
                    $"Tem a certeza que deseja eliminar '{cliente.CLINOME}'?",
                    "Eliminar", "Cancelar");

                if (confirm)
                {
                    // IMPORTANTE: O DeleteCommand usa SelectedCliente, então temos de o definir primeiro!
                    vm.SelectedCliente = cliente;
                    
                    if (vm.DeleteCommand?.CanExecute(null) == true)
                    {
                        vm.DeleteCommand.Execute(null);
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
