using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls.Shapes;
using CommunityToolkit.Maui.Views;
using NAVIGEST.macOS.ViewModels;
using NAVIGEST.macOS.Models;
using NAVIGEST.macOS.Services;
using NAVIGEST.macOS.Views;

namespace NAVIGEST.macOS.Pages;

public partial class HoursEntryPage : ContentPage
{
    private HorasColaboradorViewModel _vm => (HorasColaboradorViewModel)BindingContext;
    
    private DateTime _dataCalendario = DateTime.Today;

    public HoursEntryPage()
    {
        InitializeComponent();
        
        try
        {
            var services = Application.Current?.Handler?.MauiContext?.Services;
            var vm = services?.GetService<HorasColaboradorViewModel>() ?? new HorasColaboradorViewModel();
            BindingContext = vm;
            CarregarTab1Resumo();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao inicializar: {ex.Message}");
            BindingContext = new HorasColaboradorViewModel();
            CarregarTab1Resumo();
        }
    }

    public HoursEntryPage(HorasColaboradorViewModel vm)
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
        _vm.TabAtiva = numeroTab;
        Tab1Border.BackgroundColor = Colors.Transparent;
        Tab2Border.BackgroundColor = Colors.Transparent;
        Tab3Border.BackgroundColor = Colors.Transparent;
        
        Tab1Label.TextColor = Color.FromArgb("#8E8E93");
        Tab2Label.TextColor = Color.FromArgb("#8E8E93");
        Tab3Label.TextColor = Color.FromArgb("#8E8E93");
        
        Tab1Label.FontAttributes = FontAttributes.None;
        Tab2Label.FontAttributes = FontAttributes.None;
        Tab3Label.FontAttributes = FontAttributes.None;
        
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

    // ==================== TAB 1: RESUMO ====================
    private View CriarConteudoTab1()
    {
        var view = new NAVIGEST.macOS.Views.DashboardView();
        var vm = new NAVIGEST.macOS.ViewModels.DashboardViewModel();
        view.BindingContext = vm;
        return view;
    }
    
    // ==================== TAB 2: LISTA ====================
    private View CriarConteudoTab2()
    {
        var mainGrid = new Grid 
        { 
            RowDefinitions = new RowDefinitionCollection { new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Star) },
            RowSpacing = 12,
            Padding = 16
        };
        
