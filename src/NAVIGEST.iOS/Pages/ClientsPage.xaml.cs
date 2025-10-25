// File: NAVIGEST.iOS/Pages/ClientsPage.xaml.cs
#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel; // Launcher.OpenAsync
using NAVIGEST.iOS.PageModels;
using NAVIGEST.iOS.Models;
using NAVIGEST.iOS;

namespace NAVIGEST.iOS.Pages
{
    public partial class ClientsPage : ContentPage
    {
        private bool _loadedOnce;
        private bool _isEditMode;

        // toggle para debug de taps/swipes
        private const bool DEBUG_TAPS = true;

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
            DeleteFormButton.IsVisible = !isNew;
            SaveButton.Text = isNew ? "Adicionar" : "Atualizar";
        }

        private void OnClientCellTapped(object sender, EventArgs e)
        {
            try
            {
                if (sender is Grid grid && grid.BindingContext is Cliente cliente)
                {
                    if (BindingContext is ClientsPageModel vm &&
                        vm.SelectCommand?.CanExecute(cliente) == true)
                        vm.SelectCommand.Execute(cliente);

                    ShowFormView(isNew: false);
                }
            }
            catch (Exception ex) { GlobalErro.TratarErro(ex, mostrarAlerta: true); }
        }

        private void OnSearchBarSearchButtonPressed(object sender, EventArgs e) => SearchBar.Unfocus();
        private void OnSearchBarTextChanged(object sender, TextChangedEventArgs e) { }
        private void OnCollectionViewScrolled(object sender, ItemsViewScrolledEventArgs e) => SearchBar.Unfocus();

        // ==================== INSTRUMENTAÇÃO SWIPE ====================

        private void OnSwipeStarted(object sender, SwipeStartedEventArgs e)
        {
            if (!DEBUG_TAPS) return;
            System.Diagnostics.Debug.WriteLine($"[SWIPE] Started Direction={e.SwipeDirection}");
        }

        private void OnSwipeEnded(object sender, SwipeEndedEventArgs e)
        {
            if (!DEBUG_TAPS) return;
            System.Diagnostics.Debug.WriteLine($"[SWIPE] Ended IsOpen={e.IsOpen}");
        }

        private void LogTap(string tag, Element? el, object? param = null)
        {
            if (!DEBUG_TAPS) return;
            var bc = el?.BindingContext?.GetType().Name ?? "null";
            var pt = param?.GetType().Name ?? "null";
            System.Diagnostics.Debug.WriteLine($"[{tag}] sender={el?.GetType().Name}, BC={bc}, Param={pt}");
            // feedback visual curto sem bloquear
            _ = GlobalToast.ShowAsync($"{tag}", ToastTipo.Info, 800);
        }

        private void CloseSwipeFrom(object sender)
        {
            if (sender is not Element el) return;
            Element? p = el;
            while (p != null && p is not SwipeView) p = p.Parent;
            (p as SwipeView)?.Close();
        }

        // ==================== AÇÕES (Métodos base) ====================

        private async Task DoPastasAsync(Cliente cliente, object? origin = null)
        {
            LogTap("PASTAS:Do", origin as Element, cliente);

            if (string.IsNullOrWhiteSpace(cliente.CLICODIGO))
            {
                await DisplayAlert("Aviso", "Cliente sem código definido.", "OK");
                return;
            }

            var uri = new Uri($"qfile://open?path=/mnt/remote/CLIENTES/{cliente.CLICODIGO}");
            try { await Launcher.OpenAsync(uri); }
            catch
            {
                await DisplayAlert("Qfile",
                    $"A abrir pasta do cliente {cliente.CLINOME}...\n\nCaminho: CLIENTES/{cliente.CLICODIGO}",
                    "OK");
            }
        }

        private void DoEdit(Cliente cliente, object? origin = null)
        {
            LogTap("EDITAR:Do", origin as Element, cliente);

            if (BindingContext is ClientsPageModel vm &&
                vm.SelectCommand?.CanExecute(cliente) == true)
            {
                vm.SelectCommand.Execute(cliente);
            }
            ShowFormView(isNew: false);
        }

