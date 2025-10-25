// File: NAVIGEST.iOS/Pages/ProductsPage.xaml.cs
#nullable enable
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using NAVIGEST.iOS.PageModels;
using NAVIGEST.iOS.Models;
using NAVIGEST.iOS;

namespace NAVIGEST.iOS.Pages
{
    public partial class ProductsPage : ContentPage
    {
        private bool _loadedOnce;
        private bool _isEditMode;

        public ProductsPage() : this(new ProductsPageModel()) { }

        public ProductsPage(ProductsPageModel vm)
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
            if (BindingContext is ProductsPageModel vm)
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

            FormTitle.Text = isNew ? "Novo Product" : "Editar Product";
            DeleteFormButton.IsVisible = !isNew;
            SaveButton.Text = isNew ? "Adicionar" : "Atualizar";
        }

        // --- SWIPE BUTTONS ---

        private async void OnEditButtonClicked(object sender, EventArgs e)
        {
            await CloseSwipeFrom(sender);

            var cliente = (sender as Button)?.CommandParameter as Product
                          ?? (sender as Element)?.BindingContext as Product;
            if (cliente == null)
            {
                await DisplayAlert("Erro", "Product não identificado.", "OK");
                return;
            }

            if (BindingContext is ProductsPageModel vm &&
                vm.SelectCommand?.CanExecute(cliente) == true)
            {
                vm.SelectCommand.Execute(cliente);
            }

            ShowFormView(isNew: false);
        }

        private async void OnDeleteButtonClicked(object sender, EventArgs e)
        {
            await DisplayAlert("[DBG]", "DELETE CLICKED", "OK"); // prova de clique
            await CloseSwipeFrom(sender);

            var cliente = (sender as Button)?.CommandParameter as Product
                          ?? (sender as Element)?.BindingContext as Product;
            if (cliente == null)
            {
                await DisplayAlert("Erro", "Product não identificado.", "OK");
                return;
            }

            var confirm = await DisplayAlert("Eliminar Product",
                $"Tem a certeza que deseja eliminar '{cliente.PRODNOME}'?",
                "Eliminar", "Cancelar");

            if (!confirm) return;

            if (BindingContext is ProductsPageModel vm)
            {
                vm.SelectedProduct = cliente;

                bool canParam = vm.DeleteCommand?.CanExecute(cliente) == true;
                bool canNull  = vm.DeleteCommand?.CanExecute(null) == true;

                await DisplayAlert("[DBG]", $"CanExec(param)={canParam} | CanExec(null)={canNull}", "OK");

                if (canParam)
                {
                    vm.DeleteCommand!.Execute(cliente);
                    await GlobalToast.ShowAsync("Product eliminado com sucesso.", ToastTipo.Sucesso, 2000);
                }
                else if (canNull)
                {
                    vm.DeleteCommand!.Execute(null);
                    await GlobalToast.ShowAsync("Product eliminado com sucesso.", ToastTipo.Sucesso, 2000);
                }
                else
                {
                    await DisplayAlert("Aviso", "Não foi possível eliminar (command bloqueado).", "OK");
                }
            }
        }

        // Fecha o SwipeView pai antes de agir
        private async Task CloseSwipeFrom(object sender)
        {
            if (sender is Element el)
            {
                Element? p = el;
                while (p != null && p is not SwipeView) p = p.Parent;
                (p as SwipeView)?.Close();
            }
            await Task.Delay(75); // dá tempo à animação para não tapar os alerts
        }

        // Tap na célula – abre edição
        private void OnProductCellTapped(object sender, EventArgs e)
        {
            try
            {
                if (sender is Grid grid && grid.BindingContext is Product cliente)
                {
                    if (BindingContext is ProductsPageModel vm &&
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

        // SearchBar
        private void OnSearchBarSearchButtonPressed(object sender, EventArgs e) => SearchBar.Unfocus();
        private void OnSearchBarTextChanged(object sender, TextChangedEventArgs e) { }
        private void OnCollectionViewScrolled(object sender, ItemsViewScrolledEventArgs e) => SearchBar.Unfocus();

        // FAB
        private void OnAddProductTapped(object sender, EventArgs e)
        {
            try
            {
                if (BindingContext is ProductsPageModel vm &&
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
            if (BindingContext is ProductsPageModel vm &&
                vm.ClearCommand?.CanExecute(null) == true)
            {
                vm.ClearCommand.Execute(null);
            }
            ShowListView();
        }

        // Guardar
        private async void OnSaveProductTapped(object sender, EventArgs e)
        {
            try
            {
                if (BindingContext is not ProductsPageModel vm || vm.Editing is null) return;

                if (string.IsNullOrWhiteSpace(vm.Editing.PRODNOME))
                {
                    await DisplayAlert("Aviso", "O nome do cliente é obrigatório.", "OK");
                    return;
                }

                bool isNew = string.IsNullOrWhiteSpace(vm.Editing.PRODCODIGO);

                if (isNew)
                    await GlobalToast.ShowAsync("Product adicionado com sucesso! (Pasta a criar)", ToastTipo.Sucesso, 2000);
                else
                    await GlobalToast.ShowAsync("Product atualizado com sucesso!", ToastTipo.Sucesso, 2000);

                ShowListView();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erro", $"Erro ao guardar: {ex.Message}", "OK");
                GlobalErro.TratarErro(ex, mostrarAlerta: false);
            }
        }

        // Eliminar no Form
        private async void OnDeleteFromFormTapped(object sender, EventArgs e)
        {
            try
            {
                if (BindingContext is not ProductsPageModel vm || vm.Editing is null)
                {
                    await DisplayAlert("Erro", "Não foi possível identificar o cliente.", "OK");
                    return;
                }

                var cliente = vm.Editing;
                var confirm = await DisplayAlert(
                    "Eliminar Product",
                    $"Tem a certeza que deseja eliminar '{cliente.PRODNOME}'?",
                    "Eliminar", "Cancelar");

                if (confirm)
                {
                    vm.SelectedProduct = cliente;
                    if (vm.DeleteCommand?.CanExecute(null) == true)
                    {
                        vm.DeleteCommand.Execute(null);
                        await GlobalToast.ShowAsync("Product eliminado com sucesso.", ToastTipo.Sucesso, 2000);
                        ShowListView();
                    }
                    else
                    {
                        await DisplayAlert("Aviso", "DeleteCommand não executável (form).", "OK");
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
