using CommunityToolkit.Maui.Views;
using NAVIGEST.macOS.Models;
using NAVIGEST.macOS.Services;

namespace NAVIGEST.macOS.Popups;

public partial class ProductPickerPopup : Popup
{
    private List<ProductItemViewModel> _allProducts = new();
    private Cliente? _contextClient;

    public ProductPickerPopup(Cliente? client = null)
    {
        InitializeComponent();
        _contextClient = client;
        LoadProducts();
    }

    public bool ShowFinancials
    {
        get
        {
            var user = UserSession.Current.User;
            if (user == null) return false;
            if (user.IsAdmin || user.IsFinancial) return true;

            if (string.Equals(user.Role, "VENDEDOR", StringComparison.OrdinalIgnoreCase))
            {
                if (_contextClient != null && !string.IsNullOrWhiteSpace(_contextClient.VENDEDOR) &&
                    string.Equals(_contextClient.VENDEDOR.Trim(), user.Name.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }

    private async void LoadProducts()
    {
        LoadingIndicator.IsRunning = true;
        try
        {
            var products = await DatabaseService.GetProductsAsync();
            var show = ShowFinancials;
            _allProducts = products.Select(p => new ProductItemViewModel { Product = p, ShowPrice = show }).ToList();
            ProductsList.ItemsSource = _allProducts;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.NewTextValue))
        {
            ProductsList.ItemsSource = _allProducts;
        }
        else
        {
            var lower = e.NewTextValue.ToLower();
            ProductsList.ItemsSource = _allProducts.Where(p => 
                (p.Product.PRODNOME?.ToLower().Contains(lower) ?? false) || 
                (p.Product.PRODCODIGO?.ToLower().Contains(lower) ?? false)).ToList();
        }
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is ProductItemViewModel selected)
        {
            Close(selected.Product);
        }
    }
}

public class ProductItemViewModel
{
    public Product Product { get; set; } = new();
    public bool ShowPrice { get; set; }
}