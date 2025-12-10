using System.Collections.ObjectModel;
using System.ComponentModel;
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
        }
    }

    public string ClientCode => SelectedClient?.CLICODIGO ?? "N/A";
    public string ClientName => SelectedClient?.CLINOME ?? "Selecione um Cliente";

    public DateTime CreationDate { get; } = DateTime.Now;

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
    public List<string> StatusOptions { get; } = new() { "Orçamento", "Adjudicado", "Em Produção", "Terminado", "Entregue" };

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

    // Lines
    public ObservableCollection<OrderedProductViewModel> Items { get; } = new();

    // Totals
    public decimal SubTotal => Items.Sum(i => i.Subtotal);
    public decimal TaxRate { get; set; } = 0.23m; // Default IVA
    public decimal TaxAmount => SubTotal * TaxRate;
    public decimal TotalAmount => SubTotal + TaxAmount;

    // Commands
    public ICommand SelectClientCommand { get; }
    public ICommand AddLineCommand { get; }
    public ICommand RemoveLineCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public ServiceEditPageModel()
    {
        SelectClientCommand = new Command(OnSelectClient);
        AddLineCommand = new Command(OnAddLine);
        RemoveLineCommand = new Command<OrderedProductViewModel>(OnRemoveLine);
        SaveCommand = new Command(OnSave);
        CancelCommand = new Command(OnCancel);
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
        var popup = new ProductPickerPopup();
        var result = await AppShell.Current.ShowPopupAsync(popup);
        if (result is Product product)
        {
            var newItem = new OrderedProductViewModel(product);
            newItem.PropertyChanged += OnItemPropertyChanged;
            Items.Add(newItem);
            RecalculateTotals();
        }
    }

    private void OnRemoveLine(OrderedProductViewModel? item)
    {
        if (item != null)
        {
            item.PropertyChanged -= OnItemPropertyChanged;
            Items.Remove(item);
            RecalculateTotals();
        }
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OrderedProductViewModel.Subtotal))
        {
            RecalculateTotals();
        }
    }

    private void RecalculateTotals()
    {
        OnPropertyChanged(nameof(SubTotal));
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
        set { _quantity = value; OnPropertyChanged(); CalculateTotal(); }
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
            }
        }
    }

    private decimal _subtotal;
    public decimal Subtotal
    {
        get => _subtotal;
        private set { _subtotal = value; OnPropertyChanged(); }
    }

    public OrderedProductViewModel(Product product)
    {
        Product = product;
        UnitPrice = product.PRECOVENDA;
        CalculateTotal();
    }

    private void RecalculateM2()
    {
        if (Height > 0 && Width > 0)
        {
            M2 = Height * Width;
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
        // If M2 > 0 -> Total = M2 * Qty * UnitPrice
        // Else -> Total = Qty * UnitPrice

        if (M2 > 0)
        {
            // Area based calculation
            Subtotal = M2 * Quantity * UnitPrice;
        }
        else
        {
            // Unit based calculation
            Subtotal = Quantity * UnitPrice;
        }
    }
}