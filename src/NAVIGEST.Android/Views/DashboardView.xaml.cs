using System;
using System.Linq;
using Microsoft.Maui.Controls;
using NAVIGEST.Android.PageModels;
using NAVIGEST.Android.Models;
using CommunityToolkit.Maui.Views;
using NAVIGEST.Android.Popups;

namespace NAVIGEST.Android.Views
{
    public partial class DashboardView : ContentView
    {
        public DashboardView()
        {
            InitializeComponent();
            // ViewModel is set via x:DataType but we need to ensure BindingContext is set by parent or here.
            // Usually parent sets it.
        }

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();
            if (BindingContext is DashboardViewModel vm)
            {
                vm.RequestOpenDailyPopup += Vm_RequestOpenDailyPopup;
                vm.RequestOpenAbsencePopup += Vm_RequestOpenAbsencePopup;
                _ = vm.InitializeAsync();
            }
        }

        private void Vm_RequestOpenDailyPopup(object? sender, (MonthlyHoursData Month, System.Collections.Generic.List<DailyHoursData> Days) e)
        {
            // Show Popup
            var popup = new DailyChartPopup(e.Month, e.Days);
            Shell.Current.CurrentPage.ShowPopup(popup);
        }

        private void Vm_RequestOpenAbsencePopup(object? sender, (AbsenceSummary Summary, System.Collections.Generic.List<string> Details) e)
        {
            var popup = new AbsenceDetailsPopup(e.Summary, e.Details);
            Shell.Current.CurrentPage.ShowPopup(popup);
        }
    }
}
