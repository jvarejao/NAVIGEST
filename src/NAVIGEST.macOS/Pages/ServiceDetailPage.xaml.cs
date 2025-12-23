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
using CoreAnimation;
#endif

namespace NAVIGEST.macOS.Pages;

public partial class ServiceDetailPage : ContentPage
{
    public OrderInfoModel Order { get; private set; }
    public ObservableCollection<OrderedProduct> Products { get; } = new();
    public UserSession.UserData CurrentUser => UserSession.Current.User;
    public ImageSource? CompanyLogoSource { get; private set; }
    public decimal DiscountAmount => CalculateDiscount();
    public decimal SubTotalAfterDiscount => Math.Max(0, (Order?.SubTotal ?? 0m) - DiscountAmount);
    public decimal CalculatedTaxAmount => CalculateTax();
    public decimal CalculatedTotalAmount => SubTotalAfterDiscount + CalculatedTaxAmount;
    public decimal PaidAmount => Order?.VALORPAGO ?? 0m;
    public decimal PendingAmount => CalculatePending();
    
    private bool _autoOpenPdf;

    public ServiceDetailPage(OrderInfoModel order, bool autoOpenPdf = false)
    {
        InitializeComponent();
        Order = order;
        _autoOpenPdf = autoOpenPdf;
        
        BindingContext = this;
        InitializePageAsync();
    }

    private decimal CalculateDiscount()
    {
        if (Order == null) return 0m;

        if (Order.Desconto.HasValue)
        {
            return Order.Desconto.Value;
        }

        if (Order.SubTotal.HasValue && Order.DescPercentage.HasValue)
        {
            return Order.SubTotal.Value * Order.DescPercentage.Value / 100m;
        }

        return 0m;
    }

    private decimal CalculateTax()
    {
        if (Order == null) return 0m;

        if (Order.TaxPercentage.HasValue)
        {
            return SubTotalAfterDiscount * Order.TaxPercentage.Value;
        }

        return Order.TaxAmount ?? 0m;
    }

