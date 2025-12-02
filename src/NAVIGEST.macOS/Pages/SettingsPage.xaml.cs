using NAVIGEST.macOS.PageModels;

namespace NAVIGEST.macOS.Pages
{
    public partial class SettingsPage : ContentPage
    {
        public SettingsPage()
        {
            InitializeComponent();
            BindingContext = new SettingsPageModel();
        }
    }
}
