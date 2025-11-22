using System;
using System.Linq;
using Microsoft.Maui.Controls;
using NAVIGEST.Android.PageModels;
using NAVIGEST.Android.Models;
using CommunityToolkit.Maui.Views;
using NAVIGEST.Android.Popups;
using NAVIGEST.Android;
using System.Threading.Tasks;

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
                vm.RequestOpenChartDetail += Vm_RequestOpenChartDetail;
                _ = vm.InitializeAsync();
            }
        }

        private async void Vm_RequestOpenChartDetail(object? sender, (Colaborador Colab, System.Collections.Generic.List<MonthlyHoursData> Data, int Year) e)
        {
            try
            {
                // Show Alert about rotation
                await Shell.Current.DisplayAlert("Rotação", "Para melhor visualização, o ecrã será rodado.", "OK");
                
#if ANDROID
                if (Microsoft.Maui.ApplicationModel.Platform.CurrentActivity != null)
                {
                    Microsoft.Maui.ApplicationModel.Platform.CurrentActivity.RequestedOrientation = global::Android.Content.PM.ScreenOrientation.Landscape;
                }
                
                // Give the system a moment to start the rotation
                await Task.Delay(500);
#endif

                var popup = new ChartDetailPopup(e.Colab, e.Data, e.Year);
                popup.Closed += (s, args) => 
                {
#if ANDROID
                    if (Microsoft.Maui.ApplicationModel.Platform.CurrentActivity != null)
                    {
                        Microsoft.Maui.ApplicationModel.Platform.CurrentActivity.RequestedOrientation = global::Android.Content.PM.ScreenOrientation.Portrait;
                    }
#endif
                };
                
                if (Shell.Current?.CurrentPage != null)
                {
                    Shell.Current.CurrentPage.ShowPopup(popup);
                }
            }
            catch (Exception ex)
            {
                GlobalErro.TratarErro(ex);
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
