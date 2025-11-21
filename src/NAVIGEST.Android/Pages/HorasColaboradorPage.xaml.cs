using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls.Shapes;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Maui.Alerts;
using NAVIGEST.Android.PageModels;
using NAVIGEST.Android.Models;
using NAVIGEST.Android.Popups;
using NAVIGEST.Android.Services;

namespace NAVIGEST.Android.Pages;

public partial class HorasColaboradorPage : ContentPage
{
    private HorasColaboradorViewModel _vm => (HorasColaboradorViewModel)BindingContext;
    
    private DateTime _dataCalendario = DateTime.Today;

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
        var view = new NAVIGEST.Android.Views.DashboardView();
        view.BindingContext = new NAVIGEST.Android.PageModels.DashboardViewModel();
        return view;
    }
    
    private Border CriarStatCard(string titulo, string valor, string cor)
    {
        var border = new Border
        {
            BackgroundColor = Application.Current?.RequestedTheme == AppTheme.Dark ? Color.FromArgb("#2C2C2E") : Color.FromArgb("#F5F5F7"),
            StrokeThickness = 0,
            Padding = 12,
            StrokeShape = new RoundRectangle { CornerRadius = 8 }
        };
        
        var stack = new VerticalStackLayout { Spacing = 4 };
        stack.Add(new Label { Text = titulo, FontSize = 11, TextColor = Color.FromArgb("#8E8E93") });
        stack.Add(new Label { Text = valor, FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb(cor) });
        
        border.Content = stack;
        return border;
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
        
        // Datas Personalizadas
        var datasLabel = new Label 
        { 
            Text = "Período Personalizado:", 
            FontSize = 13, 
            FontAttributes = FontAttributes.Bold,
            Margin = new Thickness(0, 8, 0, 4)
        };
        filtrosStack.Add(datasLabel);
        
        var datasGrid = new Grid 
        { 
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star) },
            ColumnSpacing = 8
        };
        
        var dataInicioStack = new VerticalStackLayout { Spacing = 4 };
        dataInicioStack.Add(new Label { Text = "Início", FontSize = 12, TextColor = Color.FromArgb("#8E8E93") });
        var dataInicioPicker = new DatePicker
        {
            Date = _vm.DataFiltroInicio,
            Format = "dd/MM/yyyy",
            BackgroundColor = Application.Current?.RequestedTheme == AppTheme.Dark ? Color.FromArgb("#2C2C2E") : Color.FromArgb("#F5F5F7"),
            TextColor = Application.Current?.RequestedTheme == AppTheme.Dark ? Colors.White : Colors.Black
        };
        dataInicioPicker.DateSelected += (s, e) =>
        {
            _vm.DataFiltroInicio = e.NewDate;
            _ = _vm.CarregarHorasCommand.ExecuteAsync(null);
        };
        dataInicioStack.Add(dataInicioPicker);
        datasGrid.Add(dataInicioStack, 0, 0);
        
        var dataFimStack = new VerticalStackLayout { Spacing = 4 };
        dataFimStack.Add(new Label { Text = "Fim", FontSize = 12, TextColor = Color.FromArgb("#8E8E93") });
        var dataFimPicker = new DatePicker
        {
            Date = _vm.DataFiltroFim,
            Format = "dd/MM/yyyy",
            BackgroundColor = Application.Current?.RequestedTheme == AppTheme.Dark ? Color.FromArgb("#2C2C2E") : Color.FromArgb("#F5F5F7"),
            TextColor = Application.Current?.RequestedTheme == AppTheme.Dark ? Colors.White : Colors.Black
        };
        dataFimPicker.DateSelected += (s, e) =>
        {
            _vm.DataFiltroFim = e.NewDate;
            _ = _vm.CarregarHorasCommand.ExecuteAsync(null);
        };
        dataFimStack.Add(dataFimPicker);
        datasGrid.Add(dataFimStack, 1, 0);
        
        filtrosStack.Add(datasGrid);
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
                System.Diagnostics.Debug.WriteLine($">>> SWIPE EDITAR - IdCliente: '{hora.IdCliente}', Cliente: '{hora.Cliente}'");
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
    
    private async Task AbrirNovaHoraPopupAsync(HoraColaborador? hora)
    {
        try
        {
            // Se hora é null, criar novo objeto
            var horaParaEditar = hora ?? new HoraColaborador
            {
                DataTrabalho = DateTime.Today,
                HorasTrab = 0,
                HorasExtras = 0,
                IdColaborador = _vm.ColaboradorSelecionado?.ID ?? 0
            };
            
            var popup = new NovaHoraPopup(horaParaEditar, _vm.Colaboradores.ToList());
            var page = Application.Current?.Windows[0]?.Page;
            if (page != null)
            {
                var result = await page.ShowPopupAsync(popup);
                
                // Popup retorna HoraColaborador quando salva/edita com sucesso, ou Id=-1 quando elimina
                if (result is HoraColaborador horaResultado)
                {
                    if (horaResultado.Id == -1)
                    {
                        // Foi eliminado no popup
                        GlobalToast.ShowAsync("Registo eliminado com sucesso", ToastTipo.Sucesso);
                    }
                    // Recarregar lista
                    else
                    {
                        // Foi guardado/atualizado
                        string mensagem = hora?.Id > 0 ? "Registo atualizado com sucesso" : "Registo adicionado com sucesso";
                        GlobalToast.ShowAsync(mensagem, ToastTipo.Sucesso);
                    }
                    await _vm.CarregarHorasCommand.ExecuteAsync(null);
                }
            }
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex, mostrarAlerta: true);
        }
    }

    // ==================== TAB 3: CALENDÁRIO ====================
    private View CriarConteudoTab3()
    {
        var scroll = new ScrollView { Padding = 16 };
        var mainStack = new VerticalStackLayout { Spacing = 16 };
        
        var lblTitulo = new Label 
        { 
            Text = "📅 Calendário do Mês", 
            FontSize = 20, 
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center 
        };
        mainStack.Add(lblTitulo);

        // --- 1. Picker de Colaborador ---
        var pickerContainer = new Border
        {
            Stroke = Color.FromArgb("#E5E5EA"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(12, 0),
            Margin = new Thickness(0, 0, 0, 8)
        };
        // FIX: Cor de fundo adaptativa para o container do Picker
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
                // FIX: Usar o método otimizado de carregamento
                await AtualizarFiltroCalendarioAsync();
            }
        };
        
        pickerContainer.Content = picker;
        mainStack.Add(pickerContainer);
        
        // --- 2. Navegação do mês ---
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
        btnPrev.Clicked += async (s, e) => 
        {
            _dataCalendario = _dataCalendario.AddMonths(-1);
            await AtualizarFiltroCalendarioAsync();
        };
        
        var btnNext = new Button 
        { 
            Text = "▶", 
            BackgroundColor = Color.FromArgb("#0A84FF"), 
            TextColor = Colors.White, 
            CornerRadius = 20, 
            WidthRequest = 40, 
            HeightRequest = 40 
        };
        btnNext.Clicked += async (s, e) => 
        {
            _dataCalendario = _dataCalendario.AddMonths(1);
            await AtualizarFiltroCalendarioAsync();
        };
        
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
        
        // --- 3. Cabeçalhos dos dias da semana ---
        var diasSemanaGrid = new Grid 
        { 
            ColumnDefinitions = new ColumnDefinitionCollection 
            { 
                new(GridLength.Star), new(GridLength.Star), new(GridLength.Star), new(GridLength.Star), 
                new(GridLength.Star), new(GridLength.Star), new(GridLength.Star) 
            }
        };
        
        // Começando à Segunda-feira como pedido ("seg, etc...")
        var diasSemana = new[] { "Seg", "Ter", "Qua", "Qui", "Sex", "Sáb", "Dom" };
        for (int i = 0; i < 7; i++)
        {
            var lbl = new Label 
            { 
                Text = diasSemana[i], 
                FontSize = 12, 
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#8E8E93"),
                HorizontalTextAlignment = TextAlignment.Center,
                Padding = 8
            };
            diasSemanaGrid.Add(lbl, i, 0);
        }
        mainStack.Add(diasSemanaGrid);
        
        // --- 4. Grid do calendário ---
        var calendarGrid = new Grid 
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
        var diasNoMes = DateTime.DaysInMonth(_dataCalendario.Year, _dataCalendario.Month);
        
        // Ajuste para começar na Segunda (Monday = 1, Sunday = 7)
        // DayOfWeek: Sunday=0, Monday=1... Saturday=6
        // Se Monday(1) -> offset 0. Se Sunday(0) -> offset 6.
        int diaSemanaPrimeiro = ((int)primeiroDiaMes.DayOfWeek == 0) ? 6 : (int)primeiroDiaMes.DayOfWeek - 1;
        
        // OTIMIZAÇÃO: Criar dicionário para acesso rápido (O(1)) em vez de Where (O(N)) dentro do loop
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
                    
                    // Usar o dicionário otimizado
                    var horasDoDia = horasDict[data].ToList();
                    
                    float totalNormais = horasDoDia.Sum(h => h.HorasTrab);
                    float totalExtras = horasDoDia.Sum(h => h.HorasExtras);
                    bool temHoras = totalNormais > 0 || totalExtras > 0;
                    
                    // --- 5. Cor de Fim de Semana ---
                    bool isWeekend = data.DayOfWeek == DayOfWeek.Saturday || data.DayOfWeek == DayOfWeek.Sunday;
                    
                    // FIX: Cores adaptativas para Dark/Light Mode
                    var bgColor = Colors.Transparent;
                    if (isWeekend)
                    {
                        // Fim de semana: Cinza claro no Light, Cinza escuro no Dark
                        bgColor = Application.Current?.RequestedTheme == AppTheme.Dark 
                            ? Color.FromArgb("#1C1C1E") 
                            : Color.FromArgb("#F2F2F7");
                    }
                    
                    if (temHoras) bgColor = Color.FromArgb("#0A84FF").WithAlpha(0.1f);

                    // --- CÉLULA CONTAINER ---
                    // Usamos um Grid para sobrepor o conteúdo visual e a área de toque
                    var cellGrid = new Grid();

                    // 1. Conteúdo Visual
                    var diaBorder = new Border
                    {
                        BackgroundColor = bgColor,
                        StrokeThickness = data.Date == DateTime.Today ? 2 : 0,
                        Stroke = Color.FromArgb("#0A84FF"),
                        StrokeShape = new RoundRectangle { CornerRadius = 8 },
                        Padding = 4,
                        HeightRequest = 60,
                        InputTransparent = true // O toque será capturado pelo BoxView Overlay
                    };
                    
                    var diaStack = new VerticalStackLayout 
                    { 
                        Spacing = 0, 
                        VerticalOptions = LayoutOptions.Center,
                        InputTransparent = true
                    };
                    
                    // Dia Label
                    var lblDia = new Label 
                    { 
                        Text = diaAtual.ToString(), 
                        FontSize = 14, 
                        FontAttributes = FontAttributes.Bold,
                        HorizontalTextAlignment = TextAlignment.Center,
                        InputTransparent = true
                    };
                    
                    // FIX: Cor do texto adaptativa
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
                    
                    // --- 7. Horas Normais (Verde) ---
                    if (totalNormais > 0)
                    {
                        var lblNormais = new Label 
                        { 
                            Text = $"{totalNormais:0.##}h", 
                            FontSize = 10, 
                            TextColor = Color.FromArgb("#34C759"), // Green
                            HorizontalTextAlignment = TextAlignment.Center,
                            InputTransparent = true
                        };
                        diaStack.Add(lblNormais);
                    }

                    // --- 8. Horas Extras (Laranja) ---
                    if (totalExtras > 0)
                    {
                        var lblExtras = new Label 
                        { 
                            Text = $"{totalExtras:0.##}h", 
                            FontSize = 10, 
                            TextColor = Color.FromArgb("#FF9500"), // Orange
                            HorizontalTextAlignment = TextAlignment.Center,
                            InputTransparent = true
                        };
                        diaStack.Add(lblExtras);
                    }
                    
                    diaBorder.Content = diaStack;
                    cellGrid.Add(diaBorder);

                    // 2. Overlay de Toque (Button Transparente)
                    // FIX: Usar Button transparente para garantir captura do clique em Android
                    var btnOverlay = new Button 
                    { 
                        BackgroundColor = Colors.Transparent,
                        BorderColor = Colors.Transparent,
                        CornerRadius = 0,
                        Margin = 0,
                        Padding = 0,
                        HorizontalOptions = LayoutOptions.Fill,
                        VerticalOptions = LayoutOptions.Fill,
                        Opacity = 0.01 // Truque para garantir hit-test em algumas versões
                    };
                    
                    btnOverlay.Clicked += async (s, e) => await OnDiaCalendarioTapped(data, horasDoDia);
                    
                    cellGrid.Add(btnOverlay);
                    
                    calendarGrid.Add(cellGrid, col, semana);
                    
                    diaAtual++;
                }
                else
                {
                    calendarGrid.Add(new BoxView { BackgroundColor = Colors.Transparent }, col, semana);
                }
            }
        }
        
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

    private async Task AtualizarFiltroCalendarioAsync()
    {
        var inicio = new DateTime(_dataCalendario.Year, _dataCalendario.Month, 1);
        var fim = inicio.AddMonths(1).AddDays(-1);
        
        _vm.DataFiltroInicio = inicio;
        _vm.DataFiltroFim = fim;
        
        // FIX: Carregamento direto para evitar bloqueios do ViewModel (IsBusy)
        // e garantir que os dados são atualizados antes de desenhar o calendário
        try 
        {
            int? idColaboradorFiltro = _vm.ColaboradorSelecionado?.ID == 0 ? null : _vm.ColaboradorSelecionado?.ID;
            var horas = await DatabaseService.GetHorasColaboradorAsync(idColaboradorFiltro, inicio, fim);
            
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

    private async Task OnDiaCalendarioTapped(DateTime data, List<HoraColaborador> horasExistentes)
    {
        try
        {
            // Verificar se devemos mostrar o nome do colaborador (se "Todos" estiver selecionado)
            bool showColabName = _vm.ColaboradorSelecionado == null || _vm.ColaboradorSelecionado.ID == 0;

            var popup = new DailySummaryPopup(data, horasExistentes ?? new List<HoraColaborador>(), showColabName);
            var page = Application.Current?.Windows[0]?.Page;
            
            if (page != null)
            {
                var result = await page.ShowPopupAsync(popup);

                // Se o resultado for uma data, significa que o utilizador clicou em "+"
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
                    await AtualizarFiltroCalendarioAsync();
                }
                // Se o resultado for HoraColaborador, significa que clicou em "Editar"
                else if (result is HoraColaborador horaToEdit)
                {
                    await AbrirNovaHoraPopupAsync(horaToEdit);
                    await AtualizarFiltroCalendarioAsync();
                }
            }
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex, mostrarAlerta: true);
        }
    }

    private void OnSwipedLeft(object sender, SwipedEventArgs e)
    {
        if (_vm.TabAtiva < 3)
        {
            int novaTab = _vm.TabAtiva + 1;
            if (novaTab == 2) OnTab2Tapped(this, EventArgs.Empty);
            else if (novaTab == 3) OnTab3Tapped(this, EventArgs.Empty);
        }
    }

    private void OnSwipedRight(object sender, SwipedEventArgs e)
    {
        if (_vm.TabAtiva > 1)
        {
            int novaTab = _vm.TabAtiva - 1;
            if (novaTab == 1) OnTab1Tapped(this, EventArgs.Empty);
            else if (novaTab == 2) OnTab2Tapped(this, EventArgs.Empty);
        }
    }

    // ==================== CONVERTERS ====================
    private class TotalHorasConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            if (value is HoraColaborador h)
            {
                float total = h.HorasTrab + h.HorasExtras;
                return $"{total:0.0}h";
            }
            return "0h";
        }
        
        public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
            => throw new NotImplementedException();
    }
    
    private class HorasExtraConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            if (value is float horas)
                return horas > 0;
            return false;
        }
        
        public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
            => throw new NotImplementedException();
    }
    
    private class StringNotEmptyConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            return !string.IsNullOrWhiteSpace(value as string);
        }
        
        public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
            => throw new NotImplementedException();
    }

    private async void OnManageAbsenceTypesClicked(object? sender, EventArgs e)
    {
        try
        {
            // Tenta navegar via Shell Route
            await Shell.Current.GoToAsync(nameof(AbsenceTypesPage));
        }
        catch (Exception ex)
        {
            // Fallback ou erro
            System.Diagnostics.Debug.WriteLine($"Erro ao navegar: {ex.Message}");
            await DisplayAlert("Erro", $"Não foi possível abrir a página de gestão de ausências.\nErro: {ex.Message}", "OK");
        }
    }
}
