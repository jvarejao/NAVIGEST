using Microsoft.Maui.Controls;
using NAVIGEST.macOS.Models;
using NAVIGEST.macOS.Services;
using NAVIGEST.Shared.Resources.Strings;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

#if MACCATALYST
using UIKit;
using Foundation;
using CoreGraphics;
#endif

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
        // Only fetch if we don't have a Company Name OR Logo (UserSession should have it from WelcomePage)
        var user = UserSession.Current.User;
        if (user == null || string.IsNullOrEmpty(user.CompanyName) || user.CompanyLogo == null || user.CompanyLogo.Length == 0)
        {
            try 
            {
                var companies = await DatabaseService.GetActiveCompaniesAsync();
                
                // FIX: Obter a empresa selecionada na WelcomePage (via Preferences)
                var selectedCode = Microsoft.Maui.Storage.Preferences.Default.Get("company.code", string.Empty);
                var company = companies.FirstOrDefault(c => c.CodEmp == selectedCode);

                // Fallback apenas se não encontrar a seleção (ex: erro de estado), mas tenta honrar a escolha do utilizador
                if (company == null) 
                {
                    company = companies.FirstOrDefault();
                }

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
            DebugInfo += $"Logo encontrado: {CurrentUser.CompanyLogo.Length} bytes.\n";
            CompanyLogoSource = ImageSource.FromStream(() => new MemoryStream(CurrentUser.CompanyLogo));
            OnPropertyChanged(nameof(CompanyLogoSource));
        }
        else
        {
            DebugInfo += "Logo NÃO encontrado ou vazio no UserSession.\n";
        }

        // 2. Load Products
        await LoadProducts();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnPrintClicked(object sender, EventArgs e)
    {
#if MACCATALYST
        try
        {
            if (InvoiceSheet.Handler?.PlatformView is UIView platformView)
            {
                var printInfo = UIPrintInfo.PrintInfo;
                printInfo.OutputType = UIPrintInfoOutputType.General;
                printInfo.JobName = $"Encomenda_{Order.OrderNo}";
                printInfo.Orientation = UIPrintInfoOrientation.Portrait;

                var printer = UIPrintInteractionController.SharedPrintController;
                printer.PrintInfo = printInfo;
                
                // Use ViewPrintFormatter to print the specific view
                var renderer = new UIPrintPageRenderer();
                renderer.AddPrintFormatter(platformView.ViewPrintFormatter, 0);
                printer.PrintPageRenderer = renderer;

                printer.Present(true, (handler, completed, error) => {
                    if (error != null)
                    {
                        MainThread.BeginInvokeOnMainThread(() => 
                            DisplayAlert(AppResources.Common_Error, string.Format(AppResources.Print_Error, error.LocalizedDescription), AppResources.Common_OK));
                    }
                });
            }
            else
            {
                await DisplayAlert(AppResources.Common_Error, AppResources.Print_ViewAccessError, AppResources.Common_OK);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert(AppResources.Common_Error, string.Format(AppResources.Print_StartError, ex.Message), AppResources.Common_OK);
        }
#else
        await DisplayAlert(AppResources.Common_Warning, AppResources.Feature_MacIOSOnly, AppResources.Common_OK);
#endif
    }

    #if MACCATALYST
    private NSData? GeneratePdfData()
    {
        if (InvoiceSheet.Handler?.PlatformView is UIView platformView)
        {
            // A4 size: 595 x 842 points
            var pdfRect = new CGRect(0, 0, 595, 842);
            var data = new NSMutableData();

            UIGraphics.BeginPDFContext(data, pdfRect, null);
            UIGraphics.BeginPDFPage();
            
            var ctx = UIGraphics.GetCurrentContext();
            
            // 1. Fill page with white background (fixes black background issue)
            ctx.SetFillColor(UIColor.White.CGColor);
            ctx.FillRect(pdfRect);

            // Calculate scale to fit the view into A4 width with margins
            var viewBounds = platformView.Bounds;
            var availableWidth = 555f; // 595 - 20 - 20
            var scale = availableWidth / viewBounds.Width;
            
            ctx.SaveState();
            
            // Apply margins and scale
            ctx.TranslateCTM(20, 20);
            ctx.ScaleCTM((nfloat)scale, (nfloat)scale);
            
            // Render the view
            // DrawViewHierarchy is often more reliable for capturing the full visual state including subviews
            // It handles transparency and complex hierarchies better than RenderInContext
            if (!platformView.DrawViewHierarchy(platformView.Bounds, true))
            {
                // Fallback to RenderInContext if DrawViewHierarchy fails
                Console.WriteLine("DrawViewHierarchy failed, falling back to RenderInContext");
                platformView.Layer.RenderInContext(ctx);
            }
            
            ctx.RestoreState();
            
            UIGraphics.EndPDFContext();
            return data;
        }
        return null;
    }
#endif

    private async void OnSavePdfClicked(object sender, EventArgs e)
    {
#if MACCATALYST
        try
        {
            var data = GeneratePdfData();
            if (data != null)
            {
                // 1. Get Client Folder
                var clientFolder = await FolderService.GetClientFolderPathAsync(Order.CustomerName, Order.CustomerNo);
                
                if (string.IsNullOrEmpty(clientFolder))
                {
                    bool create = await DisplayAlert(AppResources.Pdf_FolderNotFound, AppResources.Pdf_FolderNotFoundMessage, AppResources.Common_Yes, AppResources.Common_No);
                    if (create)
                    {
                        var client = new Cliente { CLINOME = Order.CustomerName, CLICODIGO = Order.CustomerNo };
                        var (success, msg) = await FolderService.CreateClientFoldersAsync(client);
                        if (success)
                        {
                            // Try to get path again
                            clientFolder = await FolderService.GetClientFolderPathAsync(Order.CustomerName, Order.CustomerNo);
                            if (string.IsNullOrEmpty(clientFolder))
                            {
                                await DisplayAlert(AppResources.Common_Error, string.Format(AppResources.Pdf_FolderCreatedButNotFound, msg), AppResources.Common_OK);
                                return;
                            }
                        }
                        else
                        {
                            await DisplayAlert(AppResources.Common_Error, msg, AppResources.Common_OK);
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }
                }

                // 2. Save File
                var fileName = $"Encomenda_{Order.OrderNo}.pdf";
                var filePath = System.IO.Path.Combine(clientFolder, fileName);
                
                if (System.IO.File.Exists(filePath))
                {
                    bool overwrite = await DisplayAlert(AppResources.Pdf_FileExists, AppResources.Pdf_FileExistsMessage, AppResources.Common_Yes, AppResources.Common_No);
                    if (!overwrite) return;
                }

                data.Save(filePath, true);
                
                await DisplayAlert(AppResources.Common_Success, string.Format(AppResources.Pdf_SavedSuccess, filePath), AppResources.Common_OK);
                
                // Open folder
                try 
                {
                    System.Diagnostics.Process.Start("open", $"{clientFolder}");
                }
                catch {}
            }
            else
            {
                await DisplayAlert(AppResources.Common_Error, AppResources.Pdf_GenerationError, AppResources.Common_OK);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert(AppResources.Common_Error, string.Format(AppResources.Pdf_SaveError, ex.Message), AppResources.Common_OK);
        }
#else
        await DisplayAlert(AppResources.Common_Warning, AppResources.Feature_MacOSOnly, AppResources.Common_OK);
#endif
    }

    private async void OnSharePdfClicked(object sender, EventArgs e)
    {
#if MACCATALYST
        try
        {
            var data = GeneratePdfData();
            if (data != null)
            {
                var fileName = $"Encomenda_{Order.OrderNo}.pdf";
                var fn = System.IO.Path.Combine(FileSystem.CacheDirectory, fileName);
                data.Save(fn, true);
                
                await Share.RequestAsync(new ShareFileRequest
                {
                    Title = AppResources.Pdf_ShareTitle,
                    File = new ShareFile(fn)
                });
            }
            else
            {
                await DisplayAlert(AppResources.Common_Error, AppResources.Pdf_GenerationError, AppResources.Common_OK);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert(AppResources.Common_Error, string.Format(AppResources.Pdf_ShareError, ex.Message), AppResources.Common_OK);
        }
#else
        await DisplayAlert(AppResources.Common_Warning, AppResources.Feature_MacOSOnly, AppResources.Common_OK);
#endif
    }

    private async Task LoadProducts()
    {
        try
        {
            DebugInfo += $"A carregar produtos para a Encomenda Nº: '{Order.OrderNo}'\n";
            
            // Run Debug Check
            var debugResult = await DatabaseService.DebugCheckOrderAsync(Order.OrderNo);
            DebugInfo += debugResult + "\n";

            // Tenta carregar produtos usando OrderNo, Numserv ou Servencomenda
            var list = await DatabaseService.GetOrderedProductsExtendedAsync(Order);
            DebugInfo += $"GetOrderedProductsExtendedAsync retornou {list.Count} itens.\n";
            
            foreach (var item in list)
            {
                Products.Add(item);
            }
        }
        catch (System.Exception ex)
        {
            DebugInfo += $"Erro: {ex.Message}\n";
            await DisplayAlert(AppResources.Common_Error, string.Format(AppResources.Products_LoadError, ex.Message), AppResources.Common_OK);
        }
    }

    private async void OnDebugClicked(object sender, EventArgs e)
    {
        try
        {
            var report = await DatabaseService.DebugRawSearchAsync(Order.OrderNo);
            await DisplayAlert("Diagnóstico SQL", report, "OK");
            DebugInfo += "\n" + report;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", ex.Message, "OK");
        }
    }
}
