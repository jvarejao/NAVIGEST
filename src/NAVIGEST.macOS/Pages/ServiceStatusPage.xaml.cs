using NAVIGEST.macOS.PageModels;

namespace NAVIGEST.macOS.Pages
{
    public partial class ServiceStatusPage : ContentPage
    {
        public ServiceStatusPage()
        {
            InitializeComponent();
            BindingContext = new ServiceStatusPageModel();
        }
    }
}
