#nullable enable
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Maui.Controls;
using NAVIGEST.macOS.Models;
using NAVIGEST.macOS.Services;
using System;
using System.Diagnostics;

namespace NAVIGEST.macOS.PageModels;

public class ServicePageModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new(n));

    private readonly List<OrderInfoModel> _all = new();
    public ObservableCollection<OrderInfoModel> Orders { get; } = new();

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText == value) return;
            _searchText = value ?? string.Empty;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Filter));
            DebounceSearch();
        }
    }
    public string Filter { get => SearchText; set { if (SearchText != value) { SearchText = value ?? string.Empty; OnPropertyChanged(nameof(Filter)); } } }

    private bool _isBusy;
    public bool IsBusy { get => _isBusy; set { if (_isBusy != value) { _isBusy = value; OnPropertyChanged(); } } }

    public Command NewCommand { get; }
    public Command SearchCommand { get; }
    public Command RefreshCommand { get; }

    private CancellationTokenSource? _loadCts;
    private CancellationTokenSource? _searchCts;

    public ServicePageModel()
    {
        NewCommand = new Command(async () => await AppShell.DisplayToastAsync("Novo serviço…"));
        SearchCommand = new Command(ApplyFilterImmediate);
        RefreshCommand = new Command(async () => await LoadAsync(force: true));
    }

    public async Task LoadAsync(bool force = false)
    {
        if (IsBusy) return;
        IsBusy = true;

        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        var ct = _loadCts.Token;

        try
        {
            try
            {
                var (count, sampleNo, sampleCust) = await DatabaseService.DebugOrdersProbeAsync(ct);
                Debug.WriteLine($"[ServicePage] DebugOrdersProbeAsync => Count={count}, Sample={sampleNo} / {sampleCust}");
                await AppShell.DisplayToastAsync($"OrderInfo: {count} linhas. Exemplo: {sampleNo ?? "-"} / {sampleCust ?? "-"}", NAVIGEST.macOS.ToastTipo.Info, 2000);
            }
            catch (Exception exProbe)
            {
                Debug.WriteLine($"[ServicePage] Debug probe failed: {exProbe}");
                NAVIGEST.macOS.GlobalErro.TratarErro(exProbe);
                await AppShell.DisplayToastAsync("Erro a sondar OrderInfo (ver Output).", NAVIGEST.macOS.ToastTipo.Erro, 2500);
            }

            // Carrega da BD (sem dados de teste em memória)
            _all.Clear();
            List<OrderInfoModel> list = null!;
            try
            {
                list = await DatabaseService.GetOrdersLightAsync(null, ct);
                Debug.WriteLine($"[ServicePage] GetOrdersLightAsync returned: {list?.Count ?? 0}");
                await AppShell.DisplayToastAsync($"Serviços carregados: {list?.Count ?? 0}", NAVIGEST.macOS.ToastTipo.Info, 1400);
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("[ServicePage] Load cancelled");
                return; // cancelado
            }
            catch (Exception exLoad)
            {
                Debug.WriteLine($"[ServicePage] Error loading orders: {exLoad}");
                NAVIGEST.macOS.GlobalErro.TratarErro(exLoad);
                await AppShell.DisplayToastAsync("Erro ao carregar serviços (ver Output).", NAVIGEST.macOS.ToastTipo.Erro, 2000);
                list = new List<OrderInfoModel>();
            }

            if (list != null && list.Count > 0)
            {
                foreach (var o in list) _all.Add(o);

                Orders.Clear();
                foreach (var o in _all) Orders.Add(o);
            }
            else
            {
                Debug.WriteLine("[ServicePage] No orders returned from DB.");
                // Se desejar: manter Orders vazia para mostrar mensagem na UI
                // Já mostramos toast acima com count 0
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ServicePage] Unexpected error: {ex}");
            NAVIGEST.macOS.GlobalErro.TratarErro(ex);
            await NAVIGEST.macOS.AppShell.DisplayToastAsync("Erro ao carregar serviços.", NAVIGEST.macOS.ToastTipo.Erro, 2000);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyFilterImmediate()
    {
        var q = (SearchText ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(q)) { Repopulate(_all); return; }

        var filtered = _all.Where(s =>
            (s.OrderNo ?? string.Empty).ToLowerInvariant().Contains(q) ||
            (s.CustomerName ?? string.Empty).ToLowerInvariant().Contains(q) ||
            (s.OrderStatus ?? string.Empty).ToLowerInvariant().Contains(q));

        Repopulate(filtered);
    }

    private void DebounceSearch()
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;
        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(300, token);
                if (token.IsCancellationRequested) return;
                MainThread.BeginInvokeOnMainThread(ApplyFilterImmediate);
            }
            catch { }
        }, token);
    }

    private void Repopulate(IEnumerable<OrderInfoModel> items)
    {
        Orders.Clear();
        foreach (var it in items) Orders.Add(it);
    }
}
