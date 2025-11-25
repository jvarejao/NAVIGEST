using NAVIGEST.iOS.ViewModels;
using NAVIGEST.iOS.Models;
using NAVIGEST.iOS.Popups;
using CommunityToolkit.Maui.Views;
using Foundation;
using UIKit;

namespace NAVIGEST.iOS.Views;

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
            vm.RequestOpenChartDetail -= Vm_RequestOpenChartDetail;
            vm.RequestOpenChartDetail += Vm_RequestOpenChartDetail;
            
            // vm.RequestOpenDailyPopup -= Vm_RequestOpenDailyPopup;
            // vm.RequestOpenDailyPopup += Vm_RequestOpenDailyPopup;
            
            // vm.RequestOpenAbsencePopup -= Vm_RequestOpenAbsencePopup;
            // vm.RequestOpenAbsencePopup += Vm_RequestOpenAbsencePopup;

            _ = vm.InitializeAsync();
        }
    }

    private async void Vm_RequestOpenChartDetail(object? sender, (Colaborador Colab, List<MonthlyHoursData> Data, int Year) e)
    {
        try
        {
            // Show Alert about rotation
            await Shell.Current.DisplayAlert("Rotação", "Para melhor visualização, o ecrã será rodado.", "OK");

            // Force Landscape
            if (UIDevice.CurrentDevice.CheckSystemVersion(16, 0))
            {
                var windowScene = (UIApplication.SharedApplication.ConnectedScenes.AnyObject as UIWindowScene);
                if (windowScene != null)
                {
                    var geometryPreferences = new UIWindowSceneGeometryPreferencesIOS(UIInterfaceOrientationMask.LandscapeRight);
                    windowScene.RequestGeometryUpdate(geometryPreferences, error => { });
                }
            }
            else
            {
                UIDevice.CurrentDevice.SetValueForKey(new NSNumber((int)UIInterfaceOrientation.LandscapeRight), new NSString("orientation"));
            }
            
            await Task.Delay(500);

            var popup = new ChartDetailPopup(e.Colab, e.Data, e.Year);
            popup.Closed += (s, args) => 
            {
                // Force Portrait
                if (UIDevice.CurrentDevice.CheckSystemVersion(16, 0))
                {
                    var windowScene = (UIApplication.SharedApplication.ConnectedScenes.AnyObject as UIWindowScene);
                    if (windowScene != null)
                    {
                        var geometryPreferences = new UIWindowSceneGeometryPreferencesIOS(UIInterfaceOrientationMask.Portrait);
                        windowScene.RequestGeometryUpdate(geometryPreferences, error => { });
                    }
                }
                else
                {
                    UIDevice.CurrentDevice.SetValueForKey(new NSNumber((int)UIInterfaceOrientation.Portrait), new NSString("orientation"));
                }
            };
            
            if (Shell.Current?.CurrentPage != null)
            {
                Shell.Current.CurrentPage.ShowPopup(popup);
            }
        }
        catch (Exception ex)
        {
            NAVIGEST.iOS.GlobalErro.TratarErro(ex);
        }
    }
}