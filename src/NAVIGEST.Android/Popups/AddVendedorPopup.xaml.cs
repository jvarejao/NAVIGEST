using System;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using NAVIGEST.Android;
using NAVIGEST.Android.Models;
using NAVIGEST.Android.Services;

namespace NAVIGEST.Android.Popups;

public partial class AddVendedorPopup : Popup
{
    private bool _isBusy;
    internal bool VendedoresDirty { get; private set; }

    public AddVendedorPopup()
    {
        InitializeComponent();
        Opened += OnPopupOpened;
        NomeEntry.TextChanged += OnNomeTextChanged;
    }

    private void OnPopupOpened(object? sender, PopupOpenedEventArgs e) => _ = InitializeAsync();

    private async Task InitializeAsync()
    {
        try
        {
            SetBusy(true);
            NomeEntry.Text = string.Empty;

            var nextId = await DatabaseService.PeekNextVendedorIdAsync();
            IdEntry.Text = nextId.ToString();

            MainThread.BeginInvokeOnMainThread(() => NomeEntry.Focus());
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex);
            await GlobalToast.ShowAsync("Erro ao preparar o formulário de vendedor.", ToastTipo.Erro, 2500);
            Close(null);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool value)
    {
        _isBusy = value;
        BusyIndicator.IsVisible = value;
        BusyIndicator.IsRunning = value;
        SaveButton.IsEnabled = !value;
        CancelButton.IsEnabled = !value;
        if (ListButton is not null)
            ListButton.IsEnabled = !value;
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (_isBusy)
            return;

        var nome = NomeEntry.Text?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(nome))
        {
            ShowError("Introduza o nome do vendedor.");
            return;
        }

        try
        {
            SetBusy(true);
            HideError();
            var vendedor = await DatabaseService.InsertVendedorAsync(nome);
            VendedoresDirty = true;
            Close(vendedor);
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex);
            await GlobalToast.ShowAsync("Erro ao guardar o vendedor.", ToastTipo.Erro, 2500);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void OnCancelClicked(object sender, EventArgs e)
    {
        if (_isBusy)
            return;

        Close(null);
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    private void HideError()
    {
        ErrorLabel.Text = string.Empty;
        ErrorLabel.IsVisible = false;
    }

    private void OnNomeTextChanged(object? sender, TextChangedEventArgs e)
    {
        HideError();

        if (sender is not Entry entry)
            return;

        var upper = e.NewTextValue?.ToUpperInvariant() ?? string.Empty;
        if (upper == e.NewTextValue)
            return;

        var caret = Math.Max(0, entry.CursorPosition);

        entry.TextChanged -= OnNomeTextChanged;
        entry.Text = upper;
        entry.CursorPosition = Math.Min(caret, upper.Length);
        entry.SelectionLength = 0;
        entry.TextChanged += OnNomeTextChanged;
    }

    private async void OnShowVendedoresClicked(object sender, EventArgs e)
    {
        if (_isBusy)
            return;

        try
        {
            VendedoresDirty = false;
            _isBusy = true;
            SaveButton.IsEnabled = false;
            CancelButton.IsEnabled = false;
            if (ListButton is not null)
                ListButton.IsEnabled = false;

            var popup = new VendedoresListPopup();
            var result = await AppShell.Current.ShowPopupAsync(popup) as VendedorListResult;

            if (result is not null)
            {
                if (result.RefreshRequested)
                    VendedoresDirty = true;

                if (result.SelectedVendedor is Vendedor selected)
                {
                    var name = selected.Nome?.Trim().ToUpperInvariant() ?? string.Empty;
                    NomeEntry.TextChanged -= OnNomeTextChanged;
                    NomeEntry.Text = name;
                    NomeEntry.CursorPosition = name.Length;
                    NomeEntry.SelectionLength = 0;
                    NomeEntry.TextChanged += OnNomeTextChanged;
                    HideError();
                }
            }
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex);
            await GlobalToast.ShowAsync("Erro ao carregar vendedores.", ToastTipo.Erro, 2500);
        }
        finally
        {
            _isBusy = false;
            SaveButton.IsEnabled = true;
            CancelButton.IsEnabled = true;
            if (ListButton is not null)
                ListButton.IsEnabled = true;
        }
    }
}
