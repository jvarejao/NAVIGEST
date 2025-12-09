using CommunityToolkit.Maui.Views;
using NAVIGEST.macOS.Models;
using NAVIGEST.macOS.Services;

namespace NAVIGEST.macOS.Popups;

public partial class ProductPickerPopup : Popup
{
    private List<Product> _allProducts = new();

    public ProductPickerPopup()
    {
        InitializeComponent();
        LoadProducts();
    }

    private async void LoadProducts()
    {
        LoadingIndicator.IsRunning = true;
        try
        {
            _allProducts = await DatabaseService.GetProductsAsync();
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
                (p.PRODNOME?.ToLower().Contains(lower) ?? false) || 
                (p.PRODCODIGO?.ToLower().Contains(lower) ?? false)).ToList();
        }
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Product selected)
        {
            Close(selected);
        }
    }
}