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

    public static ServicePageModel? LastInstance { get; private set; }

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
    public ICommand ViewCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand PaymentCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ClearSearchCommand { get; }

    public string? FilterClientId { get; set; }
    public string? FilterClientName { get; set; }

    private CancellationTokenSource? _loadCts;
    private CancellationTokenSource? _searchCts;

    public ServicePageModel()
    {
        LastInstance = this;
        NewCommand = new Command(OnNewService);
        SearchCommand = new Command(ApplyFilterImmediate);
        RefreshCommand = new Command(async () => await LoadAsync(force: true));
        ViewCommand = new Command<OrderInfoModel>(OnViewService);
        EditCommand = new Command<OrderInfoModel>(OnEditService);
        PaymentCommand = new Command<OrderInfoModel>(OnPaymentService);
        DeleteCommand = new Command<OrderInfoModel>(OnDeleteService);
        ClearSearchCommand = new Command(() => SearchText = string.Empty);
    }

    private async void OnNewService()
    {
        // Navigate to the new ServiceEditPage
        await AppShell.Current.Navigation.PushAsync(new Pages.ServiceEditPage());
    }

    private async void OnViewService(OrderInfoModel? service)
    {
        if (service == null) return;
        try
        {
            await AppShell.Current.Navigation.PushAsync(new Pages.ServiceDetailPage(service, autoOpenPdf: true));
        }
        catch (Exception ex)
        {
            NAVIGEST.macOS.GlobalErro.TratarErro(ex);
        }
    }

    private async void OnEditService(OrderInfoModel? service)
    {
        if (service == null) return;
        try
        {
            await AppShell.Current.Navigation.PushAsync(new Pages.ServiceEditPage(service));
        }
        catch (Exception ex)
        {
            NAVIGEST.macOS.GlobalErro.TratarErro(ex);
            await AppShell.DisplayToastAsync("Erro ao abrir a edição do serviço.", NAVIGEST.macOS.ToastTipo.Erro, 2000);
        }
    }

    private async void OnPaymentService(OrderInfoModel? service)
    {
        if (service == null) return;
        await AppShell.DisplayToastAsync($"Registar pagamento para o serviço {service.OrderNo} (Em desenvolvimento)");
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
        var token = _loadCts.Token;

        try
        {
            // Carrega da BD
            _all.Clear();
            List<OrderInfoModel> list = null!;
            try
            {
                if (!string.IsNullOrEmpty(FilterClientId))
                {
                    list = await DatabaseService.GetOrdersByClientAsync(FilterClientId, token);
                }
                else
                {
                    list = await DatabaseService.GetOrdersLightAsync(null, token);
                }
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
                await AppShell.DisplayToastAsync("Erro ao carregar serviços.", NAVIGEST.macOS.ToastTipo.Erro, 2000);
                list = new List<OrderInfoModel>();
            }

            if (list != null && list.Count > 0)
            {
                _all.AddRange(list);
                ApplyFilterImmediate();
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
