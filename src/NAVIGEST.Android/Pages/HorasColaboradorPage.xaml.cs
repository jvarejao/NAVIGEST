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
        var scroll = new ScrollView { Padding = 16 };
        var mainStack = new VerticalStackLayout { Spacing = 16 };
        
        // Header: Período + Colaborador
        var headerGrid = new Grid 
        { 
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star) },
            ColumnSpacing = 12
        };
        
        var periodoStack = new VerticalStackLayout();
        periodoStack.Add(new Label { Text = "📅 Período", FontSize = 12, TextColor = Color.FromArgb("#8E8E93") });
        periodoStack.Add(new Label { Text = _vm.PeriodoSelecionado, FontSize = 13, FontAttributes = FontAttributes.Bold });
        headerGrid.Add(periodoStack, 0, 0);
        
        var colabStack = new VerticalStackLayout();
        colabStack.Add(new Label { Text = "👤 Colaborador", FontSize = 12, TextColor = Color.FromArgb("#8E8E93") });
        colabStack.Add(new Label { Text = _vm.ColaboradorDisplay, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0A84FF") });
        headerGrid.Add(colabStack, 1, 0);
        
        mainStack.Add(headerGrid);
        
        // Card Total Geral
        var totalBorder = new Border
        {
            BackgroundColor = Application.Current?.RequestedTheme == AppTheme.Dark ? Color.FromArgb("#2C2C2E") : Color.FromArgb("#F5F5F7"),
            StrokeThickness = 0,
            Padding = 16,
            StrokeShape = new RoundRectangle { CornerRadius = 12 }
        };
        
        var totalGrid = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Auto) } };
        var totalStack = new VerticalStackLayout();
        totalStack.Add(new Label { Text = "Total Horas", FontSize = 14, TextColor = Color.FromArgb("#8E8E93") });
        totalStack.Add(new Label { Text = _vm.TotalGeral, FontSize = 32, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#34C759") });
        totalGrid.Add(totalStack, 0, 0);
        
        if (_vm.TemExtras)
        {
            totalGrid.Add(new Label 
            { 
                Text = _vm.AlertaExtras,
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#FF9500"),
                VerticalOptions = LayoutOptions.Center
            }, 1, 0);
        }
        
        totalBorder.Content = totalGrid;
        mainStack.Add(totalBorder);
        
        // Stats Grid: Normal | Extra | Média
        var statsGrid = new Grid 
        { 
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star), new(GridLength.Star) },
            ColumnSpacing = 12
        };
        
        statsGrid.Add(CriarStatCard("📊 Normal", _vm.TotalHorasNormais, "#0A84FF"), 0, 0);
        statsGrid.Add(CriarStatCard("⚡ Extra", _vm.TotalHorasExtra, "#FF9500"), 1, 0);
        statsGrid.Add(CriarStatCard("📈 Média/dia", $"{_vm.MediaHorasDia:0.0}h", "#34C759"), 2, 0);
        
        mainStack.Add(statsGrid);
        
        // Breakdown Info
        var infoLabel = new Label
        {
            FontSize = 13,
            TextColor = Color.FromArgb("#8E8E93"),
            HorizontalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        };
        infoLabel.FormattedText = new FormattedString();
        infoLabel.FormattedText.Spans.Add(new Span { Text = "👥 " });
        infoLabel.FormattedText.Spans.Add(new Span { Text = _vm.TotalColaboradores.ToString(), FontAttributes = FontAttributes.Bold });
        infoLabel.FormattedText.Spans.Add(new Span { Text = " colaboradores | 📅 " });
        infoLabel.FormattedText.Spans.Add(new Span { Text = _vm.TotalDias.ToString(), FontAttributes = FontAttributes.Bold });
        infoLabel.FormattedText.Spans.Add(new Span { Text = " dias" });
        mainStack.Add(infoLabel);
        
        // Top Colaboradores (se houver múltiplos)
        if (_vm.TotalColaboradores > 1)
        {
            mainStack.Add(new Label 
            { 
                Text = "🏆 Top Colaboradores", 
                FontSize = 16, 
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(0, 16, 0, 8)
            });
            
            var topColabs = _vm.HorasList
                .GroupBy(h => new { h.IdColaborador, h.NomeColaborador })
                .Select(g => new { Nome = g.Key.NomeColaborador, Total = g.Sum(h => h.HorasTrab + h.HorasExtras) })
                .OrderByDescending(x => x.Total)
                .Take(5);
            
            foreach (var colab in topColabs)
            {
                var colabBorder = new Border
                {
                    BackgroundColor = Application.Current?.RequestedTheme == AppTheme.Dark ? Color.FromArgb("#2C2C2E") : Color.FromArgb("#F5F5F7"),
                    StrokeThickness = 0,
                    Padding = 12,
                    Margin = new Thickness(0, 0, 0, 8),
                    StrokeShape = new RoundRectangle { CornerRadius = 8 }
                };
                
                var colabGrid = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Auto) } };
                colabGrid.Add(new Label { Text = colab.Nome, FontSize = 14, VerticalOptions = LayoutOptions.Center }, 0, 0);
                colabGrid.Add(new Label { Text = $"{colab.Total:0.00}h", FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0A84FF"), VerticalOptions = LayoutOptions.Center }, 1, 0);
                
                colabBorder.Content = colabGrid;
                mainStack.Add(colabBorder);
            }
        }
        
        scroll.Content = mainStack;
        return scroll;
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
            Text = "✏️",
            FontSize = 28,
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
            Text = "🗑️",
            FontSize = 28,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
        deleteGrid.Add(deleteBorder);
        
        var deleteSwipeView = new SwipeItemView { Content = deleteGrid };
        deleteSwipeView.Invoked += async (s, e) =>
        {
            if ((s as SwipeItemView)?.BindingContext is HoraColaborador hora)
            {
                bool confirmacao = await Shell.Current.DisplayAlert(
                    "Confirmar",
                    "Eliminar este registo de horas?",
                    "Sim",
                    "Não"
                );
                
                if (confirmacao)
                {
                    await _vm.EliminarHoraCommand.ExecuteAsync(hora.Id);
                    await Toast.Make("Registo eliminado com sucesso", CommunityToolkit.Maui.Core.ToastDuration.Short).Show();
                }
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
                        await Toast.Make("Registo eliminado com sucesso", CommunityToolkit.Maui.Core.ToastDuration.Short).Show();
                    }
                    // Recarregar lista
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
        
        // Navegação do mês
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
        
        var btnNext = new Button 
        { 
            Text = "▶", 
            BackgroundColor = Color.FromArgb("#0A84FF"), 
            TextColor = Colors.White, 
            CornerRadius = 20, 
            WidthRequest = 40, 
            HeightRequest = 40 
        };
        
        var lblMes = new Label 
        { 
            Text = DateTime.Today.ToString("MMMM yyyy"), 
            FontSize = 18, 
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };
        
        navGrid.Add(btnPrev, 0, 0);
        navGrid.Add(lblMes, 1, 0);
        navGrid.Add(btnNext, 2, 0);
        mainStack.Add(navGrid);
        
        // Cabeçalhos dos dias da semana
        var diasSemanaGrid = new Grid 
        { 
            ColumnDefinitions = new ColumnDefinitionCollection 
            { 
                new(GridLength.Star), new(GridLength.Star), new(GridLength.Star), new(GridLength.Star), 
                new(GridLength.Star), new(GridLength.Star), new(GridLength.Star) 
            }
        };
        
        var diasSemana = new[] { "Dom", "Seg", "Ter", "Qua", "Qui", "Sex", "Sáb" };
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
        
        // Grid do calendário
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
        
        var hoje = DateTime.Today;
        var primeiroDia = new DateTime(hoje.Year, hoje.Month, 1);
        var ultimoDia = primeiroDia.AddMonths(1).AddDays(-1);
        var diasMes = ultimoDia.Day;
        var diaSemanaPrimeiro = (int)primeiroDia.DayOfWeek;
        
        int diaAtual = 1;
        for (int semana = 0; semana < 6; semana++)
        {
            for (int diaSemana = 0; diaSemana < 7; diaSemana++)
            {
                int celula = semana * 7 + diaSemana;
                
                if (celula >= diaSemanaPrimeiro && diaAtual <= diasMes)
                {
                    var data = new DateTime(hoje.Year, hoje.Month, diaAtual);
                    var horasDia = _vm.HorasList.Where(h => h.DataTrabalho.Date == data).Sum(h => h.HorasTrab + h.HorasExtras);
                    
                    var diaBorder = new Border
                    {
                        BackgroundColor = horasDia > 0 ? Color.FromArgb("#0A84FF").WithAlpha(0.2f) : Color.FromArgb("#F5F5F7"),
                        StrokeThickness = data.Date == hoje ? 2 : 0,
                        Stroke = Color.FromArgb("#0A84FF"),
                        StrokeShape = new RoundRectangle { CornerRadius = 8 },
                        Padding = 4,
                        HeightRequest = 60
                    };
                    
                    var diaStack = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
                    var lblDia = new Label 
                    { 
                        Text = diaAtual.ToString(), 
                        FontSize = 14, 
                        FontAttributes = FontAttributes.Bold,
                        TextColor = horasDia > 0 ? Color.FromArgb("#0A84FF") : Color.FromArgb("#000000"),
                        HorizontalTextAlignment = TextAlignment.Center
                    };
                    diaStack.Add(lblDia);
                    
                    if (horasDia > 0)
                    {
                        var lblHoras = new Label 
                        { 
                            Text = $"{horasDia:0.0}h", 
                            FontSize = 10, 
                            TextColor = Color.FromArgb("#34C759"),
                            HorizontalTextAlignment = TextAlignment.Center
                        };
                        diaStack.Add(lblHoras);
                    }
                    
                    diaBorder.Content = diaStack;
                    calendarGrid.Add(diaBorder, diaSemana, semana);
                    
                    diaAtual++;
                }
                else
                {
                    calendarGrid.Add(new BoxView { BackgroundColor = Colors.Transparent }, diaSemana, semana);
                }
            }
        }
        
        mainStack.Add(calendarGrid);
        
        // Legenda
        var legendaStack = new HorizontalStackLayout 
        { 
            Spacing = 16, 
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 16, 0, 0)
        };
        
        var legendaBorder1 = new Border
        {
            BackgroundColor = Color.FromArgb("#0A84FF").WithAlpha(0.2f),
            WidthRequest = 20,
            HeightRequest = 20,
            StrokeShape = new RoundRectangle { CornerRadius = 4 }
        };
        legendaStack.Add(legendaBorder1);
        legendaStack.Add(new Label { Text = "Com horas registadas", FontSize = 12, VerticalTextAlignment = TextAlignment.Center });
        
        mainStack.Add(legendaStack);
        
        scroll.Content = mainStack;
        return scroll;
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
}