        // Botões de Ação
        var buttonsGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star) },
            ColumnSpacing = 8
        };

        // Botão Adicionar
        var btnAdicionar = new Button
        {
            Text = "➕ Horas",
            BackgroundColor = Color.FromArgb("#0A84FF"),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 12,
            Padding = 12
        };
        btnAdicionar.Clicked += async (s, e) => await AbrirNovaHoraPopupAsync(null);
        
        // Botão Gerir Tipos
        var btnTipos = new Button
        {
            Text = "⚙️ Tipos Ausência",
            BackgroundColor = Color.FromArgb("#E5E5EA"),
            TextColor = Colors.Black,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 12,
            Padding = 12
        };

        buttonsGrid.Add(btnAdicionar, 0, 0);
        buttonsGrid.Add(btnTipos, 1, 0);
        
        mainGrid.Add(buttonsGrid, 0, 0);
        
        // Filtros
        var filtrosStack = new VerticalStackLayout { Spacing = 12 };
        
        var filtrosGrid = new Grid 
        { 
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star) },
            ColumnSpacing = 8
        };
        
        // Picker Colaborador
        var colabPicker = new Picker
        {
            Title = "Colaborador",
            ItemsSource = _vm.Colaboradores,
            ItemDisplayBinding = new Binding("Nome"),
            SelectedItem = _vm.ColaboradorSelecionado,
            BackgroundColor = Application.Current?.RequestedTheme == AppTheme.Dark ? Color.FromArgb("#2C2C2E") : Color.FromArgb("#F5F5F7"),
            TextColor = Application.Current?.RequestedTheme == AppTheme.Dark ? Colors.White : Colors.Black
        };
        colabPicker.SelectedIndexChanged += (s, e) =>
        {
            if (colabPicker.SelectedItem is Colaborador colab)
            {
                _vm.ColaboradorSelecionado = colab;
                _ = _vm.CarregarHorasCommand.ExecuteAsync(null);
            }
        };
        filtrosGrid.Add(colabPicker, 0, 0);
        
        // Botão Período (ActionSheet)
        var btnPeriodo = new Button
        {
            Text = "📅 Período",
            BackgroundColor = Application.Current?.RequestedTheme == AppTheme.Dark ? Color.FromArgb("#2C2C2E") : Color.FromArgb("#F5F5F7"),
            TextColor = Color.FromArgb("#0A84FF"),
            CornerRadius = 8,
            FontSize = 14
        };
        btnPeriodo.Clicked += async (s, e) => await MostrarMenuPeriodoAsync();
        filtrosGrid.Add(btnPeriodo, 1, 0);
        
        filtrosStack.Add(filtrosGrid);
        mainGrid.Add(filtrosStack, 0, 1);
        
        // Lista de Horas
        var listaCollection = new CollectionView
        {
            ItemsSource = _vm.HorasList,
            ItemTemplate = new DataTemplate(() => CriarItemLista())
        };
        
        var emptyView = new VerticalStackLayout 
        { 
            Spacing = 8,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center
        };
        emptyView.Add(new Label { Text = "📭", FontSize = 48, HorizontalTextAlignment = TextAlignment.Center });
        emptyView.Add(new Label { Text = "Sem registos neste período", FontSize = 16, TextColor = Color.FromArgb("#8E8E93"), HorizontalTextAlignment = TextAlignment.Center });
        listaCollection.EmptyView = emptyView;
        
        mainGrid.Add(listaCollection, 0, 2);
        
        return mainGrid;
    }
    
    private View CriarItemLista()
    {
        var itemBorder = new Border
        {
            BackgroundColor = Application.Current?.RequestedTheme == AppTheme.Dark ? Color.FromArgb("#1C1C1E") : Colors.White,
            StrokeThickness = 0,
            Padding = 12,
            Margin = new Thickness(0, 0, 0, 8),
            StrokeShape = new RoundRectangle { CornerRadius = 8 }
        };
        
        var itemGrid = new Grid 
        { 
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Auto), new(GridLength.Auto) },
            RowDefinitions = new RowDefinitionCollection { new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto) }
        };
        
        var lblData = new Label { FontSize = 14, FontAttributes = FontAttributes.Bold };
        lblData.SetBinding(Label.TextProperty, new Binding("DataTrabalho", stringFormat: "{0:dd/MM/yyyy - ddd}"));
        itemGrid.Add(lblData, 0, 0);
        
        var lblNome = new Label { FontSize = 12, TextColor = Color.FromArgb("#8E8E93") };
        lblNome.SetBinding(Label.TextProperty, new Binding("NomeColaborador"));
        itemGrid.Add(lblNome, 0, 1);
        
        var lblCliente = new Label { FontSize = 11, TextColor = Color.FromArgb("#8E8E93") };
        lblCliente.SetBinding(Label.TextProperty, new Binding("Cliente", stringFormat: "🏢 {0}"));
        itemGrid.Add(lblCliente, 0, 2);
        
        var horasStack = new VerticalStackLayout 
        { 
            Spacing = 2,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.End
        };
        
        var lblHorasNormais = new Label 
        { 
            FontSize = 16, 
            FontAttributes = FontAttributes.Bold, 
            TextColor = Color.FromArgb("#34C759"),
            HorizontalTextAlignment = TextAlignment.End
        };
        lblHorasNormais.SetBinding(Label.TextProperty, new Binding("HorasTrab", stringFormat: "✓ {0:0.0}h"));
        horasStack.Add(lblHorasNormais);
        
        var lblHorasExtra = new Label 
        { 
            FontSize = 14, 
            FontAttributes = FontAttributes.Bold, 
            TextColor = Color.FromArgb("#FF9500"),
            HorizontalTextAlignment = TextAlignment.End
        };
        lblHorasExtra.SetBinding(Label.TextProperty, new Binding("HorasExtras", stringFormat: "⚡ {0:0.0}h"));
        horasStack.Add(lblHorasExtra);
        
        itemGrid.Add(horasStack, 1, 0);
        Grid.SetRowSpan(horasStack, 3);

        var actionsStack = new HorizontalStackLayout { Spacing = 8, VerticalOptions = LayoutOptions.Center };
        var btnEdit = new Button { Text = "✎", BackgroundColor = Colors.Transparent, TextColor = Color.FromArgb("#0A84FF") };
        btnEdit.Clicked += async (s, e) => 
        {
             if ((s as Button)?.BindingContext is HoraColaborador hora)
                await AbrirNovaHoraPopupAsync(hora);
        };
        actionsStack.Add(btnEdit);

        var btnDelete = new Button { Text = "🗑️", BackgroundColor = Colors.Transparent, TextColor = Color.FromArgb("#FF3B30") };
        btnDelete.Clicked += async (s, e) => 
        {
             if ((s as Button)?.BindingContext is HoraColaborador hora)
                await _vm.EliminarHoraCommand.ExecuteAsync(hora);
        };
        actionsStack.Add(btnDelete);

        itemGrid.Add(actionsStack, 2, 0);
        Grid.SetRowSpan(actionsStack, 3);
        
        itemBorder.Content = itemGrid;
        
        return itemBorder;
    }
    
    private async Task MostrarMenuPeriodoAsync()
    {
        var page = Application.Current?.Windows[0]?.Page;
        if (page == null) return;
        
        var action = await page.DisplayActionSheet("Selecionar Período", "Cancelar", null, 
            "Hoje", "Esta Semana", "Este Mês", "Últimos 30 dias");
        
        if (action == "Hoje") _vm.SelecionarHojeCommand.Execute(null);
        else if (action == "Esta Semana") _vm.SelecionarEstaSemanaCommand.Execute(null);
        else if (action == "Este Mês") _vm.SelecionarEsteMesCommand.Execute(null);
        else if (action == "Últimos 30 dias") _vm.SelecionarUltimos30DiasCommand.Execute(null);
    }

    private async Task AbrirNovaHoraPopupAsync(HoraColaborador? hora)
    {
        await DisplayAlert("Info", "Funcionalidade de popup em desenvolvimento", "OK");
    }

    // ==================== TAB 3: CALENDÁRIO ====================
    private View CriarConteudoTab3()
    {
        var scroll = new ScrollView();
        var mainStack = new VerticalStackLayout { Spacing = 16, Padding = 16 };
        
        var lblTitulo = new Label 
        { 
            Text = "📅 Calendário do Mês", 
            FontSize = 20, 
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center 
        };
        mainStack.Add(lblTitulo);

        var pickerContainer = new Border
        {
            Stroke = Color.FromArgb("#E5E5EA"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(12, 0),
            Margin = new Thickness(0, 0, 0, 8)
        };
        
        pickerContainer.SetAppThemeColor(Border.BackgroundColorProperty, Colors.White, Color.FromArgb("#111827"));
        pickerContainer.SetAppThemeColor(Border.StrokeProperty, Color.FromArgb("#E5E5EA"), Color.FromArgb("#38383A"));

        var picker = new Picker
        {
            Title = "Selecione Colaborador",
            ItemsSource = _vm.Colaboradores,
            ItemDisplayBinding = new Binding("Nome"),
            SelectedItem = _vm.ColaboradorSelecionado,
            BackgroundColor = Colors.Transparent
        };
        picker.SetAppThemeColor(Picker.TextColorProperty, Colors.Black, Colors.White);
        picker.SetAppThemeColor(Picker.TitleColorProperty, Color.FromArgb("#8E8E93"), Color.FromArgb("#8E8E93"));
        
        picker.SelectedIndexChanged += async (s, e) => 
        {
            if (picker.SelectedItem is Colaborador c)
            {
                _vm.ColaboradorSelecionado = c;
                await AtualizarFiltroCalendarioAsync();
            }
        };
        
        pickerContainer.Content = picker;
        mainStack.Add(pickerContainer);
        
        var navGrid = new Grid 
        { 
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) },
            Margin = new Thickness(0, 0, 0, 16)
        };
        
        var btnPrev = new Button 
        { 
            Text = "◀", 
            BackgroundColor = Color.FromArgb("#0A84FF"), 
            TextColor = Colors.White, 
            CornerRadius = 20, 
            WidthRequest = 40, 
            HeightRequest = 40 
        };
        btnPrev.Clicked += (s, e) => MudarMes(-1);
        
        var btnNext = new Button 
        { 
            Text = "▶", 
            BackgroundColor = Color.FromArgb("#0A84FF"), 
            TextColor = Colors.White, 
            CornerRadius = 20, 
            WidthRequest = 40, 
            HeightRequest = 40 
        };
        btnNext.Clicked += (s, e) => MudarMes(1);
        
        var lblMes = new Label 
        { 
            Text = _dataCalendario.ToString("MMMM yyyy").ToUpper(), 
            FontSize = 16, 
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };
        
        navGrid.Add(btnPrev, 0, 0);
        navGrid.Add(lblMes, 1, 0);
        navGrid.Add(btnNext, 2, 0);
        mainStack.Add(navGrid);
        
        var gridDiasSemana = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection 
            { 
                new(GridLength.Star), new(GridLength.Star), new(GridLength.Star), new(GridLength.Star), 
                new(GridLength.Star), new(GridLength.Star), new(GridLength.Star) 
            }
        };
        
        string[] diasSemana = { "Seg", "Ter", "Qua", "Qui", "Sex", "Sáb", "Dom" };
        for (int i = 0; i < 7; i++)
        {
            gridDiasSemana.Add(new Label 
            { 
                Text = diasSemana[i], 
                FontSize = 12, 
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#8E8E93"), 
                HorizontalTextAlignment = TextAlignment.Center 
            }, i, 0);
        }
        mainStack.Add(gridDiasSemana);
        
        var calendarGrid = ConstruirGridCalendario();
        mainStack.Add(calendarGrid);
        
        var legendaStack = new HorizontalStackLayout 
        { 
            Spacing = 12, 
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 16, 0, 0)
        };
        
        legendaStack.Add(CriarItemLegenda("#34C759", "Normal"));
        legendaStack.Add(CriarItemLegenda("#FF9500", "Extra"));
        
        mainStack.Add(legendaStack);
        
        scroll.Content = mainStack;
        return scroll;
    }

    private View CriarItemLegenda(string colorHex, string text)
    {
        var stack = new HorizontalStackLayout { Spacing = 4, VerticalOptions = LayoutOptions.Center };
        stack.Add(new BoxView { Color = Color.FromArgb(colorHex), WidthRequest = 12, HeightRequest = 12, CornerRadius = 6, VerticalOptions = LayoutOptions.Center });
        stack.Add(new Label { Text = text, FontSize = 12, VerticalTextAlignment = TextAlignment.Center });
        return stack;
    }

    private async void MudarMes(int meses)
    {
        _dataCalendario = _dataCalendario.AddMonths(meses);
        await AtualizarFiltroCalendarioAsync();
        CarregarTab3Calendario();
    }

    private async Task AtualizarFiltroCalendarioAsync()
    {
        var inicio = new DateTime(_dataCalendario.Year, _dataCalendario.Month, 1);
        var fim = inicio.AddMonths(1).AddDays(-1);
        
        _vm.DataFiltroInicio = inicio;
        _vm.DataFiltroFim = fim;
        
        await _vm.CarregarHorasCommand.ExecuteAsync(null);
    }

    private Grid ConstruirGridCalendario()
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection 
            { 
                new(GridLength.Star), new(GridLength.Star), new(GridLength.Star), new(GridLength.Star), 
                new(GridLength.Star), new(GridLength.Star), new(GridLength.Star) 
            },
            RowSpacing = 8,
            ColumnSpacing = 8
        };

        var primeiroDiaMes = new DateTime(_dataCalendario.Year, _dataCalendario.Month, 1);
        int diasNoMes = DateTime.DaysInMonth(_dataCalendario.Year, _dataCalendario.Month);
        int diaSemanaInicio = (int)primeiroDiaMes.DayOfWeek;
        int offset = diaSemanaInicio == 0 ? 6 : diaSemanaInicio - 1;

        int row = 0;
        int col = offset;

        for (int dia = 1; dia <= diasNoMes; dia++)
        {
            var dataAtual = new DateTime(_dataCalendario.Year, _dataCalendario.Month, dia);
            
            var horasDia = _vm.HorasList.Where(h => h.DataTrabalho.Date == dataAtual.Date).ToList();
            bool temHoras = horasDia.Any();
            bool temExtras = horasDia.Sum(h => h.HorasExtras) > 0;

            var border = new Border
            {
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 8 },
                BackgroundColor = temHoras ? (temExtras ? Color.FromArgb("#FF9500") : Color.FromArgb("#34C759")) : Colors.Transparent,
                HeightRequest = 40,
                WidthRequest = 40
            };

            var label = new Label
            {
                Text = dia.ToString(),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                TextColor = temHoras ? Colors.White : (Application.Current?.RequestedTheme == AppTheme.Dark ? Colors.White : Colors.Black)
            };

            border.Content = label;
            grid.Add(border, col, row);

            col++;
            if (col > 6)
            {
                col = 0;
                row++;
                grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            }
        }
        
        if (grid.RowDefinitions.Count == 0) grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        return grid;
    }
}
