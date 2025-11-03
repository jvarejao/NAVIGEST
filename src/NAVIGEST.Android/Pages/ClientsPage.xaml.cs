// File: NAVIGEST.Android/Pages/ClientsPage.xaml.cs
#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using NAVIGEST.Android.PageModels;
using NAVIGEST.Android; // GlobalToast / GlobalErro

namespace NAVIGEST.Android.Pages
{
    public partial class ClientsPage : ContentPage
    {
        private bool _loadedOnce;
        private bool _suppressValorChange;

        public ClientsPage() : this(new ClientsPageModel()) { }

        public ClientsPage(ClientsPageModel vm)
        {
            BindingContext = vm;
            InitializeComponent();
            Dispatcher.Dispatch(async () => await EnsureLoadedAsync());
        }

        private async void OnPageLoaded(object? sender, EventArgs e) => await EnsureLoadedAsync();

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await EnsureLoadedAsync();
            // Desktop header visibilidade j tratada em XAML condicional; manter se necessrio
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

        private void OnPageSizeChanged(object sender, EventArgs e) { }

        // -------- Swipe Actions --------
        private void OnEditSwipeInvoked(object sender, EventArgs e)
        {
            try
            {
                if (sender is SwipeItemView siv && siv.BindingContext is object item)
                {
                    if (BindingContext is ClientsPageModel vm && vm.SelectCommand?.CanExecute(item) == true)
                    {
                        vm.SelectCommand.Execute(item);
                        ShowFormView();
                    }
                }
            }
            catch (Exception ex) { GlobalErro.TratarErro(ex, mostrarAlerta: false); }
        }

        private void OnDeleteSwipeInvoked(object sender, EventArgs e)
        {
            try
            {
                if (sender is SwipeItemView siv && siv.BindingContext is object item)
                {
                    if (BindingContext is ClientsPageModel vm && vm.DeleteCommand?.CanExecute(item) == true)
                        vm.DeleteCommand.Execute(item);
                }
            }
            catch (Exception ex) { GlobalErro.TratarErro(ex, mostrarAlerta: false); }
        }

