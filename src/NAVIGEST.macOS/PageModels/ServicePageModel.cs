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
using System.Windows.Input;
using System.Collections.Generic;

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

    public ICommand NewCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ClearSearchCommand { get; }

    private CancellationTokenSource? _loadCts;
    private CancellationTokenSource? _searchCts;

    public ServicePageModel()
    {
        NewCommand = new Command(OnNewService);
        SearchCommand = new Command(ApplyFilterImmediate);
        RefreshCommand = new Command(async () => await LoadAsync(force: true));
        EditCommand = new Command<OrderInfoModel>(OnEditService);
        DeleteCommand = new Command<OrderInfoModel>(OnDeleteService);
        ClearSearchCommand = new Command(() => SearchText = string.Empty);
    }

    private async void OnNewService()
    {
        await AppShell.DisplayToastAsync("Novo serviço (Em desenvolvimento)");
    }

    private async void OnEditService(OrderInfoModel? service)
    {
        if (service == null) return;
        await AppShell.DisplayToastAsync($"Editar serviço {service.OrderNo} (Em desenvolvimento)");
    }

    private async void OnDeleteService(OrderInfoModel? service)
    {
        if (service == null) return;
        bool confirm = await AppShell.Current.DisplayAlert("Eliminar", $"Tem a certeza que deseja eliminar o serviço {service.OrderNo}?", "Sim", "Não");
        if (confirm)
        {
            // TODO: Call DatabaseService to delete
            _all.Remove(service);
            Orders.Remove(service);
            await AppShell.DisplayToastAsync($"Serviço {service.OrderNo} eliminado");
        }
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
            // Carrega da BD
            _all.Clear();
            List<OrderInfoModel> list = null!;
            try
            {
                list = await DatabaseService.GetOrdersLightAsync(null, ct);
                Debug.WriteLine($"[ServicePage] GetOrdersLightAsync returned: {list?.Count ?? 0}");
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
                _all.AddRange(list);
                ApplyFilterImmediate();
            }
            else
            {
                // PROBE DEBUG
                try 
                {
                    var probe = await DatabaseService.DebugOrdersProbeAsync(ct);
                    if (probe.Count > 0)
                    {
                         await AppShell.DisplayToastAsync($"DEBUG: Tabela tem {probe.Count} registos, mas query retornou 0.", NAVIGEST.macOS.ToastTipo.Aviso, 5000);
                    }
                    else
                    {
                         await AppShell.DisplayToastAsync($"DEBUG: Tabela OrderInfo vazia (0 registos).", NAVIGEST.macOS.ToastTipo.Info, 3000);
                    }
                }
                catch { }

                // Mock data fallback if DB is empty (for testing UI)
                if (_all.Count == 0)
                {
                    Debug.WriteLine("[ServicePage] Adding Mock Data because _all is empty.");
                    _all.Add(new OrderInfoModel 
                    { 
                        OrderNo = "TEST-001", 
                        CustomerName = "Cliente Teste (Mock)", 
                        OrderStatus = "Aberto", 
                        TotalAmount = 123.45m,
                        OrderDate = DateTime.Now 
                    });
                    ApplyFilterImmediate();
                    await AppShell.DisplayToastAsync("Nenhum dado na BD. Carregado 1 registo de teste.", NAVIGEST.macOS.ToastTipo.Aviso, 3000);
                }
            }
        }
        finally
        {
            IsBusy = false;
            _loadCts = null;
        }
    }

    private void DebounceSearch()
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        Task.Delay(300, token).ContinueWith(t =>
        {
            if (t.IsCanceled) return;
            MainThread.BeginInvokeOnMainThread(ApplyFilterImmediate);
        });
    }

    private void ApplyFilterImmediate()
    {
        var f = SearchText?.Trim().ToLowerInvariant() ?? "";
        
        var filtered = string.IsNullOrWhiteSpace(f) 
            ? _all 
            : _all.Where(x => 
                (x.OrderNo?.ToLowerInvariant().Contains(f) ?? false) ||
                (x.CustomerName?.ToLowerInvariant().Contains(f) ?? false) ||
                (x.OrderStatus?.ToLowerInvariant().Contains(f) ?? false)
            ).ToList();

        Orders.Clear();
        foreach (var item in filtered)
        {
            Orders.Add(item);
        }
    }
}
