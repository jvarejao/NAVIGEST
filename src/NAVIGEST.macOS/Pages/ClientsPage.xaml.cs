#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using NAVIGEST.macOS.PageModels;
using NAVIGEST.macOS; // GlobalToast / GlobalErro

namespace NAVIGEST.macOS.Pages
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

        private void OnEditClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is Models.Cliente cliente && BindingContext is ClientsPageModel vm)
            {
                vm.OpenEditCommand.Execute(cliente);
            }
        }

        private void OnDeleteClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is Models.Cliente cliente && BindingContext is ClientsPageModel vm)
            {
                vm.DeleteCommand.Execute(cliente);
            }
        }

        private void OnPastasClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is Models.Cliente cliente && BindingContext is ClientsPageModel vm)
            {
                vm.PastasCommand.Execute(cliente);
            }
        }

        private void OnServicesClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is Models.Cliente cliente && BindingContext is ClientsPageModel vm)
            {
                vm.ServicesCommand.Execute(cliente);
            }
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
