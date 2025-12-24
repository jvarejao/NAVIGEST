using Microsoft.Maui.Controls;
using NAVIGEST.macOS.PageModels;

namespace NAVIGEST.macOS.Pages
{
    public partial class DashboardPage : ContentPage
    {
        private readonly AnalyticsDashboardViewModel _viewModel;

        public DashboardPage()
        {
            InitializeComponent();
            _viewModel = new AnalyticsDashboardViewModel();
            BindingContext = _viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = _viewModel.LoadAsync();
        }
    }
}
