#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Graphics;
using NAVIGEST.iOS.Models;
using NAVIGEST.iOS.PageModels;
using NAVIGEST.iOS.Popups;
using NAVIGEST.iOS;
#if IOS
using Microsoft.Maui.Platform;
using UIKit;
#endif

namespace NAVIGEST.iOS.Pages
{
    public partial class ClientsPage : ContentPage
    {
        private bool _loadedOnce;
        private bool _isEditMode;
        private SwipeView? _activeSwipe;

        public ClientsPage() : this(new ClientsPageModel()) { }

        public ClientsPage(ClientsPageModel vm)
        {
            BindingContext = vm;
            InitializeComponent();

            SearchBar.HandlerChanged += OnSearchBarHandlerChanged;
            SearchBar.Loaded += OnSearchBarLoaded;

            #if IOS
            ConfigureKeyboardToolbar();
            #endif

            Dispatcher.Dispatch(async () => await EnsureLoadedAsync());
        }

        private void OnSearchBarHandlerChanged(object? sender, EventArgs e) => ConfigureSearchBarClearButton();
        private void OnSearchBarLoaded(object? sender, EventArgs e) => ConfigureSearchBarClearButton();

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
            UnfocusFormFields();
            HideDialCodePicker();
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
            var actionGrid = FindByName("ActionButtonsGrid") as View;
            if (actionGrid != null)
            {
                actionGrid.IsVisible = !isNew;
            }

            System.Diagnostics.Debug.WriteLine($"[FORM] isNew={isNew} externo={ (BindingContext as ClientsPageModel)?.Editing?.EXTERNO } anulado={(BindingContext as ClientsPageModel)?.Editing?.ANULADO }");
        }

