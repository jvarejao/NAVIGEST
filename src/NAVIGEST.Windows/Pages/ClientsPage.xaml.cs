// File: NAVIGEST.macOS/Pages/ClientsPage.xaml.cs
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
            // Desktop header visibilidade j� tratada em XAML condicional; manter se necess�rio
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

        // -------- Sele��o Desktop --------
        private async void OnClientSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (BindingContext is ClientsPageModel vm && e.CurrentSelection?.FirstOrDefault() is object item)
                {
                    if (vm.SelectCommand?.CanExecute(item) == true)
                    {
                        vm.SelectCommand.Execute(item);
                        // Scroll para o topo para mostrar os campos de edição
                        await MainScrollView.ScrollToAsync(0, 0, true);
                    }
                }
            }
            catch (Exception ex) { GlobalErro.TratarErro(ex, mostrarAlerta: false); }
        }

        // -------- Overlay Mobile --------
        private void OnOpenClientPicker(object sender, EventArgs e)
        {
            ClientPickerOverlay.IsVisible = true;
        }

        private void OnCloseClientPicker(object sender, EventArgs e)
        {
            ClientPickerOverlay.IsVisible = false;
        }

        private async void OnClientPickerSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (BindingContext is ClientsPageModel vm && e.CurrentSelection?.FirstOrDefault() is object item)
                {
                    if (vm.SelectCommand?.CanExecute(item) == true)
                    {
                        vm.SelectCommand.Execute(item);
                        ClientPickerOverlay.IsVisible = false;
                        // Scroll para o topo para mostrar os campos de edição
                        await MainScrollView.ScrollToAsync(0, 0, true);
                    }
                }
            }
            catch (Exception ex) { GlobalErro.TratarErro(ex, mostrarAlerta: false); }
        }

        // -------- Valor Cr�dito --------
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
// C�digo Windows espec�fico (exemplo: anima��es, navega��o, layouts)
#endif
#if ANDROID
// C�digo Android espec�fico (exemplo: anima��es, navega��o, layouts)
#endif
#if IOS
// C�digo iOS espec�fico (exemplo: anima��es, navega��o, layouts)
#endif