        private async Task DoDeleteAsync(Cliente cliente, object? origin = null)
        {
            LogTap("ELIMINAR:Do", origin as Element, cliente);

            var confirm = await DisplayAlert("Eliminar Cliente",
                $"Tem a certeza que deseja eliminar '{cliente.CLINOME}'?",
                "Eliminar", "Cancelar");

            if (confirm && BindingContext is ClientsPageModel vm)
            {
                // se o Delete usa SelectedCliente:
                vm.SelectedCliente = cliente;

                if (vm.DeleteCommand?.CanExecute(null) == true)
                {
                    vm.DeleteCommand.Execute(null);
                    await GlobalToast.ShowAsync("Cliente eliminado com sucesso.", ToastTipo.Sucesso, 2000);
                }
            }
        }

        // ==================== Invoked (oficial do Swipe) ====================

        private async void OnPastasSwipeInvoked(object sender, EventArgs e)
        {
            LogTap("PASTAS:Invoked", sender as Element);
            try
            {
                var cliente = (sender as Element)?.BindingContext as Cliente;
                if (cliente == null) { await DisplayAlert("Erro", "Cliente não identificado.", "OK"); return; }

                await DoPastasAsync(cliente, sender);
            }
            catch (Exception ex) { GlobalErro.TratarErro(ex, mostrarAlerta: true); }
            finally { CloseSwipeFrom(sender); }
        }

        private void OnEditSwipeInvoked(object sender, EventArgs e)
        {
            LogTap("EDITAR:Invoked", sender as Element);
            try
            {
                var cliente = (sender as Element)?.BindingContext as Cliente;
                if (cliente == null) return;

                DoEdit(cliente, sender);
            }
            catch (Exception ex) { GlobalErro.TratarErro(ex, mostrarAlerta: true); }
            finally { CloseSwipeFrom(sender); }
        }

        private async void OnDeleteSwipeInvoked(object sender, EventArgs e)
        {
            LogTap("ELIMINAR:Invoked", sender as Element);
            try
            {
                var cliente = (sender as Element)?.BindingContext as Cliente;
                if (cliente == null)
                {
                    await DisplayAlert("Erro", "Cliente não identificado.", "OK");
                    return;
                }

                await DoDeleteAsync(cliente, sender);
            }
            catch (Exception ex) { GlobalErro.TratarErro(ex, mostrarAlerta: true); }
            finally { CloseSwipeFrom(sender); }
        }

        // ==================== Tap Fallback (instrumentação + execução) ====================

        private async void OnPastasTapFallback(object sender, TappedEventArgs e)
        {
            LogTap("PASTAS:Tap", sender as Element, e.Parameter);
            try
            {
                if (e.Parameter is Cliente c) await DoPastasAsync(c, sender);
            }
            catch (Exception ex) { GlobalErro.TratarErro(ex, mostrarAlerta: true); }
            finally { CloseSwipeFrom(sender); }
        }

        private void OnEditTapFallback(object sender, TappedEventArgs e)
        {
            LogTap("EDITAR:Tap", sender as Element, e.Parameter);
            try
            {
                if (e.Parameter is Cliente c) DoEdit(c, sender);
            }
            catch (Exception ex) { GlobalErro.TratarErro(ex, mostrarAlerta: true); }
            finally { CloseSwipeFrom(sender); }
        }

        private async void OnDeleteTapFallback(object sender, TappedEventArgs e)
        {
            LogTap("ELIMINAR:Tap", sender as Element, e.Parameter);
            try
            {
                if (e.Parameter is Cliente c) await DoDeleteAsync(c, sender);
            }
            catch (Exception ex) { GlobalErro.TratarErro(ex, mostrarAlerta: true); }
            finally { CloseSwipeFrom(sender); }
        }

        // ===============================================================

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
            catch (Exception ex) { GlobalErro.TratarErro(ex, mostrarAlerta: true); }
        }

        private void OnCancelEditTapped(object sender, EventArgs e)
        {
            if (BindingContext is ClientsPageModel vm &&
                vm.ClearCommand?.CanExecute(null) == true)
            {
                vm.ClearCommand.Execute(null);
            }
            ShowListView();
        }

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

        private async void OnDeleteFromFormTapped(object sender, EventArgs e)
        {
            try
            {
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
