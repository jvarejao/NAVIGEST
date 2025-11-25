using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Controls;
using NAVIGEST.macOS.ViewModels;
using NAVIGEST.macOS.Popups;
using CommunityToolkit.Maui.Views;

namespace NAVIGEST.macOS.Views
{
    public partial class DashboardView : ContentView
    {
        public DashboardView()
        {
            InitializeComponent();
        }

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();

            if (BindingContext is DashboardViewModel vm)
            {
                vm.RequestOpenChartDetail += OnRequestOpenChartDetail;
                vm.InitializeAsync();
            }
        }

        private async void OnRequestOpenChartDetail(object? sender, (Models.Colaborador Colab, List<Models.MonthlyHoursData> Data, int Year) e)
        {
            try
            {
                var popup = new ChartDetailPopup(e.Colab, e.Data, e.Year);
                
                // Find the current page to show popup
                var page = Shell.Current?.CurrentPage ?? Application.Current?.MainPage;
                if (page != null)
                {
                    // await CommunityToolkit.Maui.Views.PageExtensions.ShowPopupAsync(page, popup);
                    // Temporary workaround
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
