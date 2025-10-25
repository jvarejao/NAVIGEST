// File: NAVIGEST.iOS/Pages/ClientsPage.xaml.cs
#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel; // Launcher.OpenAsync, MainThread
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

        // Lista vs Form
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

        // Tap na célula
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
            catch (Exception ex)
            {
                GlobalErro.TratarErro(ex, mostrarAlerta: true);
            }
        }

        // SearchBar
        private void OnSearchBarSearchButtonPressed(object sender, EventArgs e) => SearchBar.Unfocus();
        private void OnSearchBarTextChanged(object sender, TextChangedEventArgs e) { }
        private void OnCollectionViewScrolled(object sender, ItemsViewScrolledEventArgs e) => SearchBar.Unfocus();

        // ===== Swipe instrumentation (logs) =====
        private void OnSwipeStarted(object sender, SwipeStartedEventArgs e)
            => System.Diagnostics.Debug.WriteLine($"[SWIPE] Started Direction={e.SwipeDirection}");

        private void OnSwipeEnded(object sender, SwipeEndedEventArgs e)
            => System.Diagnostics.Debug.WriteLine($"[SWIPE] Ended IsOpen={e.IsOpen}");

        // Fecha swipe e aguarda animação
        private async Task CloseSwipeThenYieldAsync(object sender)
        {
            if (sender is Element el)
            {
                Element? p = el;
                while (p != null && p is not SwipeView) p = p.Parent;
                (p as SwipeView)?.Close();
            }
            await Task.Delay(120);
        }

        // ========== PASTAS ==========
        private async void OnPastasSwipeInvoked(object sender, EventArgs e)
        {
            await CloseSwipeThenYieldAsync(sender);
            await DisplayAlert("[DBG] Pastas", "Invoked", "OK");

            var cliente = (sender as Element)?.BindingContext as Cliente;
            await DoPastasAsync(cliente, sender);
        }

        private async void OnPastasTapFallback(object sender, TappedEventArgs e)
        {
            await CloseSwipeThenYieldAsync(sender);
            await DisplayAlert("[DBG] Pastas", "Tap Fallback", "OK");

            var cliente = e.Parameter as Cliente;
            await DoPastasAsync(cliente, sender);
        }

        private async Task DoPastasAsync(Cliente? cliente, object? origin = null)
        {
            await DisplayAlert("[DBG] Pastas", "Entrou DoPastasAsync", "OK");

            if (cliente == null)
            {
                await DisplayAlert("Erro", "Cliente não identificado.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(cliente.CLICODIGO))
            {
                await DisplayAlert("Aviso", "Cliente sem código definido.", "OK");
                return;
            }

            var qfile = new Uri($"qfile://open?path=/mnt/remote/CLIENTES/{cliente.CLICODIGO}");
            bool triedQfile = false, opened = false;

            try
            {
                var can = await Launcher.CanOpenAsync(qfile);
                await DisplayAlert("[DBG] Pastas", $"CanOpen qfile={can}", "OK");
                if (can)
                {
                    triedQfile = true;
                    await Launcher.OpenAsync(qfile);
                    opened = true;
                    await DisplayAlert("[DBG] Pastas", "Launcher.OpenAsync(qfile) OK", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("[DBG] Pastas", $"Qfile exception: {ex.Message}", "OK");
            }

            if (!opened)
            {
                // Fallback VISÍVEL: abre um http qualquer (para confirmar que o Launcher funciona)
                var http = new Uri($"https://example.com/CLIENTES/{Uri.EscapeDataString(cliente.CLICODIGO)}");
                await DisplayAlert("[DBG] Pastas", $"Fallback HTTP: {http}", "OK");
                try
                {
                    await Launcher.OpenAsync(http);
                    await DisplayAlert("[DBG] Pastas", "Launcher.OpenAsync(http) OK", "OK");
                }
                catch (Exception ex2)
                {
                    await DisplayAlert("Qfile",
                        $"A abrir pasta do cliente {cliente.CLINOME}...\nCaminho: CLIENTES/{cliente.CLICODIGO}\n(Launcher falhou: {ex2.Message})",
                        "OK");
                }
            }
        }

        // ========== EDITAR (mantido) ==========
        private async void OnEditSwipeInvoked(object sender, EventArgs e)
        {
            await CloseSwipeThenYieldAsync(sender);
            await DisplayAlert("[DBG] Editar", "Invoked", "OK");

            var cliente = (sender as Element)?.BindingContext as Cliente;
            DoEdit(cliente, sender);
        }

        private async void OnEditTapFallback(object sender, TappedEventArgs e)
        {
            await CloseSwipeThenYieldAsync(sender);
            await DisplayAlert("[DBG] Editar", "Tap Fallback", "OK");

            var cliente = e.Parameter as Cliente;
            DoEdit(cliente, sender);
        }

        private void DoEdit(Cliente? cliente, object? origin = null)
        {
            if (cliente == null)
            {
                DisplayAlert("Erro", "Cliente não identificado.", "OK");
                return;
            }

            if (BindingContext is ClientsPageModel vm &&
                vm.SelectCommand?.CanExecute(cliente) == true)
            {
                vm.SelectCommand.Execute(cliente);
            }
            ShowFormView(isNew: false);
        }

        // ========== ELIMINAR ==========
        private async void OnDeleteSwipeInvoked(object sender, EventArgs e)
        {
            await CloseSwipeThenYieldAsync(sender);
            await DisplayAlert("[DBG] Eliminar", "Invoked", "OK");

            var cliente = (sender as Element)?.BindingContext as Cliente;
            await DoDeleteAsync(cliente, sender);
        }

        private async void OnDeleteTapFallback(object sender, TappedEventArgs e)
        {
            await CloseSwipeThenYieldAsync(sender);
            await DisplayAlert("[DBG] Eliminar", "Tap Fallback", "OK");

            var cliente = e.Parameter as Cliente;
            await DoDeleteAsync(cliente, sender);
        }

        private async Task DoDeleteAsync(Cliente? cliente, object? origin = null)
        {
            await DisplayAlert("[DBG] Eliminar", "Entrou DoDeleteAsync", "OK");

            if (cliente == null)
            {
                await DisplayAlert("Erro", "Cliente não identificado.", "OK");
                return;
            }

            bool confirm = await DisplayAlert("Eliminar Cliente",
                $"Tem a certeza que deseja eliminar '{cliente.CLINOME}'?",
                "Eliminar", "Cancelar");

            await DisplayAlert("[DBG] Eliminar", $"confirm={confirm}", "OK");
            if (!confirm) return;

            if (BindingContext is ClientsPageModel vm)
            {
                vm.SelectedCliente = cliente;

                bool canParam = vm.DeleteCommand?.CanExecute(cliente) == true;
                bool canNull  = vm.DeleteCommand?.CanExecute(null) == true;

                await DisplayAlert("[DBG] Eliminar", $"CanExec(param)={canParam} | CanExec(null)={canNull}", "OK");

                if (canParam)
                {
                    vm.DeleteCommand!.Execute(cliente);
                    await DisplayAlert("[DBG] Eliminar", "Execute(param) OK", "OK");
                    await GlobalToast.ShowAsync("Cliente eliminado com sucesso.", ToastTipo.Sucesso, 2000);
                }
                else if (canNull)
                {
                    vm.DeleteCommand!.Execute(null);
                    await DisplayAlert("[DBG] Eliminar", "Execute(null) OK", "OK");
                    await GlobalToast.ShowAsync("Cliente eliminado com sucesso.", ToastTipo.Sucesso, 2000);
                }
                else
                {
                    await DisplayAlert("Aviso", "DeleteCommand não está executável.", "OK");
                }
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
                if (BindingContext is not ClientsPageModel vm || vm.Editing is null)
                    return;

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

        // Eliminar no Form
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

#if WINDOWS
// Código Windows específico (se necessário)
#endif
#if ANDROID
// Código Android específico (se necessário)
#endif
#if IOS
// Código iOS específico (se necessário)
#endif
