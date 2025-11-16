using System;
using Microsoft.Maui.Controls;
using NAVIGEST.Android.PageModels;
using NAVIGEST.Android.Models;
using Microsoft.Extensions.DependencyInjection;

namespace NAVIGEST.Android.Pages;

public partial class HorasColaboradorPage : ContentPage
{
    public HorasColaboradorPage()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[HorasColaboradorPage] Construtor sem parâmetros - Iniciando InitializeComponent");
            InitializeComponent();
            System.Diagnostics.Debug.WriteLine("[HorasColaboradorPage] InitializeComponent OK");
            
            var services = Application.Current?.Handler?.MauiContext?.Services;
            System.Diagnostics.Debug.WriteLine($"[HorasColaboradorPage] Services disponível: {services != null}");
            
            var vm = services?.GetService<HorasColaboradorViewModel>();
            System.Diagnostics.Debug.WriteLine($"[HorasColaboradorPage] ViewModel do DI: {vm != null}");
            
            if (vm == null)
            {
                System.Diagnostics.Debug.WriteLine("[HorasColaboradorPage] Criando ViewModel manualmente");
                vm = new HorasColaboradorViewModel();
            }
            
            BindingContext = vm;
            System.Diagnostics.Debug.WriteLine("[HorasColaboradorPage] BindingContext definido");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HorasColaboradorPage] ERRO no construtor: {ex}");
            GlobalErro.TratarErro(ex, mostrarAlerta: false);
            try
            {
                BindingContext = new HorasColaboradorViewModel();
            }
            catch (Exception ex2)
            {
                System.Diagnostics.Debug.WriteLine($"[HorasColaboradorPage] ERRO ao criar ViewModel fallback: {ex2}");
            }
        }
    }

    public HorasColaboradorPage(HorasColaboradorViewModel vm)
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
