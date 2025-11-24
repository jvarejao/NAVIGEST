using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls.Shapes;
using CommunityToolkit.Maui.Views;
using NAVIGEST.iOS.ViewModels;
using NAVIGEST.iOS.Models;
using NAVIGEST.iOS.Services;
using NAVIGEST.iOS.Converters;
using Foundation;

namespace NAVIGEST.iOS.Pages;

public partial class HorasColaboradorPage : ContentPage
{
    private HorasColaboradorViewModel _vm => (HorasColaboradorViewModel)BindingContext;
    
    private DateTime _dataCalendario = DateTime.Today;

    private static string GetLogPath()
    {
        string documentsPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "NAVIGEST");
        if (!Directory.Exists(documentsPath)) Directory.CreateDirectory(documentsPath);
        return System.IO.Path.Combine(documentsPath, "tap_log.txt");
    }

    private static void LogTap(string message)
    {
        try
        {
            string logPath = GetLogPath();
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            string logMessage = $"[{timestamp}] {message}";
            File.AppendAllText(logPath, logMessage + "\n");
            
            // Também escrever em Debug para visibilidade
            System.Diagnostics.Debug.WriteLine(logMessage);
            Console.WriteLine(logMessage);
        }
        catch { /* Ignorar erros de escrita */ }
    }

    public HorasColaboradorPage()
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
        // Use the new DashboardView
        var view = new NAVIGEST.iOS.Views.DashboardView();
        var vm = new NAVIGEST.iOS.ViewModels.DashboardViewModel();
        view.BindingContext = vm;
        
        // Initialize to load collaborators
        _ = vm.InitializeAsync();
        
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
        
        // Botão Adicionar
        var btnAdicionar = new Button
        {
            Text = "➕ Adicionar Horas Trab.",
            BackgroundColor = Color.FromArgb("#0A84FF"),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 12,
            Padding = 12
        };
        btnAdicionar.Clicked += async (s, e) => await AbrirNovaHoraPopupAsync(null);
        mainGrid.Add(btnAdicionar, 0, 0);
        
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
            ItemsSource = _vm.Colaboradores.ToList(),
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
        
        // Lista de Horas com Swipe
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
        var swipeView = new SwipeView();
        
        // Editar - Botão circular
        var editGrid = new Grid 
        { 
            WidthRequest = 80, 
            Padding = 0, 
            BackgroundColor = Colors.Transparent, 
            HorizontalOptions = LayoutOptions.Center, 
            VerticalOptions = LayoutOptions.Fill 
        };
        var editBorder = new Border
        {
            WidthRequest = 56,
            HeightRequest = 56,
            BackgroundColor = Color.FromArgb("#0A84FF"),
            StrokeThickness = 0,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            StrokeShape = new RoundRectangle { CornerRadius = 28 }
        };
        editBorder.Content = new Label
        {
            Text = "✎",
            FontSize = 32,
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
        editGrid.Add(editBorder);
        
        var editSwipeView = new SwipeItemView { Content = editGrid };
        editSwipeView.Invoked += async (s, e) =>
        {
            if ((s as SwipeItemView)?.BindingContext is HoraColaborador hora)
            {
                await AbrirNovaHoraPopupAsync(hora);
            }
        };
        
        // Eliminar - Botão circular
        var deleteGrid = new Grid 
        { 
            WidthRequest = 80, 
            Padding = 0, 
            BackgroundColor = Colors.Transparent, 
            HorizontalOptions = LayoutOptions.Center, 
            VerticalOptions = LayoutOptions.Fill 
        };
        var deleteBorder = new Border
        {
            WidthRequest = 56,
            HeightRequest = 56,
            BackgroundColor = Color.FromArgb("#FF3B30"),
            StrokeThickness = 0,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            StrokeShape = new RoundRectangle { CornerRadius = 28 }
        };
        deleteBorder.Content = new Label
        {
            FontFamily = "FA7Solid",
            Text = "\uf1f8",
            FontSize = 22,
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
        deleteGrid.Add(deleteBorder);
        
        var deleteSwipeView = new SwipeItemView { Content = deleteGrid };
        deleteSwipeView.Invoked += async (s, e) =>
        {
            if ((s as SwipeItemView)?.BindingContext is HoraColaborador hora)
            {
                await _vm.EliminarHoraCommand.ExecuteAsync(hora);
            }
        };
        
        // Ambos os botões circulares no lado direito
        var swipeItems = new SwipeItems { Mode = SwipeMode.Reveal, SwipeBehaviorOnInvoked = SwipeBehaviorOnInvoked.Close };
        swipeItems.Add(editSwipeView);
        swipeItems.Add(deleteSwipeView);
        swipeView.RightItems = swipeItems;
        
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
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Auto) },
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
        lblCliente.SetBinding(Label.IsVisibleProperty, new Binding("Cliente", converter: new StringNotEmptyConverter()));
        itemGrid.Add(lblCliente, 0, 2);
        
        // Lado direito: Horas Normal + Extra
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
        lblHorasExtra.SetBinding(Label.IsVisibleProperty, new Binding("HorasExtras", converter: new HorasExtraConverter()));
        horasStack.Add(lblHorasExtra);
        
        itemGrid.Add(horasStack, 1, 0);
        Grid.SetRowSpan(horasStack, 3);
        
        itemBorder.Content = itemGrid;
        swipeView.Content = itemBorder;
        
        return swipeView;
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

        // Picker de Colaborador
        var pickerContainer = new Border
        {
            Stroke = Color.FromArgb("#E5E5EA"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(12, 0),
            Margin = new Thickness(0, 0, 0, 8)
        };
        
        // Cor de fundo adaptativa
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
        
        // Header: Mês/Ano + Navegação
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
        
        // Dias da Semana (Segunda a Domingo)
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
        
        // Grid Calendário
        var calendarGrid = ConstruirGridCalendario();
        mainStack.Add(calendarGrid);
        
        // Legenda
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
    }

    private async Task AtualizarFiltroCalendarioAsync()
    {
        var inicio = new DateTime(_dataCalendario.Year, _dataCalendario.Month, 1);
        var fim = inicio.AddMonths(1).AddDays(-1);
        
        _vm.DataFiltroInicio = inicio;
        _vm.DataFiltroFim = fim;
        
        try 
        {
            int? idColaboradorFiltro = _vm.ColaboradorSelecionado?.ID == 0 ? null : _vm.ColaboradorSelecionado?.ID;
            var horas = await NAVIGEST.iOS.Services.DatabaseService.GetHorasColaboradorAsync(idColaboradorFiltro, inicio, fim);
            
            _vm.HorasList.Clear();
            foreach (var hora in horas.OrderByDescending(h => h.DataTrabalho)) 
            {
                _vm.HorasList.Add(hora);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao carregar calendário: {ex.Message}");
        }
        
        CarregarTab3Calendario();
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
            RowDefinitions = new RowDefinitionCollection 
            { 
                new(GridLength.Star), new(GridLength.Star), new(GridLength.Star), 
                new(GridLength.Star), new(GridLength.Star), new(GridLength.Star) 
            },
            RowSpacing = 4,
            ColumnSpacing = 4
        };

        var primeiroDiaMes = new DateTime(_dataCalendario.Year, _dataCalendario.Month, 1);
        int diasNoMes = DateTime.DaysInMonth(_dataCalendario.Year, _dataCalendario.Month);
        
        // Ajuste para começar na Segunda (Monday = 1, Sunday = 7)
        int diaSemanaPrimeiro = ((int)primeiroDiaMes.DayOfWeek == 0) ? 6 : (int)primeiroDiaMes.DayOfWeek - 1;

        // OTIMIZAÇÃO: Criar dicionário para acesso rápido
        var horasDict = _vm.HorasList.ToLookup(h => h.DataTrabalho.Date);

        int diaAtual = 1;
        for (int semana = 0; semana < 6; semana++)
        {
            for (int col = 0; col < 7; col++)
            {
                int celulaIndex = semana * 7 + col;
                
                if (celulaIndex >= diaSemanaPrimeiro && diaAtual <= diasNoMes)
                {
                    var data = new DateTime(_dataCalendario.Year, _dataCalendario.Month, diaAtual);
                    var horasDoDia = horasDict[data].ToList();
                    
                    float totalNormais = horasDoDia.Sum(h => h.HorasTrab);
                    float totalExtras = horasDoDia.Sum(h => h.HorasExtras);
                    bool temHoras = totalNormais > 0 || totalExtras > 0;
                    
                    bool isWeekend = data.DayOfWeek == DayOfWeek.Saturday || data.DayOfWeek == DayOfWeek.Sunday;
                    
                    var bgColor = Colors.Transparent;
                    if (isWeekend)
                    {
                        bgColor = Application.Current?.RequestedTheme == AppTheme.Dark 
                            ? Color.FromArgb("#1C1C1E") 
                            : Color.FromArgb("#F2F2F7");
                    }
                    
                    if (temHoras) bgColor = Color.FromArgb("#0A84FF").WithAlpha(0.1f);

                    // Célula Container
                    var cellGrid = new Grid
                    {
                        Padding = 2,
                        BackgroundColor = Colors.Transparent // Ensure hit test works
                    };

                    // 1. Conteúdo Visual (Border)
                    var diaBorder = new Border
                    {
                        BackgroundColor = bgColor,
                        StrokeThickness = data.Date == DateTime.Today ? 2 : 0,
                        Stroke = Color.FromArgb("#0A84FF"),
                        StrokeShape = new RoundRectangle { CornerRadius = 8 },
                        Padding = 4,
                        HeightRequest = 60,
                        InputTransparent = false // Allow bubbling
                    };
                    
                    var diaStack = new VerticalStackLayout 
                    { 
                        Spacing = 0, 
                        VerticalOptions = LayoutOptions.Center,
                        InputTransparent = false // Allow bubbling
                    };
                    
                    // Dia Label
                    var lblDia = new Label 
                    { 
                        Text = diaAtual.ToString(), 
                        FontSize = 14, 
                        FontAttributes = FontAttributes.Bold,
                        HorizontalTextAlignment = TextAlignment.Center,
                        InputTransparent = false // Allow bubbling
                    };
                    
                    if (temHoras)
                    {
                        lblDia.TextColor = Color.FromArgb("#0A84FF");
                    }
                    else if (isWeekend)
                    {
                        lblDia.TextColor = Color.FromArgb("#8E8E93");
                    }
                    else
                    {
                        lblDia.SetAppThemeColor(Label.TextColorProperty, Colors.Black, Colors.White);
                    }
                    
                    diaStack.Add(lblDia);
                    
                    // Horas Normais (Verde)
                    if (totalNormais > 0)
                    {
                        var lblNormais = new Label 
                        { 
                            Text = $"{totalNormais:0.##}h", 
                            FontSize = 10, 
                            TextColor = Color.FromArgb("#34C759"),
                            HorizontalTextAlignment = TextAlignment.Center,
                            InputTransparent = false
                        };
                        diaStack.Add(lblNormais);
                    }

                    // Horas Extras (Laranja)
                    if (totalExtras > 0)
                    {
                        var lblExtras = new Label 
                        { 
                            Text = $"{totalExtras:0.##}h", 
                            FontSize = 10, 
                            TextColor = Color.FromArgb("#FF9500"),
                            HorizontalTextAlignment = TextAlignment.Center,
                            InputTransparent = false
                        };
                        diaStack.Add(lblExtras);
                    }
                    
                    diaBorder.Content = diaStack;

                    // Captura de toque via GestureRecognizer aplicada ao Grid e ao Border para maximizar hit-test
                    async Task HandleTapAsync()
                    {
                        Console.WriteLine($"[CalendarioIOS] Tap detectado em {data:yyyy-MM-dd}");
                        try
                        {
                            await diaBorder.FadeTo(0.5, 50);
                            await diaBorder.FadeTo(1.0, 50);
                        }
                        catch
                        {
                            // Ignorar efeitos de animação se a view já não estiver visível
                        }

                        await OnDiaCalendarioTapped(data, horasDoDia);
                    }

                    var tapGestureCell = new TapGestureRecognizer();
                    tapGestureCell.Tapped += async (s, e) => await HandleTapAsync();
                    cellGrid.GestureRecognizers.Add(tapGestureCell);

                    cellGrid.Add(diaBorder);
                    
                    grid.Add(cellGrid, col, semana);
                    
                    diaAtual++;
                }
                else
                {
                    // Espaço vazio
                }
            }
        }

        return grid;
    }

    private async Task OnDiaCalendarioTapped(DateTime data, List<HoraColaborador> horasExistentes)
    {
        try
        {
            // DEBUG: Verificar se o click chega aqui
            await DisplayAlert("Debug Click", $"Dia clicado: {data:dd/MM/yyyy}", "OK");

            bool showColabName = _vm.ColaboradorSelecionado == null || _vm.ColaboradorSelecionado.ID == 0;
            var popup = new NAVIGEST.iOS.Popups.DailySummaryPopup(data, horasExistentes ?? new List<HoraColaborador>(), showColabName);
            
            var result = await this.ShowPopupAsync(popup);

            if (result is DateTime dateToAdd)
            {
                var novaHora = new HoraColaborador
                {
                    DataTrabalho = dateToAdd,
                    HorasTrab = 0,
                    HorasExtras = 0,
                    IdColaborador = _vm.ColaboradorSelecionado?.ID ?? 0
                };
                await AbrirNovaHoraPopupAsync(novaHora);
            }
            else if (result is HoraColaborador horaToEdit)
            {
                await AbrirNovaHoraPopupAsync(horaToEdit);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao abrir popup dia: {ex.Message}");
        }
    }
    
    private async Task AbrirNovaHoraPopupAsync(HoraColaborador? hora)
    {
        try
        {
            var horaParaEditar = hora ?? new HoraColaborador
            {
                DataTrabalho = DateTime.Today,
                HorasTrab = 0,
                HorasExtras = 0,
                IdColaborador = _vm.ColaboradorSelecionado?.ID ?? 0
            };
            
            var popup = new NAVIGEST.iOS.Popups.NovaHoraPopup(horaParaEditar, _vm.Colaboradores.ToList());
            var result = await this.ShowPopupAsync(popup);
            
            if (result is HoraColaborador horaResultado)
            {
                if (horaResultado.Id == -1)
                {
                    // Eliminado
                }
                
                // Refresh data
                await _vm.CarregarHorasCommand.ExecuteAsync(null);
                
                // Refresh calendar if active
                if (_vm.TabAtiva == 3)
                {
                    CarregarTab3Calendario();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao abrir popup nova hora: {ex.Message}");
            await DisplayAlert("Erro", $"Erro ao abrir popup: {ex.Message}", "OK");
        }
    }
    
    private void OnSwipedLeft(object sender, SwipedEventArgs e)
    {
        if (_vm.TabAtiva < 3)
        {
            if (_vm.TabAtiva == 1) OnTab2Tapped(this, EventArgs.Empty);
            else if (_vm.TabAtiva == 2) OnTab3Tapped(this, EventArgs.Empty);
        }
    }

    private void OnSwipedRight(object sender, SwipedEventArgs e)
    {
        if (_vm.TabAtiva > 1)
        {
            if (_vm.TabAtiva == 3) OnTab2Tapped(this, EventArgs.Empty);
            else if (_vm.TabAtiva == 2) OnTab1Tapped(this, EventArgs.Empty);
        }
    }
}
