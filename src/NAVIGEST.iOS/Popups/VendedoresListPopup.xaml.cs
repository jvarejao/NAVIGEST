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

public record VendedorListResult(Vendedor? SelectedVendedor, bool RefreshRequested);

public partial class VendedoresListPopup : Popup
{
    private readonly List<Vendedor> _all = new();
    private bool _isBusy;
    private bool _refreshRequested;

    public ObservableCollection<Vendedor> Vendedores { get; } = new();

    public VendedoresListPopup()
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
            var vendedores = await DatabaseService.GetVendedoresAsync();
            UpdateCache(vendedores);
        });
    }

    private void UpdateCache(IEnumerable<Vendedor> vendedores)
    {
        _all.Clear();
        foreach (var item in vendedores.Select(Normalize)
                                       .OrderBy(v => v.Nome, StringComparer.OrdinalIgnoreCase))
        {
            _all.Add(item);
        }

        ApplyFilter(SearchBar.Text);
    }

    private static Vendedor Normalize(Vendedor vendedor)
        => new()
        {
            Id = vendedor.Id,
            Nome = (vendedor.Nome ?? string.Empty).Trim().ToUpperInvariant()
        };

    private void ApplyFilter(string? query)
    {
        var filter = (query ?? string.Empty).Trim();
        IEnumerable<Vendedor> filtered = _all;

        if (!string.IsNullOrWhiteSpace(filter))
        {
            filtered = _all.Where(v =>
                (v.Nome?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false) ||
                v.Id.ToString().Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        Vendedores.Clear();
        foreach (var item in filtered)
            Vendedores.Add(item);
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
        VendedoresCollectionView.IsEnabled = !value;
        CloseButton.IsEnabled = !value;
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isBusy)
            return;

        ApplyFilter(e.NewTextValue);
    }

    private void CloseWithResult(Vendedor? vendedor)
    {
        Close(new VendedorListResult(vendedor, _refreshRequested));
    }

    private void OnCloseClicked(object sender, EventArgs e)
    {
        if (_isBusy)
            return;

        CloseWithResult(null);
    }

    private void OnVendedorTapped(object? sender, TappedEventArgs e)
    {
        if (_isBusy)
            return;

        if (e.Parameter is Vendedor vendedor)
        {
            CloseWithResult(vendedor);
        }
        else if (sender is BindableObject bindable && bindable.BindingContext is Vendedor ctx)
        {
            CloseWithResult(ctx);
        }
    }

    private async void OnEditVendedorClicked(object sender, EventArgs e)
    {
        if (_isBusy)
            return;

        if (sender is not Button button || button.BindingContext is not Vendedor vendedor)
            return;

        var initial = vendedor.Nome?.Trim().ToUpperInvariant();

        var novoNome = await AppShell.Current.DisplayPromptAsync(
            "Editar vendedor",
            "Nome do vendedor",
            accept: "Guardar",
            cancel: "Cancelar",
            initialValue: initial,
            maxLength: 120,
            keyboard: Keyboard.Create(KeyboardFlags.CapitalizeCharacter));

        if (novoNome is null)
            return;

        novoNome = novoNome.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(novoNome))
        {
            await GlobalToast.ShowAsync("Nome obrigatório.", ToastTipo.Aviso, 2500);
            return;
        }

        await RunBusyAsync(async () =>
        {
            var updated = await DatabaseService.UpdateVendedorAsync(vendedor.Id, novoNome);
            if (!updated)
            {
                await GlobalToast.ShowAsync("Não foi possível atualizar o vendedor.", ToastTipo.Erro, 2500);
                return;
            }

            _refreshRequested = true;
            ReplaceInCache(vendedor.Id, novoNome);
            await GlobalToast.ShowAsync("Vendedor atualizado.", ToastTipo.Sucesso, 1800);
        });
    }

    private async void OnDeleteVendedorClicked(object sender, EventArgs e)
    {
        if (_isBusy)
            return;

        if (sender is not Button button || button.BindingContext is not Vendedor vendedor)
            return;

        var confirm = await AppShell.Current.DisplayAlert(
            "Eliminar vendedor",
            $"Pretende eliminar '{vendedor.Nome}'?",
            "Eliminar",
            "Cancelar");

        if (!confirm)
            return;

        await RunBusyAsync(async () =>
        {
            var deleted = await DatabaseService.DeleteVendedorAsync(vendedor.Id);
            if (!deleted)
            {
                await GlobalToast.ShowAsync("Não foi possível eliminar o vendedor.", ToastTipo.Erro, 2500);
                return;
            }

            _refreshRequested = true;
            RemoveFromCache(vendedor.Id);
            await GlobalToast.ShowAsync("Vendedor eliminado.", ToastTipo.Sucesso, 1800);
        });
    }

    private void ReplaceInCache(int id, string nome)
    {
        var normalized = nome.Trim().ToUpperInvariant();
        var updated = new Vendedor { Id = id, Nome = normalized };

        var index = _all.FindIndex(v => v.Id == id);
        if (index >= 0)
            _all[index] = updated;
        else
            _all.Add(updated);

        ApplyFilter(SearchBar.Text);
    }

    private void RemoveFromCache(int id)
    {
        _all.RemoveAll(v => v.Id == id);
        ApplyFilter(SearchBar.Text);
    }
}
