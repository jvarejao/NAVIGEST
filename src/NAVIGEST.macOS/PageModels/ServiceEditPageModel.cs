using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Maui.Views;
using NAVIGEST.macOS.Models;
using NAVIGEST.macOS.Popups;
using NAVIGEST.macOS.Services;

namespace NAVIGEST.macOS.PageModels;

public class ServiceEditPageModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new(n));

    // Header Fields
    private Cliente? _selectedClient;
    public Cliente? SelectedClient
    {
        get => _selectedClient;
        set
        {
            _selectedClient = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ClientCode));
            OnPropertyChanged(nameof(ClientName));
            OnPropertyChanged(nameof(ShowFinancials));
            OnPropertyChanged(nameof(FinancialColumnWidth));

            foreach (var item in Items)
            {
                item.ShowFinancials = ShowFinancials;
                item.FinancialColumnWidth = FinancialColumnWidth;
            }
        }
    }

    public string ClientCode => SelectedClient?.CLICODIGO ?? "N/A";
    public string ClientName => SelectedClient?.CLINOME ?? "Selecione um Cliente";

    public DateTime CreationDate { get; private set; } = DateTime.Now;

    private DateTime _deliveryDate = DateTime.Now.AddDays(7);
    public DateTime DeliveryDate
    {
        get => _deliveryDate;
        set { _deliveryDate = value; OnPropertyChanged(); }
    }

    private string _status = "Orçamento";
    public string Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); }
    }
    private static readonly List<ServiceStatus> DefaultStatusOptions = new()
    {
        new() { Descricao = "Orçamento", Cor = "#6B7280" },
        new() { Descricao = "Adjudicado", Cor = "#2563EB" },
        new() { Descricao = "Em Produção", Cor = "#0EA5E9" },
        new() { Descricao = "Terminado", Cor = "#22C55E" },
        new() { Descricao = "Entregue", Cor = "#F59E0B" }
    };
    public List<ServiceStatus> StatusOptions { get; private set; } = new(DefaultStatusOptions);
    private bool _statusOptionsLoaded;

    private string _observations = "";
    public string Observations
    {
        get => _observations;
        set { _observations = value; OnPropertyChanged(); }
    }

    private string _internalObservations = "";
    public string InternalObservations
    {
        get => _internalObservations;
        set { _internalObservations = value; OnPropertyChanged(); }
    }

    public bool IsAdminOrFinance => UserSession.Current.User?.IsFinancial ?? false;

    public bool ShowFinancials
    {
        get
        {
            var user = UserSession.Current.User;
            if (user == null) return false;
            if (user.IsAdmin || user.IsFinancial) return true;

            if (string.Equals(user.Role, "VENDEDOR", StringComparison.OrdinalIgnoreCase))
            {
                if (SelectedClient != null && !string.IsNullOrWhiteSpace(SelectedClient.VENDEDOR) &&
                    string.Equals(SelectedClient.VENDEDOR.Trim(), user.Name.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }

    public GridLength FinancialColumnWidth => ShowFinancials ? new GridLength(90) : new GridLength(0);

    // Lines
    public ObservableCollection<OrderedProductViewModel> Items { get; } = new();
    public int ItemsCount => Items.Count;

    // Totals
    public decimal SubTotal => Items.Sum(i => i.Subtotal);

    private string _discountInput = "0";
    private decimal _discountPercentage;
    public string DiscountInput
    {
        get => _discountInput;
        set
        {
            if (_discountInput == value) return;
            _discountInput = value ?? "0";

            if (decimal.TryParse(_discountInput.Replace(".", ","), NumberStyles.Any, new CultureInfo("pt-PT"), out var parsed))
            {
                _discountPercentage = parsed;
                OnPropertyChanged(nameof(DiscountPercentage));
                RecalculateTotals();
            }

            OnPropertyChanged();
        }
    }

    public decimal DiscountPercentage => _discountPercentage;

    public decimal DiscountAmount => Math.Max(0, SubTotal * DiscountPercentage / 100m);

    public decimal SubTotalAfterDiscount => Math.Max(0, SubTotal - DiscountAmount);

    private decimal _taxRate = 0.23m; // Default IVA
    public decimal TaxRate
    {
        get => _taxRate;
        set
        {
            if (_taxRate == value) return;
            _taxRate = value;
            OnPropertyChanged();
            RecalculateTotals();
        }
    }

    public decimal TaxAmount => SubTotalAfterDiscount * TaxRate;
    public decimal TotalAmount => SubTotalAfterDiscount + TaxAmount;

    // Commands
    public ICommand SelectClientCommand { get; }
    public ICommand OpenStatusPickerCommand { get; }
    public ICommand AddLineCommand { get; }
    public ICommand RemoveLineCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public ServiceEditPageModel()
    {
        Items.CollectionChanged += OnItemsCollectionChanged;
        SelectClientCommand = new Command(OnSelectClient);
        OpenStatusPickerCommand = new Command(OnOpenStatusPicker);
        AddLineCommand = new Command(OnAddLine);
        RemoveLineCommand = new Command<OrderedProductViewModel>(OnRemoveLine);
        SaveCommand = new Command(OnSave);
        CancelCommand = new Command(OnCancel);

        _ = LoadStatusOptionsAsync();
    }

    public ServiceEditPageModel(OrderInfoModel existingOrder) : this()
    {
        if (existingOrder == null) return;

        // Prefill basic fields when navigating from the list
        SelectedClient = new Cliente
        {
            CLICODIGO = existingOrder.CustomerNo,
            CLINOME = existingOrder.CustomerName,
            VENDEDOR = existingOrder.CONTROLVEND
        };

        CreationDate = existingOrder.OrderDate ?? CreationDate;
        DeliveryDate = existingOrder.OrderDateEnt ?? DeliveryDate;
        Status = string.IsNullOrWhiteSpace(existingOrder.OrderStatus) ? Status : existingOrder.OrderStatus;
        Observations = existingOrder.Observacoes ?? string.Empty;
        InternalObservations = existingOrder.DESCPROD ?? string.Empty;

        // Financials
        if (existingOrder.TaxPercentage.HasValue)
        {
            var tax = existingOrder.TaxPercentage.Value;
            // DB guarda percentagem (ex: 23). Converter para fração (0.23) se vier > 1.
            TaxRate = tax > 1 ? tax / 100m : tax;
        }

        if (existingOrder.DescPercentage.HasValue)
        {
            _discountPercentage = existingOrder.DescPercentage.Value;
            _discountInput = _discountPercentage.ToString("N2", new CultureInfo("pt-PT"));
            OnPropertyChanged(nameof(DiscountInput));
            OnPropertyChanged(nameof(DiscountPercentage));
        }

        LoadExistingOrderAsync(existingOrder);
    }

    private async void LoadExistingOrderAsync(OrderInfoModel existingOrder)
    {
        try
        {
            var products = await DatabaseService.GetOrderedProductsExtendedAsync(existingOrder);

            if (products.Count == 0)
            {
                await AppShell.DisplayToastAsync("Sem produtos associados a este serviço.", NAVIGEST.macOS.ToastTipo.Aviso, 2500);
            }
            else
            {
                foreach (var ordered in products)
                {
                    var item = new OrderedProductViewModel(ordered)
                    {
                        ShowFinancials = ShowFinancials,
                        FinancialColumnWidth = FinancialColumnWidth
                    };
                    item.PropertyChanged += OnItemPropertyChanged;
                    Items.Add(item);
                }

                RecalculateTotals();
                OnPropertyChanged(nameof(ItemsCount));
            }
        }
        catch (Exception ex)
        {
            NAVIGEST.macOS.GlobalErro.TratarErro(ex);
            await AppShell.DisplayToastAsync("Erro ao carregar produtos do serviço.", NAVIGEST.macOS.ToastTipo.Erro, 2500);
        }
    }

    private async void OnOpenStatusPicker()
    {
        await LoadStatusOptionsAsync();
        if (StatusOptions.Count == 0) return;

        var popup = new StatusPickerPopup(StatusOptions);
        var result = await AppShell.Current.ShowPopupAsync(popup);
        if (result is string status)
        {
            Status = status;
        }
    }

    private async Task LoadStatusOptionsAsync()
    {
        if (_statusOptionsLoaded && StatusOptions.Count > 0) return;

        try
        {
            var statuses = await DatabaseService.GetServiceStatusAsync();

            var options = statuses
                .Where(s => s != null && !string.IsNullOrWhiteSpace(s.Descricao))
                .GroupBy(s => s.Descricao.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            if (options.Count == 0)
            {
                options = new(DefaultStatusOptions);
            }

            StatusOptions = options;
            _statusOptionsLoaded = true;
            OnPropertyChanged(nameof(StatusOptions));

            // Ajustar o estado pré-selecionado se não existir na lista carregada
            if (string.IsNullOrWhiteSpace(Status) || !StatusOptions.Any(o => string.Equals(o.Descricao, Status, StringComparison.OrdinalIgnoreCase)))
            {
                Status = StatusOptions.FirstOrDefault()?.Descricao ?? Status;
            }
        }
        catch (Exception ex)
        {
            NAVIGEST.macOS.GlobalErro.TratarErro(ex);

            if (StatusOptions.Count == 0)
            {
                StatusOptions = new(DefaultStatusOptions);
                OnPropertyChanged(nameof(StatusOptions));
            }
        }
    }

    private async void OnSelectClient()
    {
        var popup = new ClientPickerPopup();
        var result = await AppShell.Current.ShowPopupAsync(popup);
        if (result is Cliente client)
        {
            SelectedClient = client;
        }
    }

    private async void OnAddLine()
    {
        var popup = new ProductPickerPopup(SelectedClient);
        var result = await AppShell.Current.ShowPopupAsync(popup);
        if (result is Product product)
        {
            var newItem = new OrderedProductViewModel(product);
            newItem.ShowFinancials = ShowFinancials;
            newItem.FinancialColumnWidth = FinancialColumnWidth;
            newItem.PropertyChanged += OnItemPropertyChanged;
            Items.Add(newItem);
            RecalculateTotals();
            OnPropertyChanged(nameof(ItemsCount));
        }
    }

    private void OnRemoveLine(OrderedProductViewModel? item)
    {
        if (item != null)
        {
            item.PropertyChanged -= OnItemPropertyChanged;
            Items.Remove(item);
            RecalculateTotals();
            OnPropertyChanged(nameof(ItemsCount));
        }
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OrderedProductViewModel.Subtotal))
        {
            RecalculateTotals();
        }
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(ItemsCount));
    }

    private void RecalculateTotals()
    {
        OnPropertyChanged(nameof(SubTotal));
        OnPropertyChanged(nameof(DiscountAmount));
        OnPropertyChanged(nameof(SubTotalAfterDiscount));
        OnPropertyChanged(nameof(TaxAmount));
        OnPropertyChanged(nameof(TotalAmount));
    }

    private async void OnSave()
    {
        if (SelectedClient == null)
        {
            await AppShell.Current.DisplayAlert("Erro", "Selecione um cliente.", "OK");
            return;
        }

        if (Items.Count == 0)
        {
            await AppShell.Current.DisplayAlert("Erro", "Adicione pelo menos um produto.", "OK");
            return;
        }

        // TODO: Map to OrderInfoModel and Save to DB
        await AppShell.Current.DisplayAlert("Sucesso", "Serviço guardado (Simulação).", "OK");
        await AppShell.Current.Navigation.PopAsync();
    }

    private async void OnCancel()
    {
        await AppShell.Current.Navigation.PopAsync();
    }
}

public class OrderedProductViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new(n));

    private GridLength _financialColumnWidth = new GridLength(0);
    public GridLength FinancialColumnWidth
    {
        get => _financialColumnWidth;
        set { _financialColumnWidth = value; OnPropertyChanged(); }
    }

    private bool _showFinancials;
    public bool ShowFinancials
    {
        get => _showFinancials;
        set { _showFinancials = value; OnPropertyChanged(); }
    }

    public Product Product { get; }

    public string ProductCode => Product.PRODCODIGO ?? "";
    public string ProductName => Product.PRODNOME ?? "";

    private string _color = "";
    public string Color { get => _color; set { _color = value; OnPropertyChanged(); } }

    private string _size = "";
    public string Size { get => _size; set { _size = value; OnPropertyChanged(); } }

    private decimal _quantity = 1;
    public decimal Quantity
    {
        get => _quantity;
        set 
        { 
            _quantity = value; 
            OnPropertyChanged(); 
            RecalculateM2();
            CalculateTotal(); 
        }
    }

    private decimal _height;
    public decimal Height
    {
        get => _height;
        set 
        { 
            if (_height != value)
            {
                _height = value; 
                OnPropertyChanged(); 
                RecalculateM2();
            }
        }
    }

    private decimal _width;
    public decimal Width
    {
        get => _width;
        set 
        { 
            if (_width != value)
            {
                _width = value; 
                OnPropertyChanged(); 
                RecalculateM2();
            }
        }
    }

    private decimal _m2;
    public decimal M2
    {
        get => _m2;
        set 
        { 
            if (_m2 != value)
            {
                _m2 = value; 
                OnPropertyChanged(); 
                CalculateTotal(); 
            }
        }
    }

    private string _unitPriceDisplayRaw = "";
    private bool _suppressRawReset = false;

    public string UnitPriceDisplay
    {
        get => _unitPriceDisplayRaw;
        set
        {
            if (_unitPriceDisplayRaw != value)
            {
                _unitPriceDisplayRaw = value;
                OnPropertyChanged();

                // Try to parse and update the actual decimal UnitPrice without formatting back
                // We replace . with , to allow user to type 2.50 and have it parsed as 2,50
                if (decimal.TryParse(value.Replace(".", ","), System.Globalization.NumberStyles.Any, new System.Globalization.CultureInfo("pt-PT"), out decimal result))
                {
                    _suppressRawReset = true;
                    UnitPrice = result;
                    _suppressRawReset = false;
                }
            }
        }
    }

    public void FormatUnitPriceOnUnfocus()
    {
        // 1. Sanitize: Replace . with ,
        string sanitized = _unitPriceDisplayRaw.Replace(".", ",");
        
        // 2. Parse
        if (decimal.TryParse(sanitized, System.Globalization.NumberStyles.Any, new System.Globalization.CultureInfo("pt-PT"), out decimal result))
        {
            // 3. Update Model
            UnitPrice = result;
            
            // 4. Format Display (C2)
            _unitPriceDisplayRaw = result.ToString("C2", new System.Globalization.CultureInfo("pt-PT"));
            OnPropertyChanged(nameof(UnitPriceDisplay));
        }
    }

    private decimal _unitPrice;
    public decimal UnitPrice
    {
        get => _unitPrice;
        set 
        { 
            if (_unitPrice != value)
            {
                _unitPrice = value; 
                OnPropertyChanged(); 
                CalculateTotal(); 
                
                if (!_suppressRawReset)
                {
                     _unitPriceDisplayRaw = _unitPrice.ToString("C2", new System.Globalization.CultureInfo("pt-PT"));
                     OnPropertyChanged(nameof(UnitPriceDisplay));
                }
            }
        }
    }

    private decimal _subtotal;
    public decimal Subtotal
    {
        get => _subtotal;
        private set { _subtotal = value; OnPropertyChanged(); }
    }

    public ICommand SelectColorCommand { get; }
    public ICommand SelectSizeCommand { get; }

    public OrderedProductViewModel(Product product)
    {
        Product = product;
        UnitPrice = product.PRECOVENDA;
        _unitPriceDisplayRaw = UnitPrice.ToString("C2", new System.Globalization.CultureInfo("pt-PT"));
        
        SelectColorCommand = new Command(OnSelectColor);
        SelectSizeCommand = new Command(OnSelectSize);
        
        CalculateTotal();
    }

    public OrderedProductViewModel(OrderedProduct ordered)
    {
        Product = new Product
        {
            PRODCODIGO = ordered.ProductCode,
            PRODNOME = ordered.ProductName,
            PRECOCUSTO = ordered.PrecoCusto,
            PRECOVENDA = ordered.PrecoUnit
        };

        SelectColorCommand = new Command(OnSelectColor);
        SelectSizeCommand = new Command(OnSelectSize);

        // Use property setters to refresh bindings and calculations
        Color = ordered.Cor;
        Size = ordered.Tam;
        Quantity = ordered.Quantidade <= 0 ? 1 : ordered.Quantidade;
        Height = ordered.Altura;
        Width = ordered.Largura;
        _suppressRawReset = true;
        // Use selling price when present; fallback to cost if selling price is zero
        UnitPrice = ordered.PrecoUnit > 0 ? ordered.PrecoUnit : ordered.PrecoCusto;
        _suppressRawReset = false;
        _unitPriceDisplayRaw = UnitPrice.ToString("C2", new System.Globalization.CultureInfo("pt-PT"));
        OnPropertyChanged(nameof(UnitPriceDisplay));

        // If M2 came from DB, honor it and recalc totals; otherwise recalc from dimensions
        if (ordered.M2 > 0)
        {
            M2 = ordered.M2;
        }
        else
        {
            RecalculateM2();
        }

        // If subtotal came from DB, set it directly (prefer cost subtotal when selling subtotal is zero)
        var dbSubtotal = ordered.SUBTOTAIS > 0 ? ordered.SUBTOTAIS : ordered.SubtotalCusto;
        if (dbSubtotal > 0)
        {
            Subtotal = dbSubtotal;
        }
        else
        {
            CalculateTotal();
        }
    }

    private async void OnSelectColor()
    {
        var popup = new ColorPickerPopup();
        var result = await AppShell.Current.ShowPopupAsync(popup);
        if (result is Cor cor)
        {
            Color = cor.NomeCor;
        }
    }

    private async void OnSelectSize()
    {
        var popup = new SizePickerPopup();
        var result = await AppShell.Current.ShowPopupAsync(popup);
        if (result is Tamanho tamanho)
        {
            Size = tamanho.NomeTamanho;
        }
    }

    private void RecalculateM2()
    {
        if (Height > 0 && Width > 0)
        {
            M2 = Height * Width * Quantity;
        }
        // If H or W is 0, we don't force M2 to 0, allowing manual entry if needed? 
        // Or we force it? Usually if dimensions change to 0, M2 should be 0.
        else if (Height == 0 || Width == 0)
        {
             // Only reset M2 if it was previously calculated from dimensions?
             // For simplicity, if user touches dimensions, we update M2.
             // If they want manual M2, they should leave dimensions at 0 (or set them) then edit M2.
             // But if they set H=0, M2 becomes 0.
             M2 = 0;
        }
    }

    private void CalculateTotal()
    {
        // Logic:
        // If M2 > 0 -> Total = M2 * UnitPrice (M2 already includes Quantity)
        // Else -> Total = Quantity * UnitPrice

        if (M2 > 0)
        {
            // Area based calculation
            Subtotal = M2 * UnitPrice;
        }
        else
        {
            // Unit based calculation
            Subtotal = Quantity * UnitPrice;
        }
    }
}