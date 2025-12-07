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
    public UserSession.UserData CurrentUser => UserSession.Current.User;
    public ImageSource? CompanyLogoSource { get; private set; }
    
    private string _debugInfo = "";
    public string DebugInfo
    {
        get => _debugInfo;
        set { _debugInfo = value; OnPropertyChanged(); }
    }

    public ServiceDetailPage(OrderInfoModel order)
    {
        InitializeComponent();
        Order = order;
        
        BindingContext = this;
        InitializePageAsync();
    }

    private async void InitializePageAsync()
    {
        // 1. Ensure Company Info
        if (CurrentUser.CompanyLogo == null || CurrentUser.CompanyLogo.Length == 0)
        {
            try 
            {
                var companies = await DatabaseService.GetActiveCompaniesAsync();
                var company = companies.FirstOrDefault(); // Fallback to first active company
                if (company != null)
                {
                    if (UserSession.Current.User == null) UserSession.Current.User = new UserSession.UserData();
                    UserSession.Current.User.CompanyName = company.Empresa ?? "";
                    UserSession.Current.User.CompanyLogo = company.Logotipo;
                    UserSession.Current.User.CompanyAddress = company.Morada ?? "";
                    UserSession.Current.User.CompanyCity = company.Localidade ?? "";
                    UserSession.Current.User.CompanyZip = company.CodPostal ?? "";
                    UserSession.Current.User.CompanyNif = company.Nif ?? "";
                    
                    // Refresh bindings
                    OnPropertyChanged(nameof(CurrentUser));
                }
            }
            catch (Exception ex)
            {
                DebugInfo += $"Erro no Logo: {ex.Message}\n";
            }
        }

        if (CurrentUser.CompanyLogo != null && CurrentUser.CompanyLogo.Length > 0)
        {
            CompanyLogoSource = ImageSource.FromStream(() => new MemoryStream(CurrentUser.CompanyLogo));
            OnPropertyChanged(nameof(CompanyLogoSource));
        }

        // 2. Load Products
        await LoadProducts();
    }

    private async Task LoadProducts()
    {
        try
        {
            DebugInfo += $"A carregar produtos para a Encomenda Nº: '{Order.OrderNo}'\n";
            
            // Run Debug Check
            var debugResult = await DatabaseService.DebugCheckOrderAsync(Order.OrderNo);
            DebugInfo += debugResult + "\n";

            var list = await DatabaseService.GetOrderedProductsAsync(Order.OrderNo);
            DebugInfo += $"GetOrderedProductsAsync retornou {list.Count} itens.\n";
            
            foreach (var item in list)
            {
                Products.Add(item);
            }
        }
        catch (System.Exception ex)
        {
            DebugInfo += $"Erro: {ex.Message}\n";
            await DisplayAlert("Erro", "Falha ao carregar produtos: " + ex.Message, "OK");
        }
    }
}
