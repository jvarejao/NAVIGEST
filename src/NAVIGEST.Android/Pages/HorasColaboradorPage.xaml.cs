using Microsoft.Maui.Controls;
using NAVIGEST.Android.PageModels;

namespace NAVIGEST.Android.Pages;

public partial class HorasColaboradorPage : ContentPage
{
    public HorasColaboradorPage()
    {
        InitializeComponent();
        
        try
        {
            var services = Application.Current?.Handler?.MauiContext?.Services;
            var vm = services?.GetService<HorasColaboradorViewModel>() ?? new HorasColaboradorViewModel();
            BindingContext = vm;
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex, mostrarAlerta: false);
            BindingContext = new HorasColaboradorViewModel();
        }
    }

    public HorasColaboradorPage(HorasColaboradorViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
