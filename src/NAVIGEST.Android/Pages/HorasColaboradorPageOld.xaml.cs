using System;
using Microsoft.Maui.Controls;
using NAVIGEST.Android.PageModels;
using NAVIGEST.Android.Models;
using Microsoft.Extensions.DependencyInjection;

namespace NAVIGEST.Android.Pages;

public partial class HorasColaboradorPageOld : ContentPage
{
    public HorasColaboradorPageOld()
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

    public HorasColaboradorPageOld(HorasColaboradorViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    private async void OnItemTapped(object sender, TappedEventArgs e)
    {
        try
        {
            if (sender is Border border && border.BindingContext is HoraColaborador hora)
            {
                if (BindingContext is HorasColaboradorViewModel vm)
                {
                    await vm.EditarHoraCommand.ExecuteAsync(hora);
                }
            }
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex, mostrarAlerta: false);
        }
    }

    private async void OnEditarSwipe(object sender, EventArgs e)
    {
        try
        {
            if (sender is SwipeItemView siv && siv.BindingContext is HoraColaborador hora)
            {
                if (BindingContext is HorasColaboradorViewModel vm)
                {
                    await vm.EditarHoraCommand.ExecuteAsync(hora);
                }
            }
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex, mostrarAlerta: false);
        }
    }

    private async void OnEliminarSwipe(object sender, EventArgs e)
    {
        try
        {
            if (sender is SwipeItemView siv && siv.BindingContext is HoraColaborador hora)
            {
                if (BindingContext is HorasColaboradorViewModel vm)
                {
                    await vm.EliminarHoraCommand.ExecuteAsync(hora);
                }
            }
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex, mostrarAlerta: false);
        }
    }
}
