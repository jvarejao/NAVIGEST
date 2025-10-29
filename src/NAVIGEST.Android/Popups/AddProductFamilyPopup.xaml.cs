using System;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using NAVIGEST.Android.Services;
using NAVIGEST.Android.Models;

namespace NAVIGEST.Android.Popups;

public record ProductFamilyInput(string Codigo, string Descricao);

public partial class AddProductFamilyPopup : Popup
{
    private bool _isBusy;
    internal bool FamiliesDirty { get; private set; }

    public AddProductFamilyPopup()
    {
        InitializeComponent();
        Opened += OnPopupOpened;
    CodigoEntry.TextChanged += (_, _) => HideError();
    DescricaoEntry.TextChanged += OnDescricaoTextChanged;
    }

    private void OnPopupOpened(object? sender, PopupOpenedEventArgs e) => _ = InitializeAsync();

    private async Task InitializeAsync()
    {
        try
        {
            SetBusy(true);
            string nextCodigo;
            try
            {
                nextCodigo = await DatabaseService.PeekNextProductFamilyCodigoAsync();
            }
            catch
            {
                nextCodigo = "001";
            }

            CodigoEntry.Text = nextCodigo;
            MainThread.BeginInvokeOnMainThread(() => DescricaoEntry.Focus());
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex);
            await GlobalToast.ShowAsync("Erro ao preparar o formulário de família.", ToastTipo.Erro, 2500);
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

    private void OnSaveClicked(object sender, EventArgs e)
    {
        if (_isBusy)
            return;

    var codigo = CodigoEntry.Text?.Trim().ToUpperInvariant();
    var descricao = DescricaoEntry.Text?.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(descricao))
        {
            ShowError("Preencha código e descrição.");
            return;
        }

        try
        {
            SetBusy(true);
            HideError();
            FamiliesDirty = true;
            Close(new ProductFamilyInput(codigo, descricao));
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

    private void OnDescricaoTextChanged(object? sender, TextChangedEventArgs e)
    {
        HideError();

        if (sender is not Entry entry)
            return;

        var upper = e.NewTextValue?.ToUpperInvariant() ?? string.Empty;
        if (upper == e.NewTextValue)
            return;

    var caret = Math.Max(0, entry.CursorPosition);

        entry.TextChanged -= OnDescricaoTextChanged;
        entry.Text = upper;
        entry.CursorPosition = Math.Min(caret, upper.Length);
        entry.SelectionLength = 0;
        entry.TextChanged += OnDescricaoTextChanged;
    }

    private async void OnShowFamiliesClicked(object sender, EventArgs e)
    {
        if (_isBusy)
            return;

        try
        {
            FamiliesDirty = false;
            _isBusy = true;
            SaveButton.IsEnabled = false;
            CancelButton.IsEnabled = false;
            ListButton.IsEnabled = false;

            var popup = new ProductFamiliesListPopup();
            var result = await AppShell.Current.ShowPopupAsync(popup) as ProductFamilyListResult;

            if (result is null)
                return;

            if (result.RefreshRequested)
                FamiliesDirty = true;

            if (result.SelectedFamily is not null)
            {
                CodigoEntry.Text = result.SelectedFamily.Codigo;
                DescricaoEntry.Text = result.SelectedFamily.Nome?.ToUpperInvariant();
            }
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex);
            await GlobalToast.ShowAsync("Erro ao carregar famílias.", ToastTipo.Erro, 2500);
        }
        finally
        {
            _isBusy = false;
            SaveButton.IsEnabled = true;
            CancelButton.IsEnabled = true;
            ListButton.IsEnabled = true;
        }
    }
}
