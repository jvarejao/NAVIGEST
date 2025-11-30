using Microsoft.Maui.Controls;
using NAVIGEST.macOS.ViewModels;

namespace NAVIGEST.macOS.Views
{
    public partial class DashboardView : ContentView
    {
        public DashboardView()
        {
            InitializeComponent();
        }

        protected override async void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();

            if (BindingContext is DashboardViewModel vm)
            {
                await vm.InitializeAsync();
            }
        }
    }
}
