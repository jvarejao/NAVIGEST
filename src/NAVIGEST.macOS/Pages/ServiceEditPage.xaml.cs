using Microsoft.Maui.Controls;
using NAVIGEST.macOS.PageModels;

namespace NAVIGEST.macOS.Pages;

public partial class ServiceEditPage : ContentPage
{
    public ServiceEditPage()
    {
        InitializeComponent();
        // Ensure a BindingContext exists even if the XAML declaration is removed later
        BindingContext ??= new ServiceEditPageModel();
    }

    public ServiceEditPage(NAVIGEST.macOS.Models.OrderInfoModel order)
    {
        InitializeComponent();
        BindingContext = new ServiceEditPageModel(order);
    }

    private void UnitPrice_Unfocused(object sender, FocusEventArgs e)
    {
        if (sender is Entry entry && entry.BindingContext is OrderedProductViewModel itemViewModel)
        {
            itemViewModel.FormatUnitPriceOnUnfocus();
        }
    }
}