    private decimal CalculatePending()
    {
        if (Order == null) return 0m;

        var paid = Order.VALORPAGO ?? 0m;
        var pending = Order.VALORPENDENTE ?? 0m;

        if (pending > 0) return pending;

        var total = CalculatedTotalAmount;
        var computedPending = total - paid;
        return computedPending < 0 ? 0 : computedPending;
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
                System.Diagnostics.Debug.WriteLine($"Erro no Logo: {ex.Message}");
            }
        }

        if (CurrentUser.CompanyLogo != null && CurrentUser.CompanyLogo.Length > 0)
        {
            CompanyLogoSource = ImageSource.FromStream(() => new MemoryStream(CurrentUser.CompanyLogo));
            OnPropertyChanged(nameof(CompanyLogoSource));
        }
        else
        {
            // Logo not found
        }

        // 2. Load Products
        await LoadProducts();

        if (_autoOpenPdf)
        {
            // Give UI time to render the products
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(800), OpenPdfDirectly);
        }
    }

    private async void OpenPdfDirectly()
    {
#if MACCATALYST
        try
        {
            var data = GeneratePdfData();
            if (data != null)
            {
                var fileName = $"Preview_{Order.OrderNo}.pdf";
                var filePath = System.IO.Path.Combine(FileSystem.CacheDirectory, fileName);
                
                data.Save(NSUrl.FromFilename(filePath), true);
                
                try 
                {
                    System.Diagnostics.Process.Start("open", filePath);
                }
                catch (Exception ex)
                {
                    await DisplayAlert(AppResources.Common_Error, "Erro ao abrir PDF: " + ex.Message, AppResources.Common_OK);
                }
            }
            else
            {
                // If data is null, maybe view isn't ready.
                await DisplayAlert(AppResources.Common_Error, "Não foi possível gerar o PDF. A visualização pode não estar pronta.", AppResources.Common_OK);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert(AppResources.Common_Error, "Erro ao gerar PDF: " + ex.Message, AppResources.Common_OK);
        }
#endif
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
            var pdfData = GeneratePdfData();
            if (pdfData != null)
            {
                var printInfo = UIPrintInfo.PrintInfo;
                printInfo.OutputType = UIPrintInfoOutputType.General;
                printInfo.JobName = $"Encomenda_{Order.OrderNo}";
                printInfo.Orientation = UIPrintInfoOrientation.Portrait;

                var printer = UIPrintInteractionController.SharedPrintController;
                printer.PrintInfo = printInfo;
                
                // Use the generated PDF data which we know works (has white background, correct scaling)
                printer.PrintingItem = pdfData;

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
        // Use the InvoiceSheet (Border) as the source
        var sourceView = InvoiceSheet;

        if (sourceView.Handler?.PlatformView is UIView platformView)
        {
            Console.WriteLine($"[PDF Debug] Generating PDF using Component Snapshot Strategy for {sourceView.GetType().Name}");

            // 1. Define Sizes
            var a4Width = 595.0f;
            var a4Height = 842.0f;
            var layoutWidth = 1200.0f; // Fixed width for consistent layout
            var scaleFactor = a4Width / layoutWidth;
            var pageRect = new CGRect(0, 0, a4Width, a4Height);

            // 2. Capture original state
            var originalFrame = platformView.Frame;
            var originalShadowOpacity = platformView.Layer.ShadowOpacity;
            var originalMauiShadow = InvoiceSheet.Shadow;
            var originalMasksToBounds = platformView.Layer.MasksToBounds;
            
            // 3. Prepare for PDF Rendering
            InvoiceSheet.Shadow = null!;
            
            void ClearShadows(CALayer layer)
            {
                layer.ShadowOpacity = 0;
                layer.ShadowColor = UIColor.Clear.CGColor;
                layer.ShadowOffset = CGSize.Empty;
                layer.ShadowRadius = 0;
                if (layer.Sublayers != null)
                    foreach (var sub in layer.Sublayers) ClearShadows(sub);
            }
            ClearShadows(platformView.Layer);

            platformView.Layer.MasksToBounds = true;
            platformView.BackgroundColor = UIColor.White;
            platformView.Layer.BackgroundColor = UIColor.White.CGColor;
            platformView.Opaque = true;

            // 4. Layout the view at 1200px width
            // We need to measure and layout to ensure all children are sized correctly
            var sizeRequest = sourceView.Measure(layoutWidth, double.PositiveInfinity);
            var contentHeight = (float)sizeRequest.Height;
            
            platformView.Frame = new CGRect(0, 0, layoutWidth, contentHeight);
            platformView.SetNeedsLayout();
            platformView.LayoutIfNeeded();

            // 5. Helper to Snapshot a View
            UIImage? Snapshot(View? view)
            {
                if (view == null) return null;
                if (view.Handler?.PlatformView is UIView nativeView)
                {
                    var renderer = new UIGraphicsImageRenderer(nativeView.Bounds.Size);
                    return renderer.CreateImage(_ => nativeView.DrawViewHierarchy(nativeView.Bounds, true));
                }
                return null;
            }

            // 6. Snapshot Components
            // We access the named views from XAML
            var headerImg = Snapshot(HeaderView);
            var clientInfoImg = Snapshot(ClientInfoView);
            var tableHeaderImg = Snapshot(TableHeaderView);
            var footerMessageImg = Snapshot(FooterMessageView);
            var totalsValuesImg = Snapshot(TotalsValuesView);
            var footerImg = Snapshot(FooterView);

            // Snapshot Rows
            var rowImages = new List<UIImage>();
            // ItemsContainer is a VerticalStackLayout with BindableLayout
            // We need to iterate its children (which are the rows)
            // Since it's a Layout, its Children property contains the MAUI views
            foreach (var child in ItemsContainer.Children)
            {
                if (child is View rowView)
                {
                    var img = Snapshot(rowView);
                    if (img != null) rowImages.Add(img);
                }
            }

            // 7. Generate PDF
            var data = new NSMutableData();
            UIGraphics.BeginPDFContext(data, pageRect, null);

            using (var context = UIGraphics.GetCurrentContext())
            {
                // Layout Constants (Scaled)
                float currentY = 0;
                float paddingX = 40 * scaleFactor; // Padding from ContentWrapper
                float contentWidth = (layoutWidth - 80) * scaleFactor; // Width inside padding
                float fullWidth = layoutWidth * scaleFactor; // 595

                // Heights (Scaled)
                float headerH = (float)(headerImg?.Size.Height ?? 0) * scaleFactor;
                float clientInfoH = (float)(clientInfoImg?.Size.Height ?? 0) * scaleFactor;
                float tableHeaderH = (float)(tableHeaderImg?.Size.Height ?? 0) * scaleFactor;
                float footerMessageH = (float)(footerMessageImg?.Size.Height ?? 0) * scaleFactor;
                float totalsValuesH = (float)(totalsValuesImg?.Size.Height ?? 0) * scaleFactor;
                float footerH = (float)(footerImg?.Size.Height ?? 0) * scaleFactor;

                // Calculate Footer Area Height (Message + Blue Bar + Padding)
                // We want the message to sit above the blue bar.
                float footerAreaH = footerH + footerMessageH + 10; 

                // Helper to Start New Page
                void StartNewPage()
                {
                    UIGraphics.BeginPDFPage();
                    context.SetFillColor(UIColor.White.CGColor);
                    context.FillRect(pageRect);
                    currentY = 0;

                    // Draw Header (Always)
                    if (headerImg != null)
                    {
                        headerImg.Draw(new CGRect(0, currentY, fullWidth, headerH));
                        currentY += headerH;
                    }

                    // Draw Client Info (Always - per user request)
                    if (clientInfoImg != null)
                    {
                        // Client Info is inside padding
                        clientInfoImg.Draw(new CGRect(paddingX, currentY, contentWidth, clientInfoH));
                        currentY += clientInfoH;
                    }

                    // Draw Table Header (Always)
                    if (tableHeaderImg != null)
                    {
                        // Table Header is inside padding
                        // Add a small margin before table header
                        currentY += 10; 
                        tableHeaderImg.Draw(new CGRect(paddingX, currentY, contentWidth, tableHeaderH));
                        currentY += tableHeaderH;
                    }
                }

                // Helper to Draw Standard Footer (Message + Blue Bar)
                void DrawStandardFooter()
                {
                    // Draw Blue Bar at bottom
                    if (footerImg != null)
                    {
                        footerImg.Draw(new CGRect(0, a4Height - footerH, fullWidth, footerH));
                    }
                    
                    // Draw Message above Blue Bar (Left Aligned)
                    if (footerMessageImg != null)
                    {
                        float msgY = a4Height - footerH - footerMessageH - 10;
                        footerMessageImg.Draw(new CGRect(paddingX, msgY, contentWidth, footerMessageH));
                    }
                }

                // Start Page 1
                StartNewPage();

                // Draw Rows
                foreach (var rowImg in rowImages)
                {
                    float rowH = (float)rowImg.Size.Height * scaleFactor;

                    // Check if row fits
                    // We need space for Row + Footer Area
                    if (currentY + rowH + footerAreaH > a4Height) 
                    {
                        // Draw Footer on current page
                        DrawStandardFooter();

                        // New Page
                        StartNewPage();
                    }

                    // Draw Row
                    rowImg.Draw(new CGRect(paddingX, currentY, contentWidth, rowH));
                    currentY += rowH;
                }

                // Draw Totals (Last Page)
                // We need space for Totals Block (Max of Message or Totals) + Footer Bar
                float finalBlockH = Math.Max(footerMessageH, totalsValuesH) + 10;
                
                if (currentY + finalBlockH + footerH > a4Height)
                {
                    DrawStandardFooter();
                    StartNewPage();
                }

                // Draw Footer Bar (Last Page)
                if (footerImg != null)
                {
                    footerImg.Draw(new CGRect(0, a4Height - footerH, fullWidth, footerH));
                }

                // Draw Message (Left)
                if (footerMessageImg != null)
                {
                    float msgY = a4Height - footerH - footerMessageH - 10;
                    footerMessageImg.Draw(new CGRect(paddingX, msgY, contentWidth, footerMessageH));
                }

                // Draw Totals (Right)
                if (totalsValuesImg != null)
                {
                    float totalsW = (float)totalsValuesImg.Size.Width * scaleFactor;
                    float totalsX = paddingX + contentWidth - totalsW;
                    float totalsY = a4Height - footerH - totalsValuesH - 10;
                    
                    totalsValuesImg.Draw(new CGRect(totalsX, totalsY, totalsW, totalsValuesH));
                }
            }

            UIGraphics.EndPDFContext();

            // 9. Restore original state
            InvoiceSheet.Shadow = originalMauiShadow;
            platformView.Layer.ShadowOpacity = originalShadowOpacity;
            platformView.Layer.MasksToBounds = originalMasksToBounds;
            platformView.Frame = originalFrame;
            platformView.SetNeedsLayout();
            platformView.LayoutIfNeeded();

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

                data.Save(NSUrl.FromFilename(filePath), true);
                
                await DisplayAlert(AppResources.Common_Success, string.Format(AppResources.Pdf_SavedSuccess, filePath), AppResources.Common_OK);
                
                // Open file directly
                try 
                {
                    System.Diagnostics.Process.Start("open", filePath);
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
                System.IO.File.WriteAllBytes(fn, data.ToArray());
                
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
            // Tenta carregar produtos usando OrderNo, Numserv ou Servencomenda
            var list = await DatabaseService.GetOrderedProductsExtendedAsync(Order);
            
            foreach (var item in list)
            {
                Products.Add(item);
            }
        }
        catch (System.Exception ex)
        {
            await DisplayAlert(AppResources.Common_Error, string.Format(AppResources.Products_LoadError, ex.Message), AppResources.Common_OK);
        }
    }

}
