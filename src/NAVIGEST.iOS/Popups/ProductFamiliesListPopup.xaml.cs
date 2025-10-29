using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
using NAVIGEST.iOS;
using NAVIGEST.iOS.Models;
using NAVIGEST.iOS.Services;

namespace NAVIGEST.iOS.Popups;

public record ProductFamilyListResult(ProductFamilyOption? SelectedFamily, bool RefreshRequested);

public partial class ProductFamiliesListPopup : Popup
{
    private readonly List<ProductFamilyOption> _all = new();
    private bool _isBusy;
    private bool _refreshRequested;

    public ObservableCollection<ProductFamilyOption> Families { get; } = new();

    public ProductFamiliesListPopup()
    {
        InitializeComponent();
        BindingContext = this;
        Opened += OnPopupOpened;
    }

    private void OnPopupOpened(object? sender, PopupOpenedEventArgs e) => _ = LoadAsync();

    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            var families = await DatabaseService.GetProductFamiliesAsync();
            UpdateCache(families);
        });
    }

    private void UpdateCache(IEnumerable<ProductFamilyOption> families)
    {
        _all.Clear();
        _all.AddRange(families
            .GroupBy(f => f.Codigo, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(f => f.Codigo, StringComparer.OrdinalIgnoreCase));

        ApplyFilter(SearchBar.Text);
    }

    private void ApplyFilter(string? query)
    {
        var filter = (query ?? string.Empty).Trim();
        IEnumerable<ProductFamilyOption> filtered = _all;
        if (!string.IsNullOrWhiteSpace(filter))
        {
            filtered = _all.Where(f =>
                (f.Nome?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false) ||
                f.Codigo.Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        Families.Clear();
        foreach (var item in filtered.OrderBy(f => f.NomeDisplay, StringComparer.OrdinalIgnoreCase))
            Families.Add(item);
    }

    private async Task RunBusyAsync(Func<Task> work)
    {
        if (_isBusy)
            return;

        try
        {
            SetBusy(true);
            await work();
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool value)
    {
        _isBusy = value;
        BusyOverlay.IsVisible = value;
        BusyIndicator.IsRunning = value;
        SearchBar.IsEnabled = !value;
        FamiliesCollectionView.IsEnabled = !value;
        CloseButton.IsEnabled = !value;
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isBusy)
            return;

        ApplyFilter(e.NewTextValue);
    }

    private void CloseWithResult(ProductFamilyOption? option)
    {
        Close(new ProductFamilyListResult(option, _refreshRequested));
    }

    private void OnCloseClicked(object sender, EventArgs e)
    {
        if (_isBusy)
            return;

        CloseWithResult(null);
    }

    private void OnFamilyTapped(object? sender, TappedEventArgs e)
    {
        if (_isBusy)
            return;

        if (e.Parameter is ProductFamilyOption option)
        {
            CloseWithResult(option);
        }
        else if (sender is BindableObject bindable && bindable.BindingContext is ProductFamilyOption ctx)
        {
            CloseWithResult(ctx);
        }
    }

    private async void OnEditFamilyClicked(object sender, EventArgs e)
    {
        if (_isBusy)
            return;

        if (sender is not Button button || button.BindingContext is not ProductFamilyOption option)
            return;

        var initialValue = option.Nome?.Trim().ToUpperInvariant();

        var newDescription = await AppShell.Current.DisplayPromptAsync(
            "Editar família",
            "Descrição da família",
            accept: "Guardar",
            cancel: "Cancelar",
            initialValue: initialValue,
            maxLength: 120,
            keyboard: Keyboard.Create(KeyboardFlags.CapitalizeCharacter));

        if (newDescription is null)
            return;

        newDescription = newDescription.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(newDescription))
        {
            await GlobalToast.ShowAsync("Descrição obrigatória.", ToastTipo.Aviso, 2500);
            return;
        }

        await RunBusyAsync(async () =>
        {
            var saved = await DatabaseService.UpsertProductFamilyAsync(option.Codigo, newDescription);
            if (!saved)
            {
                await GlobalToast.ShowAsync("Não foi possível guardar a família.", ToastTipo.Erro, 2500);
                return;
            }

            _refreshRequested = true;
            ReplaceInCache(option.Codigo, newDescription);
            await GlobalToast.ShowAsync("Família atualizada.", ToastTipo.Sucesso, 1800);
        });
    }

    private async void OnDeleteFamilyClicked(object sender, EventArgs e)
    {
        if (_isBusy)
            return;

        if (sender is not Button button || button.BindingContext is not ProductFamilyOption option)
            return;

        var confirm = await AppShell.Current.DisplayAlert(
            "Eliminar família",
            $"Pretende eliminar '{option.NomeDisplay}'?",
            "Eliminar",
            "Cancelar");

        if (!confirm)
            return;

        await RunBusyAsync(async () =>
        {
            var deleted = await DatabaseService.DeleteProductFamilyAsync(option.Codigo);
            if (!deleted)
            {
                await GlobalToast.ShowAsync("Não foi possível eliminar a família.", ToastTipo.Erro, 2500);
                return;
            }

            _refreshRequested = true;
            RemoveFromCache(option.Codigo);
            await GlobalToast.ShowAsync("Família eliminada.", ToastTipo.Sucesso, 1800);
        });
    }

    private void ReplaceInCache(string codigo, string descricao)
    {
        var updated = new ProductFamilyOption(codigo, descricao);

        var existingIndex = _all.FindIndex(f => string.Equals(f.Codigo, codigo, StringComparison.OrdinalIgnoreCase));
        if (existingIndex >= 0)
            _all[existingIndex] = updated;
        else
            _all.Add(updated);

        ApplyFilter(SearchBar.Text);
    }

    private void RemoveFromCache(string codigo)
    {
        _all.RemoveAll(f => string.Equals(f.Codigo, codigo, StringComparison.OrdinalIgnoreCase));
        ApplyFilter(SearchBar.Text);
    }
}