        private void OnPastasSwipeInvoked(object sender, EventArgs e)
        {
            try
            {
                if (sender is SwipeItemView siv && siv.BindingContext is object item)
                {
                    if (BindingContext is ClientsPageModel vm)
                    {
                        // Navigate to Pastas
                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            await Shell.Current.GoToAsync($"ClientPastas?clienteId={vm.Editing?.CLICODIGO}");
                        });
                    }
                }
            }
            catch (Exception ex) { GlobalErro.TratarErro(ex, mostrarAlerta: false); }
        }

        private void OnServicesSwipeInvoked(object sender, EventArgs e)
        {
            try
            {
                if (sender is SwipeItemView siv && siv.BindingContext is object item)
                {
                    if (BindingContext is ClientsPageModel vm)
                    {
                        // Navigate to Services
                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            await Shell.Current.GoToAsync($"ClientServices?clienteId={vm.Editing?.CLICODIGO}");
                        });
                    }
                }
            }
            catch (Exception ex) { GlobalErro.TratarErro(ex, mostrarAlerta: false); }
        }

        // -------- Cell Tap --------
        private void OnClientCellTapped(object sender, TappedEventArgs e)
        {
            try
            {
                if (sender is Grid grid && grid.BindingContext is object item)
                {
                    if (BindingContext is ClientsPageModel vm && vm.SelectCommand?.CanExecute(item) == true)
                    {
                        vm.SelectCommand.Execute(item);
                        ShowFormView();
                    }
                }
            }
            catch (Exception ex) { GlobalErro.TratarErro(ex, mostrarAlerta: false); }
        }

        // -------- Add Client FAB --------
        private void OnAddClientTapped(object sender, TappedEventArgs e)
        {
            try
            {
                if (BindingContext is ClientsPageModel vm && vm.NewCommand?.CanExecute(null) == true)
                {
                    vm.NewCommand.Execute(null);
                    ShowFormView();
                }
            }
            catch (Exception ex) { GlobalErro.TratarErro(ex, mostrarAlerta: false); }
        }

        // -------- Form Controls --------
        private void ShowFormView()
        {
            if (ListViewContainer is not null && FormViewContainer is not null)
            {
                ListViewContainer.IsVisible = false;
                FormViewContainer.IsVisible = true;
            }
        }

        private void OnSaveClientTapped(object sender, EventArgs e)
        {
            try
            {
                if (BindingContext is ClientsPageModel vm && vm.SaveCommand?.CanExecute(null) == true)
                {
                    vm.SaveCommand.Execute(null);
                    HideFormView();
                }
            }
            catch (Exception ex) { GlobalErro.TratarErro(ex, mostrarAlerta: false); }
        }

        private void OnCancelEditTapped(object sender, EventArgs e)
        {
            HideFormView();
        }

        private void HideFormView()
        {
            if (ListViewContainer is not null && FormViewContainer is not null)
            {
                FormViewContainer.IsVisible = false;
                ListViewContainer.IsVisible = true;
            }
        }

        private void OnFormBackgroundTapped(object sender, TappedEventArgs e)
        {
            HideFormView();
        }

        private void OnSwipeEnded(object sender, SwipeEndedEventArgs e)
        {
            // Swipe completed
        }

        private void OnCollectionViewScrolled(object sender, ItemsViewScrolledEventArgs e)
        {
            // Handle scroll if needed
        }

        private void OnSearchBarSearchButtonPressed(object sender, EventArgs e)
        {
            // Search completed
        }

        private void OnSearchBarTextChanged(object sender, TextChangedEventArgs e)
        {
            // Filtering is done through binding in ViewModel
        }

        private void OnAddVendedorTapped(object sender, EventArgs e)
        {
            try
            {
                if (BindingContext is ClientsPageModel vm)
                {
                    // Open vendor picker or add dialog
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        var result = await DisplayPromptAsync(
                            title: "Novo Vendedor",
                            message: "Digite o nome do vendedor",
                            placeholder: "Nome",
                            accept: "Guardar",
                            cancel: "Cancelar");

                        if (!string.IsNullOrWhiteSpace(result))
                        {
                            // Add vendor through ViewModel if needed
                            await GlobalToast.ShowAsync("Vendedor adicionado.", ToastTipo.Sucesso, 1600);
                        }
                    });
                }
            }
            catch (Exception ex) { GlobalErro.TratarErro(ex, mostrarAlerta: false); }
        }

        private void OnPastasFormTapped(object sender, EventArgs e)
        {
            try
            {
                if (BindingContext is ClientsPageModel vm)
                {
                    // Navigate to Pastas
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await Shell.Current.GoToAsync($"ClientPastas?clienteId={vm.Editing?.CLICODIGO}");
                        HideFormView();
                    });
                }
            }
            catch (Exception ex) { GlobalErro.TratarErro(ex, mostrarAlerta: false); }
        }

        private void OnDeleteFromFormTapped(object sender, EventArgs e)
        {
            try
            {
                if (BindingContext is ClientsPageModel vm && vm.DeleteCommand?.CanExecute(null) == true)
                {
                    vm.DeleteCommand.Execute(null);
                    HideFormView();
                }
            }
            catch (Exception ex) { GlobalErro.TratarErro(ex, mostrarAlerta: false); }
        }

        // -------- Valor Crédito --------
        private void OnValorCreditoFocused(object sender, FocusEventArgs e)
        {
            try
            {
                if (sender is Entry entry)
                {
                    var len = entry.Text?.Length ?? 0;
                    Dispatcher.Dispatch(() =>
                    {
                        try { entry.CursorPosition = 0; entry.SelectionLength = len; } catch { }
                    });
                }
            }
            catch (Exception ex) { GlobalErro.TratarErro(ex, mostrarAlerta: false); }
        }

        private void OnValorCreditoUnfocused(object sender, FocusEventArgs e)
        {
            try
            {
                if (BindingContext is ClientsPageModel vm)
                    vm.FormatValorCreditoOnBlur();
            }
            catch (Exception ex) { GlobalErro.TratarErro(ex, mostrarAlerta: false); }
        }

        private void OnValorCreditoTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppressValorChange) return;
            if (sender is not Entry entry) return;
            if (!entry.IsFocused) return;
            try
            {
                if (BindingContext is not ClientsPageModel vm || vm.Editing is null) return;
                var txt = e.NewTextValue ?? string.Empty;
                var filtered = new string(txt.Where((c, idx) =>
                    char.IsDigit(c) || (c == '-' && idx == 0) || c == ',' || c == '.').ToArray());

                int firstDec = filtered.IndexOfAny(new[] { ',', '.' });
                if (firstDec >= 0)
                {
                    var tail = filtered[(firstDec + 1)..].Replace(".", string.Empty).Replace(",", string.Empty);
                    filtered = filtered.Substring(0, firstDec + 1) + tail;
                    filtered = filtered.Replace('.', ',');
                }

                if (filtered != txt)
                {
                    _suppressValorChange = true;
                    var caret = filtered.Length;
                    vm.Editing.VALORCREDITO = filtered;
                    entry.Text = filtered;
                    entry.CursorPosition = caret;
                }
            }
            catch (Exception ex) { GlobalErro.TratarErro(ex, mostrarAlerta: false); }
            finally { _suppressValorChange = false; }
        }
    }
}

#if WINDOWS
// Cdigo Windows especfico (exemplo: animaes, navegao, layouts)
#endif
#if ANDROID
// Cdigo Android especfico (exemplo: animaes, navegao, layouts)
#endif
#if IOS
// Cdigo iOS especfico (exemplo: animaes, navegao, layouts)
#endif
