using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using NAVIGEST.Android.PageModels;
using NAVIGEST.Android.Models;

namespace NAVIGEST.Android.Pages;

public partial class HorasColaboradorPage : ContentPage
{
    private HorasColaboradorViewModel _vm => (HorasColaboradorViewModel)BindingContext;
    
    public HorasColaboradorPage()
    {
        InitializeComponent();
        
        try
        {
            var services = Application.Current?.Handler?.MauiContext?.Services;
            var vm = services?.GetService<HorasColaboradorViewModel>() ?? new HorasColaboradorViewModel();
            BindingContext = vm;
            
            // Carregar Tab 1 por padrão
            CarregarTab1Resumo();
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex, mostrarAlerta: false);
            BindingContext = new HorasColaboradorViewModel();
            CarregarTab1Resumo();
        }
    }

    public HorasColaboradorPage(HorasColaboradorViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        CarregarTab1Resumo();
    }

    private void OnTab1Tapped(object sender, EventArgs e)
    {
        AtivarTab(1);
        CarregarTab1Resumo();
    }

    private void OnTab2Tapped(object sender, EventArgs e)
    {
        AtivarTab(2);
        CarregarTab2Lista();
    }

    private void OnTab3Tapped(object sender, EventArgs e)
    {
        AtivarTab(3);
        CarregarTab3Calendario();
    }

    private void AtivarTab(int numeroTab)
    {
        // Reset de todos os tabs
        Tab1Border.BackgroundColor = Colors.Transparent;
        Tab2Border.BackgroundColor = Colors.Transparent;
        Tab3Border.BackgroundColor = Colors.Transparent;
        
        Tab1Label.TextColor = Color.FromArgb("#8E8E93");
        Tab2Label.TextColor = Color.FromArgb("#8E8E93");
        Tab3Label.TextColor = Color.FromArgb("#8E8E93");
        
        Tab1Label.FontAttributes = FontAttributes.None;
        Tab2Label.FontAttributes = FontAttributes.None;
        Tab3Label.FontAttributes = FontAttributes.None;
        
        // Ativar tab selecionado
        switch (numeroTab)
        {
            case 1:
                Tab1Border.BackgroundColor = Color.FromArgb("#0A84FF");
                Tab1Label.TextColor = Colors.White;
                Tab1Label.FontAttributes = FontAttributes.Bold;
                break;
            case 2:
                Tab2Border.BackgroundColor = Color.FromArgb("#0A84FF");
                Tab2Label.TextColor = Colors.White;
                Tab2Label.FontAttributes = FontAttributes.Bold;
                break;
            case 3:
                Tab3Border.BackgroundColor = Color.FromArgb("#0A84FF");
                Tab3Label.TextColor = Colors.White;
                Tab3Label.FontAttributes = FontAttributes.Bold;
                break;
        }
    }

    private void CarregarTab1Resumo()
    {
        TabContentView.Content = CriarConteudoTab1();
    }

    private void CarregarTab2Lista()
    {
        TabContentView.Content = CriarConteudoTab2();
    }

    private void CarregarTab3Calendario()
    {
        TabContentView.Content = CriarConteudoTab3();
    }

    private View CriarConteudoTab1()
    {
        var scroll = new ScrollView();
        var stack = new VerticalStackLayout { Padding = 16, Spacing = 16 };
        
        stack.Add(new Label 
        { 
            Text = "🚧 Tab Resumo - Em Construção",
            FontSize = 20,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 40, 0, 20)
        });
        
        stack.Add(new Label 
        { 
            Text = "Aqui irá aparecer:\n• Cards de estatísticas\n• Totais do período\n• Gráficos de horas",
            HorizontalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            TextColor = Color.FromArgb("#8E8E93")
        });
        
        scroll.Content = stack;
        return scroll;
    }

    private View CriarConteudoTab2()
    {
        var scroll = new ScrollView();
        var stack = new VerticalStackLayout { Padding = 16, Spacing = 16 };
        
        stack.Add(new Label 
        { 
            Text = "🚧 Tab Lista - Em Construção",
            FontSize = 20,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 40, 0, 20)
        });
        
        stack.Add(new Label 
        { 
            Text = "Aqui irá aparecer:\n• Filtros avançados\n• Month picker\n• Lista de horas\n• Swipe edit/delete",
            HorizontalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            TextColor = Color.FromArgb("#8E8E93")
        });
        
        scroll.Content = stack;
        return scroll;
    }

    private View CriarConteudoTab3()
    {
        var scroll = new ScrollView();
        var stack = new VerticalStackLayout { Padding = 16, Spacing = 16 };
        
        stack.Add(new Label 
        { 
            Text = "🚧 Tab Calendário - Em Construção",
            FontSize = 20,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 40, 0, 20)
        });
        
        stack.Add(new Label 
        { 
            Text = "Aqui irá aparecer:\n• Vista de calendário\n• Dias com horas registadas\n• Cores por colaborador",
            HorizontalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            TextColor = Color.FromArgb("#8E8E93")
        });
        
        scroll.Content = stack;
        return scroll;
    }
}
