// File: NAVIGEST.iOS/Pages/ProductsPage.xaml.cs
#nullable enable
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using CommunityToolkit.Maui.Views;
using NAVIGEST.iOS.PageModels;
using NAVIGEST.iOS.Models;
using NAVIGEST.iOS;
using NAVIGEST.iOS.Popups;

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

        private static Page? GetRootPage()
        {
            var app = Application.Current;
            var window = app?.Windows?.FirstOrDefault();
            var root = window?.Page;

            return root switch
            {
                Shell shell when shell.CurrentPage is Page current => current,
                Shell shell                                            => shell,
                NavigationPage nav when nav.CurrentPage is not null   => nav.CurrentPage,
                _                                                     => root
            };
        }

        private static Task ShowAlertAsync(string title, string message, string cancel = "OK") =>
            MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var root = GetRootPage();
                if (root is null)
                    return;

                await root.DisplayAlert(title, message, cancel);
            });

        private static Task<bool> ShowConfirmAsync(string title, string message, string accept, string cancel) =>
            MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var root = GetRootPage();
                if (root is null)
                    return false;

                return await root.DisplayAlert(title, message, accept, cancel);
            });

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
                    await GlobalToast.ShowAsync("Falha ao carregar produtos.", ToastTipo.Erro, 2500);
                }
                _loadedOnce = true;
            }
        }

        private void ShowListView()
        {
            UnfocusFormFields();
            ListViewContainer.IsVisible = true;
            FormViewContainer.IsVisible = false;
            _isEditMode = false;
        }

        private void ShowFormView(bool isNew)
        {
            ListViewContainer.IsVisible = false;
            FormViewContainer.IsVisible = true;
            _isEditMode = true;

            FormTitle.Text = isNew ? "Novo Produto" : "Editar Produto";
            DeleteFormButton.IsVisible = !isNew;
            SaveButton.Text = "Guardar";
        }

        // --- SWIPE BUTTONS ---

        private async void OnEditSwipeInvoked(object sender, EventArgs e)
        {
            await CloseSwipeFrom(sender);

            var product = ResolveProductFrom(sender);
            if (product == null)
            {
                await ShowAlertAsync("Erro", "Produto não identificado.");
                return;
            }

            if (BindingContext is ProductsPageModel vm &&
                vm.SelectCommand?.CanExecute(product) == true)
            {
                vm.SelectCommand.Execute(product);
            }

            ShowFormView(isNew: false);
        }

        private async void OnDeleteSwipeInvoked(object sender, EventArgs e)
        {
            await CloseSwipeFrom(sender);

            var product = ResolveProductFrom(sender);
            if (product == null)
            {
                await ShowAlertAsync("Erro", "Produto não identificado.");
                return;
            }

            var confirm = await ShowConfirmAsync("Eliminar produto",
                $"Tem a certeza que deseja eliminar '{product.Descricao}'?",
                "Eliminar", "Cancelar");

            if (!confirm) return;

            if (BindingContext is ProductsPageModel vm)
            {
                vm.SelectedProduct = product;
                var deleted = await vm.DeleteAsync(product);
                if (deleted)
                    ShowListView();
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

        private static Product? ResolveProductFrom(object sender)
        {
            if (sender is SwipeItemView siv)
            {
                if (siv.CommandParameter is Product productFromCommand)
                    return productFromCommand;

                if (siv.BindingContext is Product productFromBinding)
                    return productFromBinding;
            }

            if (sender is Element el)
            {
                if (el.BindingContext is Product productFromElement)
                    return productFromElement;
            }

            return null;
        }

        // Tap na célula – abre edição
        private void OnProductCellTapped(object sender, EventArgs e)
        {
            try
            {
                if (sender is Grid grid && grid.BindingContext is Product product)
                {
                    if (BindingContext is ProductsPageModel vm &&
                        vm.SelectCommand?.CanExecute(product) == true)
                    {
                        vm.SelectCommand.Execute(product);
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

    private void OnFormBackgroundTapped(object sender, TappedEventArgs e) => UnfocusFormFields();

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

        private async void OnAddFamiliaTapped(object sender, EventArgs e)
        {
            try
            {
                if (BindingContext is not ProductsPageModel vm)
                    return;

                var popup = new AddProductFamilyPopup();
                var result = await AppShell.Current.ShowPopupAsync(popup);

                if (result is ProductFamilyInput familia && !string.IsNullOrWhiteSpace(familia.Codigo))
                {
                    var (ok, message, option) = await vm.AddFamilyAsync(familia.Codigo, familia.Descricao);
                    if (!ok)
                    {
                        if (!string.IsNullOrWhiteSpace(message))
                            await ShowAlertAsync("Família", message);

                        if (popup.FamiliesDirty)
                        {
                            await vm.ReloadFamiliesAsync();
                            FamiliaPicker.SelectedItem = vm.SelectedFamily;
                        }
                    }
                    else
                    {
                        if (popup.FamiliesDirty)
                            await vm.ReloadFamiliesAsync(option?.Codigo ?? familia.Codigo);

                        var selectedOption = popup.FamiliesDirty ? vm.SelectedFamily : option;
                        if (selectedOption is not null)
                        {
                            vm.SelectedFamily = selectedOption;
                            FamiliaPicker.SelectedItem = selectedOption;
                        }
                    }
                }
                else if (popup.FamiliesDirty)
                {
                    await vm.ReloadFamiliesAsync();
                    FamiliaPicker.SelectedItem = vm.SelectedFamily;
                }
            }
            catch (Exception ex)
            {
                GlobalErro.TratarErro(ex, mostrarAlerta: true);
            }
        }

        // Cancelar
        private void OnCancelEditTapped(object sender, EventArgs e)
        {
            UnfocusFormFields();
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
                if (BindingContext is not ProductsPageModel vm)
                    return;

                var saved = await vm.SaveAsync();
                if (saved)
                    ShowListView();
            }
            catch (Exception ex)
            {
                await ShowAlertAsync("Erro", $"Erro ao guardar: {ex.Message}");
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
                    await ShowAlertAsync("Erro", "Não foi possível identificar o produto.");
                    return;
                }

                var product = vm.Editing;
                var confirm = await ShowConfirmAsync(
                    "Eliminar produto",
                    $"Tem a certeza que deseja eliminar '{product.Descricao}'?",
                    "Eliminar", "Cancelar");

                if (!confirm)
                    return;

                vm.SelectedProduct = product;
                var deleted = await vm.DeleteAsync(product);
                if (deleted)
                    ShowListView();
            }
            catch (Exception ex)
            {
                await ShowAlertAsync("Erro", $"Erro ao eliminar: {ex.Message}");
                GlobalErro.TratarErro(ex, mostrarAlerta: false);
            }
        }

        private void OnPrecoCustoFocused(object sender, FocusEventArgs e)
        {
            if (sender is not Entry entry)
                return;

            var text = entry.Text ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text))
            {
                entry.Text = string.Empty;
                return;
            }

            var sanitized = text.Replace("€", string.Empty, StringComparison.OrdinalIgnoreCase)
                                 .Replace(" ", string.Empty, StringComparison.Ordinal);

            sanitized = sanitized.Replace(".", string.Empty, StringComparison.Ordinal);
            if (!sanitized.Contains(',', StringComparison.Ordinal) && sanitized.Contains('.', StringComparison.Ordinal))
                sanitized = sanitized.Replace('.', ',');

            if (!decimal.TryParse(sanitized.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                return;

            var culture = CultureInfo.GetCultureInfo("pt-PT");
            entry.Text = value.ToString("0.00", culture);

            entry.Dispatcher.Dispatch(() =>
            {
                var current = entry.Text ?? string.Empty;
                entry.CursorPosition = 0;
                entry.SelectionLength = current.Length;
            });
        }

        private void OnPrecoCustoUnfocused(object sender, FocusEventArgs e)
        {
            if (BindingContext is ProductsPageModel vm)
                vm.FormatValorOnBlur();
        }

        private void UnfocusFormFields()
        {
            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        CodigoEntry?.Unfocus();
                        DescricaoEntry?.Unfocus();
                        FamiliaPicker?.Unfocus();
                        ColaboradorEntry?.Unfocus();
                        PrecoCustoEntry?.Unfocus();
                    }
                    catch (Exception inner)
                    {
                        GlobalErro.TratarErro(inner, mostrarAlerta: false);
                    }
                });
            }
            catch (Exception ex)
            {
                GlobalErro.TratarErro(ex, mostrarAlerta: false);
            }
        }
    }
}
