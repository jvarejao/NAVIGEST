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

        // SearchBar
        private void OnSearchBarSearchButtonPressed(object sender, EventArgs e) => SearchBar.Unfocus();
        private void OnSearchBarTextChanged(object sender, TextChangedEventArgs e) { }
        private void OnCollectionViewScrolled(object sender, ItemsViewScrolledEventArgs e) => SearchBar.Unfocus();

        // ---------- Swipe instrumentation (opcional) ----------
        private void OnSwipeStarted(object sender, SwipeStartedEventArgs e)
            => System.Diagnostics.Debug.WriteLine($"[SWIPE] Started Direction={e.SwipeDirection}");

        private void OnSwipeEnded(object sender, SwipeEndedEventArgs e)
            => System.Diagnostics.Debug.WriteLine($"[SWIPE] Ended IsOpen={e.IsOpen}");

        // Fecha o swipe e dá 75ms para acabar a animação (evita UI “presa” atrás do swipe)
        private async Task CloseSwipeThenYieldAsync(object sender)
        {
            if (sender is Element el)
            {
                Element? p = el;
                while (p != null && p is not SwipeView) p = p.Parent;
                (p as SwipeView)?.Close();
            }
            await Task.Delay(75);
        }

        // ---------- PASTAS ----------
        private async void OnPastasSwipeInvoked(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[PASTAS] Invoked");
            await CloseSwipeThenYieldAsync(sender);

            var cliente = (sender as Element)?.BindingContext as Cliente;
            await DoPastasAsync(cliente, sender);
        }

        private async void OnPastasTapFallback(object sender, TappedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[PASTAS] Tap Fallback param={e.Parameter?.GetType().Name}");
            await CloseSwipeThenYieldAsync(sender);

            var cliente = e.Parameter as Cliente;
            await DoPastasAsync(cliente, sender);
        }

        private async Task DoPastasAsync(Cliente? cliente, object? origin = null)
        {
            if (cliente == null)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                    DisplayAlert("Erro", "Cliente não identificado.", "OK"));
                System.Diagnostics.Debug.WriteLine("[PASTAS] cliente=null");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[PASTAS] CLICODIGO={cliente.CLICODIGO}");

            if (string.IsNullOrWhiteSpace(cliente.CLICODIGO))
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                    DisplayAlert("Aviso", "Cliente sem código definido.", "OK"));
                return;
            }

            var uri = new Uri($"qfile://open?path=/mnt/remote/CLIENTES/{cliente.CLICODIGO}");

            try
            {
                var can = await Launcher.CanOpenAsync(uri);
                System.Diagnostics.Debug.WriteLine($"[PASTAS] CanOpen={can}");
                if (can)
                {
                    await Launcher.OpenAsync(uri);
                    System.Diagnostics.Debug.WriteLine("[PASTAS] Launcher.OpenAsync OK");
                }
                else
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                        DisplayAlert("Qfile",
                            $"A abrir pasta do cliente {cliente.CLINOME}...\n\nCaminho: CLIENTES/{cliente.CLICODIGO}",
                            "OK"));
                    System.Diagnostics.Debug.WriteLine("[PASTAS] CanOpen=false -> Alert fallback");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PASTAS][ERR] {ex}");
                await MainThread.InvokeOnMainThreadAsync(() =>
                    DisplayAlert("Erro", $"Falhou abrir Qfile: {ex.Message}", "OK"));
            }
        }

        // ---------- EDITAR (mantido; já funcionava) ----------
        private async void OnEditSwipeInvoked(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[EDITAR] Invoked");
            await CloseSwipeThenYieldAsync(sender);

            var cliente = (sender as Element)?.BindingContext as Cliente;
            DoEdit(cliente, sender);
        }

        private async void OnEditTapFallback(object sender, TappedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[EDITAR] Tap Fallback param={e.Parameter?.GetType().Name}");
            await CloseSwipeThenYieldAsync(sender);

            var cliente = e.Parameter as Cliente;
            DoEdit(cliente, sender);
        }

        private void DoEdit(Cliente? cliente, object? origin = null)
        {
            if (cliente == null)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                    DisplayAlert("Erro", "Cliente não identificado.", "OK"));
                System.Diagnostics.Debug.WriteLine("[EDITAR] cliente=null");
                return;
            }

            if (BindingContext is ClientsPageModel vm &&
                vm.SelectCommand?.CanExecute(cliente) == true)
            {
                vm.SelectCommand.Execute(cliente);
                System.Diagnostics.Debug.WriteLine("[EDITAR] SelectCommand executed");
            }
            MainThread.BeginInvokeOnMainThread(() => ShowFormView(isNew: false));
        }

        // ---------- ELIMINAR ----------
        private async void OnDeleteSwipeInvoked(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[ELIMINAR] Invoked");
            await CloseSwipeThenYieldAsync(sender);

            var cliente = (sender as Element)?.BindingContext as Cliente;
            await DoDeleteAsync(cliente, sender);
        }

        private async void OnDeleteTapFallback(object sender, TappedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[ELIMINAR] Tap Fallback param={e.Parameter?.GetType().Name}");
            await CloseSwipeThenYieldAsync(sender);

            var cliente = e.Parameter as Cliente;
            await DoDeleteAsync(cliente, sender);
        }

        private async Task DoDeleteAsync(Cliente? cliente, object? origin = null)
        {
            if (cliente == null)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                    DisplayAlert("Erro", "Cliente não identificado.", "OK"));
            }

            bool confirm = await MainThread.InvokeOnMainThreadAsync(async () =>
                await DisplayAlert("Eliminar Cliente",
                    $"Tem a certeza que deseja eliminar '{cliente?.CLINOME}'?",
                    "Eliminar", "Cancelar"));

            System.Diagnostics.Debug.WriteLine($"[ELIMINAR] confirm={confirm}");
            if (!confirm || cliente == null) return;

            if (BindingContext is ClientsPageModel vm)
            {
                // Alguns VMs usam SelectedCliente; outros esperam parâmetro no Command.
                vm.SelectedCliente = cliente;

                bool didExec = false;

                if (vm.DeleteCommand != null && vm.DeleteCommand.CanExecute(cliente))
                {
                    vm.DeleteCommand.Execute(cliente);
                    didExec = true;
                    System.Diagnostics.Debug.WriteLine("[ELIMINAR] DeleteCommand(cliente) executed");
                }
                else if (vm.DeleteCommand != null && vm.DeleteCommand.CanExecute(null))
                {
                    vm.DeleteCommand.Execute(null);
                    didExec = true;
                    System.Diagnostics.Debug.WriteLine("[ELIMINAR] DeleteCommand(null) executed");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[ELIMINAR] DeleteCommand not executable");
                }

                if (didExec)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                        GlobalToast.ShowAsync("Cliente eliminado com sucesso.", ToastTipo.Sucesso, 2000));
                }
                else
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                        DisplayAlert("Aviso", "A eliminação não foi executada (command bloqueado).", "OK"));
                }
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
// Código Windows específico (se necessário)
#endif
#if ANDROID
// Código Android específico (se necessário)
#endif
#if IOS
// Código iOS específico (se necessário)
#endif
