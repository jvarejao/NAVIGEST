using Microsoft.Maui.Controls;
using NAVIGEST.macOS.PageModels;

namespace NAVIGEST.macOS.Pages;

public partial class ServiceEditPage : ContentPage
{
	public ServiceEditPage()
	{
		InitializeComponent();
	}

	private void UnitPrice_Unfocused(object sender, FocusEventArgs e)
	{
		if (sender is Entry entry && entry.BindingContext is OrderedProductViewModel vm)
		{
			vm.FormatUnitPriceOnUnfocus();
		}
	}
}
