using System;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.ApplicationModel;
using NAVIGEST.iOS;
using NAVIGEST.iOS.Models;
using NAVIGEST.iOS.Services;

namespace NAVIGEST.iOS.Popups;

public partial class AddVendedorPopup : Popup
{
    private bool _isBusy;

    public AddVendedorPopup()
    {
        InitializeComponent();
        Opened += OnPopupOpened;
        NomeEntry.TextChanged += (_, _) => HideError();
    }

    private void OnPopupOpened(object? sender, PopupOpenedEventArgs e) => _ = InitializeAsync();

    private async Task InitializeAsync()
    {
        try
        {
            SetBusy(true);
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
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (_isBusy)
            return;

        var nome = NomeEntry.Text?.Trim();
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
}