        // Tap na célula – abre edição
        private void OnClientCellTapped(object sender, EventArgs e)
        {
            try
            {
                if (sender is Grid grid && grid.BindingContext is Cliente cliente)
                {
                    CloseParentSwipe(grid);

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

        private void OnFormBackgroundTapped(object sender, TappedEventArgs e) => UnfocusFormFields();

        // ===========================
        // SWIPE HANDLERS
        // ===========================
        private void OnSwipeEnded(object sender, SwipeEndedEventArgs e)
        {
            try
            {
                if (sender is not SwipeView swipe)
                    return;

                if (e.IsOpen)
                {
                    if (_activeSwipe is not null && _activeSwipe != swipe)
                    {
                        _activeSwipe.Close();
                    }
                    _activeSwipe = swipe;
                }
                else if (_activeSwipe == swipe)
                {
                    _activeSwipe = null;
                }
            }
            catch (Exception ex)
            {
                GlobalErro.TratarErro(ex, mostrarAlerta: true);
            }
        }

        private async void OnEditSwipeInvoked(object sender, EventArgs e)
        {
            try
            {
                if (sender is not SwipeItemView swipeItem || swipeItem.BindingContext is not Cliente cliente)
                    return;

                CloseParentSwipe(swipeItem);
                HandleEditar(cliente);
            }
            catch (Exception ex)
            {
                await ShowAlertAsync("Erro", $"Erro ao abrir edição: {ex.Message}");
            }
        }

        private async void OnDeleteSwipeInvoked(object sender, EventArgs e)
        {
            try
            {
                if (sender is not SwipeItemView swipeItem || swipeItem.BindingContext is not Cliente cliente)
                    return;

                CloseParentSwipe(swipeItem);
                await HandleEliminarAsync(cliente);
            }
            catch (Exception ex)
            {
                await ShowAlertAsync("Erro", $"Erro ao eliminar: {ex.Message}");
            }
        }

        private async void OnPastasSwipeInvoked(object sender, EventArgs e)
        {
            try
            {
                if (sender is not SwipeItemView swipeItem || swipeItem.BindingContext is not Cliente cliente)
                    return;

                CloseParentSwipe(swipeItem);
                await HandlePastasAsync(cliente);
            }
            catch (Exception ex)
            {
                await ShowAlertAsync("Erro", $"Erro ao abrir pastas: {ex.Message}");
            }
        }

        private async void OnServicesSwipeInvoked(object sender, EventArgs e)
        {
            try
            {
                if (sender is not SwipeItemView swipeItem || swipeItem.BindingContext is not Cliente cliente)
                    return;

                CloseParentSwipe(swipeItem);
                await HandleServicesAsync(cliente);
            }
            catch (Exception ex)
            {
                GlobalErro.TratarErro(ex, mostrarAlerta: true);
            }
        }

        private static void CloseParentSwipe(Element element)
        {
            FindParentSwipeView(element)?.Close();
        }

        private static SwipeView? FindParentSwipeView(Element? element)
        {
            while (element is not null)
            {
                if (element is SwipeView swipeView)
                    return swipeView;

                element = element.Parent;
            }

            return null;
        }

        private static async Task HandleServicesAsync(Cliente cliente)
        {
            var nome = string.IsNullOrWhiteSpace(cliente.CLINOME)
                ? (cliente.CLICODIGO ?? "Cliente")
                : cliente.CLINOME;

            await AppShell.DisplayToastAsync($"{nome}: {cliente.ServicosCountDisplay}.");
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
                var confirm = await ShowConfirmAsync("Eliminar Cliente",
                    $"Tem a certeza que deseja eliminar '{cliente.CLINOME}'?",
                    "Eliminar", "Cancelar");

                if (!confirm) return;

                if (BindingContext is ClientsPageModel vm)
                {
                    vm.SelectedCliente = cliente;

                    bool canParam = vm.DeleteCommand?.CanExecute(cliente) == true;
                    bool canNull  = vm.DeleteCommand?.CanExecute(null) == true;

                    await ShowAlertAsync("[DBG]", $"CanExec(param)={canParam} | CanExec(null)={canNull}");

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
                        await ShowAlertAsync("Aviso", "Não foi possível eliminar (command bloqueado).");
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
                    await ShowAlertAsync("Aviso", "Cliente sem código definido.");
                    return;
                }

                var uri = new Uri($"qfile://open?path=/mnt/remote/CLIENTES/{cliente.CLICODIGO}");
                var can = await Launcher.CanOpenAsync(uri);
                await ShowAlertAsync("[DBG]", $"CanOpen qfile = {can}");

                if (can)
                {
                    await Launcher.OpenAsync(uri);
                    await ShowAlertAsync("[DBG]", "OpenAsync(qfile) OK");
                }
                else
                {
                    await ShowAlertAsync("Qfile",
                        $"A abrir pasta do cliente {cliente.CLINOME}...\n\nCaminho: CLIENTES/{cliente.CLICODIGO}");
                }
            }
            catch (Exception ex)
            {
                await ShowAlertAsync("Erro", $"Falhou abrir Qfile: {ex.Message}");
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

        private async void OnAddVendedorTapped(object sender, EventArgs e)
        {
            try
            {
                if (BindingContext is not ClientsPageModel vm)
                    return;

                var popup = new AddVendedorPopup();
                var result = await AppShell.Current.ShowPopupAsync(popup);
                if (result is Vendedor vendedor)
                {
                    vm.UpsertVendedor(vendedor);
                    await GlobalToast.ShowAsync("Vendedor criado.", ToastTipo.Sucesso, 2000);
                }

                if (popup.VendedoresDirty)
                {
                    var preferred = result as Vendedor;
                    await vm.ReloadVendedoresAsync(preferred);
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
            HideDialCodePicker();
            UnfocusFormFields();
            if (BindingContext is ClientsPageModel vm &&
                vm.ClearCommand?.CanExecute(null) == true)
            {
                vm.ClearCommand.Execute(null);
            }
            ShowListView();
        }

        private void HideDialCodePicker()
        {
            if (DialCodePicker is null) return;

            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        DialCodePicker.Unfocus();
#if IOS
                        DismissIosFirstResponder();
                        if (DialCodePicker.Handler?.PlatformView is UIView view)
                        {
                            view.EndEditing(true);
                            if (view.IsFirstResponder)
                                view.ResignFirstResponder();
                            view.Superview?.EndEditing(true);
                            view.Window?.EndEditing(true);
                        }
#endif
                    }
                    catch (Exception ex)
                    {
                        GlobalErro.TratarErro(ex, mostrarAlerta: false);
                    }
                });
            }
            catch (Exception ex)
            {
                GlobalErro.TratarErro(ex, mostrarAlerta: false);
            }
        }

        private void UnfocusFormFields()
        {
            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        NomeEntry?.Unfocus();
                        TelefoneEntry?.Unfocus();
                        EmailEntry?.Unfocus();
                        VendedorPicker?.Unfocus();
                        ValorCreditoEntry?.Unfocus();
                        ExternoSwitch?.Unfocus();
                        AnuladoSwitch?.Unfocus();
                        DialCodePicker?.Unfocus();
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

#if IOS
        private static void DismissIosFirstResponder()
        {
            try
            {
                var scenes = UIApplication.SharedApplication?.ConnectedScenes;
                if (scenes == null) return;

                foreach (var windowScene in scenes.OfType<UIWindowScene>())
                {
                    foreach (var window in windowScene.Windows ?? Array.Empty<UIWindow>())
                    {
                        window?.EndEditing(true);
                    }
                }
            }
            catch
            {
                // Ignorar – apenas best effort para fechar o picker.
            }
        }
#endif

        private void OnValorCreditoFocused(object sender, FocusEventArgs e)
        {
            if (sender is not Entry entry)
                return;

            if (TryParseValorCredito(entry.Text, out var value))
            {
                var culture = CultureInfo.GetCultureInfo("pt-PT");
                entry.Text = value.ToString("0.00", culture);
            }

            entry.Dispatcher.Dispatch(() =>
            {
                var current = entry.Text ?? string.Empty;
                entry.CursorPosition = 0;
                entry.SelectionLength = current.Length;
            });
        }

        private void OnValorCreditoUnfocused(object sender, FocusEventArgs e)
        {
            if (sender is not Entry entry)
                return;

            var text = entry.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                entry.Text = string.Empty;
                return;
            }

            if (!TryParseValorCredito(text, out var value))
                return;

            var culture = CultureInfo.GetCultureInfo("pt-PT");
            var formattedNumber = value.ToString("N2", culture)
                                       .Replace('\u00A0', ' ')
                                       .Replace('\u202F', ' ');

            entry.Text = formattedNumber + " €";
        }

        private static bool TryParseValorCredito(string? text, out decimal value)
        {
            value = 0m;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var culture = CultureInfo.GetCultureInfo("pt-PT");

            var sanitized = text.Replace("€", string.Empty, StringComparison.OrdinalIgnoreCase);
            sanitized = new string(sanitized.Where(ch => !(char.IsWhiteSpace(ch) || ch == '\u00A0' || ch == '\u202F')).ToArray());
            sanitized = sanitized.Trim();

            if (sanitized.Length == 0)
                return false;

            if (sanitized.Contains('.', StringComparison.Ordinal) &&
                !sanitized.Contains(',', StringComparison.Ordinal))
            {
                sanitized = sanitized.Replace('.', ',');
            }

            if (decimal.TryParse(sanitized, NumberStyles.Any, culture, out value))
                return true;

            sanitized = sanitized.Replace(',', '.');
            return decimal.TryParse(sanitized, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }

#if IOS
        private void ConfigureSearchBarClearButton()
        {
            try
            {
                if (SearchBar.Handler?.PlatformView is UISearchBar nativeSearch)
                {
                    nativeSearch.SearchTextField.ClearButtonMode = UITextFieldViewMode.Never;
                }
            }
            catch (Exception ex)
            {
                GlobalErro.TratarErro(ex, mostrarAlerta: false);
            }
        }
#else
        private void ConfigureSearchBarClearButton() { }
#endif

        // Guardar
        private async void OnSaveClientTapped(object sender, EventArgs e)
        {
            await ShowAlertAsync("SAVE BUTTON", "Botão Save foi clicado!");
            
            try
            {
                if (BindingContext is not ClientsPageModel vm || vm.Editing is null) return;

                if (string.IsNullOrWhiteSpace(vm.Editing.CLINOME))
                {
                    await ShowAlertAsync("Aviso", "O nome do cliente é obrigatório.");
                    return;
                }

                bool isNew = string.IsNullOrWhiteSpace(vm.Editing.CLICODIGO);

                // Chamar o SaveCommand
                if (vm.SaveCommand?.CanExecute(null) == true)
                {
                    await vm.OnSaveAsync();
                }

                if (isNew)
                    await GlobalToast.ShowAsync("Cliente adicionado com sucesso! (Pasta a criar)", ToastTipo.Sucesso, 2000);
                else
                {
                    await GlobalToast.ShowAsync("Cliente atualizado com sucesso!", ToastTipo.Sucesso, 2000);
                    
                    // Se estamos editando (não é novo), abrir a pasta do cliente
                    await HandlePastasAsync(vm.Editing);
                }

                ShowListView();
            }
            catch (Exception ex)
            {
                await ShowAlertAsync("Erro", $"Erro ao guardar: {ex.Message}");
                GlobalErro.TratarErro(ex, mostrarAlerta: false);
            }
        }

        // Pastas do formulário
        private async void OnPastasFormTapped(object sender, EventArgs e)
        {
            await ShowAlertAsync("PASTAS BUTTON", "Botão Pastas foi clicado!");
            
            try
            {
                if (BindingContext is not ClientsPageModel vm || vm.Editing is null)
                {
                    await ShowAlertAsync("[DEBUG] Pastas", "VM ou Editing é null");
                    return;
                }

                await ShowAlertAsync("[DEBUG] Pastas", "Antes de OnPastasAsync");
                
                // Chamar diretamente o OnPastasAsync
                vm.SelectedCliente = vm.Editing;
                await vm.OnPastasAsync();
                
                await ShowAlertAsync("[DEBUG] Pastas", "Depois de OnPastasAsync");
            }
            catch (Exception ex)
            {
                await ShowAlertAsync("Erro", $"Erro ao abrir pastas: {ex.Message}\n\n{ex.StackTrace}");
                GlobalErro.TratarErro(ex, mostrarAlerta: false);
            }
        }

        // Eliminar do formulário
        private async void OnDeleteFromFormTapped(object sender, EventArgs e)
        {
            await ShowAlertAsync("DELETE BUTTON", "Botão Delete foi clicado!");
            
            try
            {
                if (BindingContext is not ClientsPageModel vm || vm.Editing is null)
                {
                    await ShowAlertAsync("[DEBUG] Delete", "VM ou Editing é null");
                    return;
                }

                var confirm = await ShowConfirmAsync("Eliminar Cliente",
                    $"Tem a certeza que deseja eliminar '{vm.Editing.CLINOME}'?",
                    "Eliminar", "Cancelar");

                if (!confirm) return;

                await ShowAlertAsync("[DEBUG] Delete", "Antes de OnDeleteAsync");
                
                // Chamar diretamente o OnDeleteAsync (que já está no ViewModel)
                vm.SelectedCliente = vm.Editing;
                await vm.OnDeleteAsync();
                
                await ShowAlertAsync("[DEBUG] Delete", "Depois de OnDeleteAsync");
                
                ShowListView();
                await GlobalToast.ShowAsync("Cliente eliminado com sucesso!", ToastTipo.Sucesso, 2000);
            }
            catch (Exception ex)
            {
                await ShowAlertAsync("Erro", $"Erro ao eliminar: {ex.Message}\n\n{ex.StackTrace}");
                GlobalErro.TratarErro(ex, mostrarAlerta: false);
            }
        }
    }
}
