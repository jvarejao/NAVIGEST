using NAVIGEST.macOS.PageModels;

namespace NAVIGEST.macOS.Pages;

public partial class ColorsPage : ContentPage
{
    public ColorsPage()
    {
        InitializeComponent();
        BindingContext = new ColorsPageModel();
    }
}
