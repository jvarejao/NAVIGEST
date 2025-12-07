using Microsoft.Maui.Controls;
using NAVIGEST.macOS.Models;
using NAVIGEST.macOS.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace NAVIGEST.macOS.Pages;

public partial class ServiceDetailPage : ContentPage
{
    public OrderInfoModel Order { get; private set; }
    public ObservableCollection<OrderedProduct> Products { get; } = new();

    public ServiceDetailPage(OrderInfoModel order)
    {
        InitializeComponent();
        Order = order;
        BindingContext = this;
        LoadProducts();
    }

    private async void LoadProducts()
    {
        try
        {
            var list = await DatabaseService.GetOrderedProductsAsync(Order.OrderNo);
            foreach (var item in list)
            {
                Products.Add(item);
            }
        }
        catch (System.Exception ex)
        {
            await DisplayAlert("Erro", "Falha ao carregar produtos: " + ex.Message, "OK");
        }
    }
}
