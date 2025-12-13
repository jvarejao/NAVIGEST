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
using NAVIGEST.macOS.Popups;
using NAVIGEST.Shared.Resources.Strings;

namespace NAVIGEST.macOS.Pages;

public partial class HoursEntryPage : ContentPage
{
    private HorasColaboradorViewModel _vm => (HorasColaboradorViewModel)BindingContext;
    
    private DateTime _dataCalendario = DateTime.Today;
    private DateTime _overlayDate;
    
    // Filter State
    private int _selectedYear = DateTime.Today.Year;
    private int _selectedMonth = DateTime.Today.Month;

    // Edit Overlay State
    // Removed as we now use NovaHoraPopup

    // List Picker State
    private TaskCompletionSource<object?>? _pickerTcs;
    private bool _isPickerActive;
    private List<string> _allItems = new();

    public static readonly BindableProperty ShowCollaboratorNameProperty =
        BindableProperty.Create(nameof(ShowCollaboratorName), typeof(bool), typeof(HoursEntryPage), false);

    public bool ShowCollaboratorName
    {
        get => (bool)GetValue(ShowCollaboratorNameProperty);
        set => SetValue(ShowCollaboratorNameProperty, value);
    }

    public HoursEntryPage()
    {
        InitializeComponent();
        
        try
        {
            var services = Application.Current?.Handler?.MauiContext?.Services;
            var vm = services?.GetService<HorasColaboradorViewModel>() ?? new HorasColaboradorViewModel();
            BindingContext = vm;
            InitializeTabContent();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao inicializar: {ex.Message}");
            BindingContext = new HorasColaboradorViewModel();
            InitializeTabContent();
        }
    }

    public HoursEntryPage(HorasColaboradorViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        InitializeTabContent();
    }


    private void InitializeTabContent()
    {
        var user = NAVIGEST.macOS.UserSession.Current.User;
        bool canSeeDashboard = user.IsFinancial || user.IsGeneralSupervisor;

        if (canSeeDashboard)
        {
            CarregarTab1Resumo();
        }
        else
        {
            if (BindingContext is HorasColaboradorViewModel vm)
            {
                vm.TabAtiva = 2;
            }
            AtivarTab(2);
            CarregarTab2Lista();
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_vm != null)
        {
            _vm.HorasList.CollectionChanged += OnHorasListCollectionChanged;
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (_vm != null)
        {
            _vm.HorasList.CollectionChanged -= OnHorasListCollectionChanged;
        }
    }

    private void OnHorasListCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (_vm.TabAtiva == 3)
        {
            MainThread.BeginInvokeOnMainThread(() => CarregarTab3Calendario());
        }
    }

    private void OnTab1Tapped(object sender, EventArgs e)
    {
        AtivarTab(1);
        CarregarTab1Resumo();
    }

    private void OnTab2Tapped(object sender, EventArgs e)
    {
        // Reset to current date when opening Tab 2
        _selectedYear = DateTime.Today.Year;
        _selectedMonth = DateTime.Today.Month;
        
        AtivarTab(2);
        CarregarTab2Lista();
    }

    private void OnTab3Tapped(object sender, EventArgs e)
    {
        AtivarTab(3);
        _dataCalendario = new DateTime(_selectedYear, _selectedMonth, 1);
        _ = AtualizarFiltroCalendarioAsync();
    }

    private void AtivarTab(int numeroTab)
    {
        _vm.TabAtiva = numeroTab;
        
        // Reset styles to Unselected state (using AppTheme aware colors)
        var unselectedTextLight = Color.FromArgb("#6E6E73");
        var unselectedTextDark = Color.FromArgb("#8E8E93");
        var unselectedBgLight = Colors.White;
        var unselectedBgDark = Color.FromArgb("#2C2C2E");

        // Tab 1
        Tab1Border.SetAppThemeColor(Border.BackgroundColorProperty, unselectedBgLight, unselectedBgDark);
        Tab1Label.SetAppThemeColor(Label.TextColorProperty, unselectedTextLight, unselectedTextDark);
        Tab1Label.FontAttributes = FontAttributes.None;

        // Tab 2
        Tab2Border.SetAppThemeColor(Border.BackgroundColorProperty, unselectedBgLight, unselectedBgDark);
        Tab2Label.SetAppThemeColor(Label.TextColorProperty, unselectedTextLight, unselectedTextDark);
        Tab2Label.FontAttributes = FontAttributes.None;

        // Tab 3
        Tab3Border.SetAppThemeColor(Border.BackgroundColorProperty, unselectedBgLight, unselectedBgDark);
        Tab3Label.SetAppThemeColor(Label.TextColorProperty, unselectedTextLight, unselectedTextDark);
        Tab3Label.FontAttributes = FontAttributes.None;
        
        // Apply Selected style
        var selectedColor = Color.FromArgb("#0A84FF");
        
        switch (numeroTab)
        {
            case 1:
                Tab1Border.BackgroundColor = selectedColor; // Override AppTheme color
                Tab1Label.TextColor = Colors.White;
                Tab1Label.FontAttributes = FontAttributes.Bold;
                break;
            case 2:
                Tab2Border.BackgroundColor = selectedColor;
                Tab2Label.TextColor = Colors.White;
                Tab2Label.FontAttributes = FontAttributes.Bold;
                break;
            case 3:
                Tab3Border.BackgroundColor = selectedColor;
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

    private void OnCloseDailySummaryClicked(object sender, EventArgs e)
    {
        DailySummaryOverlay.IsVisible = false;
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
            RowDefinitions = new RowDefinitionCollection 
            { 
                new(GridLength.Auto), // Buttons
                new(GridLength.Auto), // Colab Picker
                new(GridLength.Auto), // Date Selector
                new(GridLength.Star)  // List
            },
            RowSpacing = 12,
            Padding = 16,
            MaximumWidthRequest = 1400,
            HorizontalOptions = LayoutOptions.Center
        };
        
        // 1. Botões de Ação
        var buttonsGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star) },
            ColumnSpacing = 8
        };

        var btnAdicionar = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            BackgroundColor = Color.FromArgb("#0A84FF"),
            Padding = 12,
            StrokeThickness = 0
        };
        var lblAdd = new Label 
        { 
            Text = $"➕ {AppResources.Hours_NewTitle}", 
            TextColor = Colors.White, 
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };
        btnAdicionar.Content = lblAdd;
        
        var tapAdd = new TapGestureRecognizer();
        tapAdd.Tapped += async (s, e) => await AbrirNovaHoraPopupAsync(null);
        btnAdicionar.GestureRecognizers.Add(tapAdd);
        
        var btnTipos = new Button
        {
            Text = $"⚙️ {AppResources.Hours_Absence}",
            BackgroundColor = Color.FromArgb("#E5E5EA"),
            TextColor = Colors.Black,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 12,
            Padding = 12,
            IsVisible = _vm.IsFinancial // Only Financial/Admin
        };
        btnTipos.Clicked += async (s, e) => 
        {
            try
            {
                // Usar Shell.Current.Navigation para garantir o contexto correto
                if (Shell.Current != null)
                {
                    await Shell.Current.Navigation.PushModalAsync(new NAVIGEST.macOS.Popups.GerirTiposAusenciaPopup());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao abrir pagina: {ex}");
                if (Shell.Current != null)
                    await Shell.Current.DisplayAlert("Erro", $"Falha ao abrir gestão de tipos: {ex.Message}", "OK");
            }
        };

        buttonsGrid.Add(btnAdicionar, 0, 0);
        buttonsGrid.Add(btnTipos, 1, 0);
        
        mainGrid.Add(buttonsGrid, 0, 0);
        
        // 2. Picker Colaborador (Custom)
        var pickerContainer = new Border
        {
            Stroke = Color.FromArgb("#E5E5EA"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(12, 8),
            HeightRequest = 44,
            IsEnabled = _vm.IsFinancial // Only Financial/Admin can change collaborator
        };
        pickerContainer.SetAppThemeColor(Border.BackgroundColorProperty, Color.FromArgb("#F5F5F7"), Color.FromArgb("#2C2C2E"));
        
        var pickerGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Auto) }
        };

        var lblPicker = new Label
        {
            FontSize = 16,
            VerticalTextAlignment = TextAlignment.Center
        };
        lblPicker.SetAppThemeColor(Label.TextColorProperty, Colors.Black, Colors.White);
        lblPicker.SetBinding(Label.TextProperty, new Binding("ColaboradorSelecionado.Nome", source: _vm) { TargetNullValue = AppResources.Hours_SelectCollaborator });

        var iconPicker = new Label
        {
            Text = "▼",
            FontSize = 12,
            TextColor = Color.FromArgb("#8E8E93"),
            VerticalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0)
        };

        pickerGrid.Add(lblPicker, 0, 0);
        pickerGrid.Add(iconPicker, 1, 0);
        pickerContainer.Content = pickerGrid;

        var tapPicker = new TapGestureRecognizer();
        tapPicker.Tapped += async (s, e) => 
        {
            var names = _vm.Colaboradores.Select(c => c.Nome).ToList();
            var result = await ShowListPickerAsync(AppResources.Hours_SelectCollaborator, names);
            
            if (result is string action)
            {
                var selected = _vm.Colaboradores.FirstOrDefault(c => c.Nome == action);
                if (selected != null)
                {
                    _vm.ColaboradorSelecionado = selected;
                    _ = _vm.CarregarHorasCommand.ExecuteAsync(null);
                }
            }
        };
        pickerContainer.GestureRecognizers.Add(tapPicker);
        
        mainGrid.Add(pickerContainer, 0, 1);

        // 3. Seletor de Data (Ano + Meses)
        mainGrid.Add(CriarSeletorData(), 0, 2);
        
        // 4. Lista de Horas
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
        emptyView.Add(new Label { Text = AppResources.Hours_NoRecords, FontSize = 16, TextColor = Color.FromArgb("#8E8E93"), HorizontalTextAlignment = TextAlignment.Center });
        listaCollection.EmptyView = emptyView;
        
        mainGrid.Add(listaCollection, 0, 3);
        
        // Trigger initial load
        MainThread.BeginInvokeOnMainThread(async () => await AtualizarFiltroListaAsync());
        
        return mainGrid;
    }
    
    private View CriarItemLista()
    {
        var itemBorder = new Border
        {
            StrokeThickness = 0,
            Padding = 16,
            Margin = new Thickness(0, 0, 0, 12),
            StrokeShape = new RoundRectangle { CornerRadius = 12 }
        };
        itemBorder.SetAppThemeColor(Border.BackgroundColorProperty, Colors.White, Color.FromArgb("#1C1C1E"));
        
        var itemGrid = new Grid 
        { 
            ColumnDefinitions = new ColumnDefinitionCollection 
            { 
                new(GridLength.Star), // Info
                new(GridLength.Auto), // Hours
                new(GridLength.Auto)  // Actions
            },
            RowDefinitions = new RowDefinitionCollection 
            { 
                new(GridLength.Auto), 
                new(GridLength.Auto) 
            },
            ColumnSpacing = 16
        };
        
        // Col 0: Info
        var infoStack = new VerticalStackLayout { Spacing = 4 };
        
        var lblData = new Label { FontSize = 16, FontAttributes = FontAttributes.Bold };
        lblData.SetBinding(Label.TextProperty, new Binding("DataTrabalho", stringFormat: "{0:dd/MM/yyyy - ddd}"));
        lblData.SetAppThemeColor(Label.TextColorProperty, Colors.Black, Colors.White);
        infoStack.Add(lblData);
        
        var lblNome = new Label { FontSize = 14, TextColor = Color.FromArgb("#8E8E93") };
        lblNome.SetBinding(Label.TextProperty, new Binding("NomeColaborador"));
        infoStack.Add(lblNome);
        
        var clientStack = new HorizontalStackLayout { Spacing = 6, VerticalOptions = LayoutOptions.Center };

        var lblIcon = new Label 
        { 
            FontFamily = "FA7Solid",
            FontSize = 14,
            TextColor = Color.FromArgb("#FF2D55"),
            VerticalTextAlignment = TextAlignment.Center
        };
        lblIcon.SetBinding(Label.TextProperty, new Binding("DisplayIcon"));
        clientStack.Add(lblIcon);

        var lblCliente = new Label 
        { 
            FontSize = 13, 
            TextColor = Color.FromArgb("#8E8E93"),
            VerticalTextAlignment = TextAlignment.Center
        };
        lblCliente.SetBinding(Label.TextProperty, new Binding("DisplayText"));
        clientStack.Add(lblCliente);

        infoStack.Add(clientStack);
        
        itemGrid.Add(infoStack, 0, 0);
        Grid.SetRowSpan(infoStack, 2);
        
        // Col 1: Hours
        var horasStack = new VerticalStackLayout 
        { 
            Spacing = 4,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.End
        };
        
        var lblHorasNormais = new Label 
        { 
            FontSize = 18, 
            FontAttributes = FontAttributes.Bold, 
            TextColor = Color.FromArgb("#34C759"),
            HorizontalTextAlignment = TextAlignment.End
        };
        lblHorasNormais.SetBinding(Label.TextProperty, new Binding("HorasTrab", stringFormat: "✓ {0:0.##}h"));
        horasStack.Add(lblHorasNormais);
        
        var lblHorasExtra = new Label 
        { 
            FontSize = 16, 
            FontAttributes = FontAttributes.Bold, 
            TextColor = Color.FromArgb("#FF9500"),
            HorizontalTextAlignment = TextAlignment.End
        };
        lblHorasExtra.SetBinding(Label.TextProperty, new Binding("HorasExtras", stringFormat: "⚡ {0:0.##}h"));
        horasStack.Add(lblHorasExtra);
        
        itemGrid.Add(horasStack, 1, 0);
        Grid.SetRowSpan(horasStack, 2);

        // Col 2: Actions
        var actionsStack = new HorizontalStackLayout { Spacing = 12, VerticalOptions = LayoutOptions.Center };
        
        var btnEdit = new Border 
        { 
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            BackgroundColor = Color.FromArgb("#0A84FF").WithAlpha(0.1f), 
            WidthRequest = 44,
            HeightRequest = 44,
            StrokeThickness = 0,
            Padding = 0
        };
        var lblEdit = new Label
        {
            Text = "\uf044", // fa-pencil-alt
            FontFamily = "FA7Solid",
            TextColor = Color.FromArgb("#0A84FF"),
            FontSize = 18,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };
        btnEdit.Content = lblEdit;

        var tapEdit = new TapGestureRecognizer();
        tapEdit.Tapped += async (s, e) => 
        {
             if ((s as Element)?.BindingContext is HoraColaborador hora)
                await AbrirNovaHoraPopupAsync(hora);
        };
        btnEdit.GestureRecognizers.Add(tapEdit);
        actionsStack.Add(btnEdit);

        var btnDelete = new Border 
        { 
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            BackgroundColor = Color.FromArgb("#FF3B30").WithAlpha(0.1f), 
            WidthRequest = 44,
            HeightRequest = 44,
            StrokeThickness = 0,
            Padding = 0
        };
        var lblDelete = new Label
        {
            Text = "\uf2ed", // fa-trash-alt
            FontFamily = "FA7Solid",
            TextColor = Color.FromArgb("#FF3B30"),
            FontSize = 18,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };
        btnDelete.Content = lblDelete;

        var tapDelete = new TapGestureRecognizer();
        tapDelete.Tapped += async (s, e) => 
        {
             if ((s as Element)?.BindingContext is HoraColaborador hora)
                await _vm.EliminarHoraCommand.ExecuteAsync(hora);
        };
        btnDelete.GestureRecognizers.Add(tapDelete);
        actionsStack.Add(btnDelete);

        itemGrid.Add(actionsStack, 2, 0);
        Grid.SetRowSpan(actionsStack, 2);
        
        itemBorder.Content = itemGrid;
        
        return itemBorder;
    }
       
    private async Task MostrarMenuPeriodoAsync()
    {
        var result = await ShowListPickerAsync(AppResources.Hours_SelectPeriod, new List<string> { AppResources.Hours_PeriodToday, AppResources.Hours_PeriodThisWeek, AppResources.Hours_PeriodThisMonth, AppResources.Hours_PeriodLast30Days });
        
        if (result is string action)
        {
            if (action == AppResources.Hours_PeriodToday) _vm.SelecionarHojeCommand.Execute(null);
            else if (action == AppResources.Hours_PeriodThisWeek) _vm.SelecionarEstaSemanaCommand.Execute(null);
            else if (action == AppResources.Hours_PeriodThisMonth) _vm.SelecionarEsteMesCommand.Execute(null);
            else if (action == AppResources.Hours_PeriodLast30Days) _vm.SelecionarUltimos30DiasCommand.Execute(null);
        }
    }

    private void OnCloseNovaHoraOverlayClicked(object sender, EventArgs e)
    {
        NovaHoraOverlay.IsVisible = false;
        NovaHoraContent.Content = null;
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

            // Ensure collaborators are loaded
            if (_vm.Colaboradores.Count == 0)
            {
                await _vm.CarregarColaboradoresAsync();
            }

            // Use Overlay View instead of Popup
            var view = new NAVIGEST.macOS.Views.NovaHoraView(horaParaEditar, _vm.Colaboradores.ToList(), async (horaSalva) => 
            {
                NovaHoraOverlay.IsVisible = false;
                NovaHoraContent.Content = null;

                if (horaSalva != null)
                {
                    if (horaSalva.Id == -1)
                    {
                        // Deleted
                        if (hora != null)
                        {
                            _vm.HorasList.Remove(hora);
                        }
                        await AtualizarFiltroListaAsync(); // Refresh totals
                    }
                    else
                    {
                        // Saved/Updated
                        await AtualizarFiltroListaAsync();
                    }
                    
                    // Force Calendar Refresh
                    if (_vm.TabAtiva == 3) 
                    {
                        MainThread.BeginInvokeOnMainThread(() => CarregarTab3Calendario());
                    }
                }
            });

            NovaHoraContent.Content = view;
            NovaHoraOverlay.IsVisible = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao abrir popup nova hora: {ex.Message}");
            await DisplayAlert(AppResources.Common_Error, $"Erro ao abrir popup: {ex.Message}", AppResources.Common_OK);
        }
    }



    // --- Generic List Picker ---

    private async Task<object?> ShowListPickerAsync(string title, List<string> items)
    {
        var popup = new GenericPickerPopup(title, items);
        var result = await this.ShowPopupAsync(popup);
        return result;
    }

    private void OnListPickerSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = e.NewTextValue;
        ListPickerClearButton.IsVisible = !string.IsNullOrEmpty(searchText);

        if (string.IsNullOrWhiteSpace(searchText))
        {
            ListPickerCollectionView.ItemsSource = _allItems;
        }
        else
        {
            ListPickerCollectionView.ItemsSource = _allItems
                .Where(i => i.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }
    }

    private void OnListPickerClearSearchClicked(object sender, EventArgs e)
    {
        ListPickerSearchEntry.Text = string.Empty;
        // TextChanged event will handle resetting the list
    }

    private void OnCloseListPickerClicked(object sender, EventArgs e)
    {
        ListPickerOverlay.IsVisible = false;
        _pickerTcs?.TrySetResult(null);
    }

    private void OnListPickerSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is string selectedItem)
        {
            Console.WriteLine($"DEBUG: ListPicker selected item: {selectedItem}");
            ListPickerOverlay.IsVisible = false;
            _pickerTcs?.TrySetResult(selectedItem);
            ListPickerCollectionView.SelectedItem = null;
        }
    }

    private void OnListPickerItemTapped(object sender, EventArgs e)
    {
        if (sender is Element element && element.BindingContext is string selectedItem)
        {
            Console.WriteLine($"DEBUG: ListPicker item tapped: {selectedItem}");
            ListPickerOverlay.IsVisible = false;
            _pickerTcs?.TrySetResult(selectedItem);
        }
    }

    // ==================== TAB 3: CALENDÁRIO ====================
    private View CriarConteudoTab3()
    {
        if (_vm.Colaboradores.Count == 0)
        {
             _ = _vm.CarregarColaboradoresAsync();
        }

        var scroll = new ScrollView();
        var mainStack = new VerticalStackLayout 
        { 
            Spacing = 24, 
            Padding = 24,
            MaximumWidthRequest = 1400,
            HorizontalOptions = LayoutOptions.Center
        };
        
        var lblTitulo = new Label 
        { 
            Text = $"📅 {AppResources.Hours_CalendarMonth}", 
            FontSize = 24, 
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center 
        };
        mainStack.Add(lblTitulo);

        var pickerContainer = new Border
        {
            Stroke = Color.FromArgb("#E5E5EA"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(12, 8),
            Margin = new Thickness(0, 0, 0, 16),
            HeightRequest = 44,
            IsEnabled = _vm.IsFinancial // Only Financial/Admin
        };
        
        pickerContainer.SetAppThemeColor(Border.BackgroundColorProperty, Colors.White, Color.FromArgb("#111827"));
        pickerContainer.SetAppThemeColor(Border.StrokeProperty, Color.FromArgb("#E5E5EA"), Color.FromArgb("#38383A"));

        var pickerGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Auto) }
        };

        var lblPicker = new Label
        {
            Text = _vm.ColaboradorSelecionado?.Nome ?? AppResources.Hours_SelectCollaborator,
            FontSize = 16,
            VerticalTextAlignment = TextAlignment.Center
        };
        lblPicker.SetAppThemeColor(Label.TextColorProperty, Colors.Black, Colors.White);

        var iconPicker = new Label
        {
            Text = "▼",
            FontSize = 12,
            TextColor = Color.FromArgb("#8E8E93"),
            VerticalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0)
        };

        pickerGrid.Add(lblPicker, 0, 0);
        pickerGrid.Add(iconPicker, 1, 0);
        pickerContainer.Content = pickerGrid;

        var tapPicker = new TapGestureRecognizer();
        tapPicker.Tapped += async (s, e) => 
        {
            var result = await ShowListPickerAsync(AppResources.Hours_SelectCollaborator, _vm.Colaboradores.Select(c => c.Nome).ToList());
            
            if (result is string action)
            {
                var selected = _vm.Colaboradores.FirstOrDefault(c => c.Nome == action);
                if (selected != null)
                {
                    _vm.ColaboradorSelecionado = selected;
                    lblPicker.Text = selected.Nome;
                    await AtualizarFiltroCalendarioAsync();
                }
            }
        };
        pickerContainer.GestureRecognizers.Add(tapPicker);
        
        mainStack.Add(pickerContainer);
        
        var navGrid = new Grid 
        { 
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) },
            Margin = new Thickness(0, 0, 0, 24)
        };
        
        var btnPrev = new Button 
        { 
            Text = "◀", 
            BackgroundColor = Color.FromArgb("#0A84FF"), 
            TextColor = Colors.White, 
            CornerRadius = 20, 
            WidthRequest = 44, 
            HeightRequest = 44,
            FontSize = 18
        };
        btnPrev.Clicked += (s, e) => MudarMes(-1);
        
        var btnNext = new Button 
        { 
            Text = "▶", 
            BackgroundColor = Color.FromArgb("#0A84FF"), 
            TextColor = Colors.White, 
            CornerRadius = 20, 
            WidthRequest = 44, 
            HeightRequest = 44,
            FontSize = 18
        };
        btnNext.Clicked += (s, e) => MudarMes(1);
        
        var lblMes = new Label 
        { 
            Text = _dataCalendario.ToString("MMMM yyyy").ToUpper(), 
            FontSize = 20, 
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };
        lblMes.SetAppThemeColor(Label.TextColorProperty, Colors.Black, Colors.White);
        
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
            },
            Margin = new Thickness(0,0,0,8),
            HorizontalOptions = LayoutOptions.Fill
        };
        
        string[] diasSemana = { "Seg", "Ter", "Qua", "Qui", "Sex", "Sáb", "Dom" };
        for (int i = 0; i < 7; i++)
        {
            var lblDiaSemana = new Label 
            { 
                Text = diasSemana[i], 
                FontSize = 14, 
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center 
            };
            lblDiaSemana.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb("#3A3A3C"), Color.FromArgb("#8E8E93"));
            gridDiasSemana.Add(lblDiaSemana, i, 0);
        }
        mainStack.Add(gridDiasSemana);
        
        var calendarGrid = ConstruirGridCalendario();
        mainStack.Add(calendarGrid);
        
        var legendaStack = new HorizontalStackLayout 
        { 
            Spacing = 16, 
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 24, 0, 0)
        };
        
        legendaStack.Add(CriarItemLegenda("#34C759", AppResources.Hours_LegendNormal));
        legendaStack.Add(CriarItemLegenda("#FF9500", AppResources.Hours_LegendExtra));
        legendaStack.Add(CriarItemLegenda("#8E8E93", AppResources.Hours_Weekend));
        
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
            var horas = await NAVIGEST.macOS.Services.DatabaseService.GetHorasColaboradorAsync(idColaboradorFiltro, inicio, fim);
            
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
                new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto), 
                new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto) 
            },
            RowSpacing = 8,
            ColumnSpacing = 8,
            HorizontalOptions = LayoutOptions.Fill
        };

        var primeiroDiaMes = new DateTime(_dataCalendario.Year, _dataCalendario.Month, 1);
        int diasNoMes = DateTime.DaysInMonth(_dataCalendario.Year, _dataCalendario.Month);
        
        // Monday = 1, Sunday = 7. Adjust so Monday is 0 index.
        int diaSemanaPrimeiro = ((int)primeiroDiaMes.DayOfWeek == 0) ? 6 : (int)primeiroDiaMes.DayOfWeek - 1;

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
                    
                    
                    // Cell Container
                    var cellGrid = new Grid
                    {
                        Padding = 0,
                        BackgroundColor = Colors.Transparent
                    };

                    var ausencias = horasDoDia.Where(h => h.IdCentroCusto.HasValue).ToList();
                    bool temAusencias = ausencias.Any();

                    // Visual Border
                    var diaBorder = new Border
                    {
                        StrokeThickness = 0, // Default
                        Stroke = Colors.Transparent, // Default
                        StrokeShape = new RoundRectangle { CornerRadius = 12 },
                        Padding = 8,
                        HeightRequest = 120, // DOUBLE SIZE requested
                        MinimumWidthRequest = 110,
                        InputTransparent = false // IMPORTANT: Border must capture input
                    };

                    // Background Color & Border Logic with AppTheme Support
                    if (data.Date == DateTime.Today)
                    {
                        diaBorder.StrokeThickness = 2;
                        diaBorder.Stroke = Color.FromArgb("#0A84FF");
                    }

                    if (temHoras)
                    {
                        diaBorder.BackgroundColor = Color.FromArgb("#0A84FF").WithAlpha(0.1f);
                    }
                    else if (temAusencias)
                    {
                        // Absences but no hours: Light Orange/Yellow background
                        diaBorder.SetAppThemeColor(Border.BackgroundColorProperty, 
                            Color.FromArgb("#FF9F0A").WithAlpha(0.15f), // Light Mode
                            Color.FromArgb("#6c6151ff").WithAlpha(0.25f)); // Dark Mode
                    }
                    else if (isWeekend)
                    {
                        // Darker grey for Light Mode to be visible
                        diaBorder.SetAppThemeColor(Border.BackgroundColorProperty, Color.FromArgb("#E5E5EA"), Color.FromArgb("#1C1C1E"));
                    }
                    else
                    {
                        // Empty Day: Transparent background but THIN BORDER
                        diaBorder.BackgroundColor = Colors.Transparent;
                        
                        if (data.Date != DateTime.Today) // Don't override Today's border
                        {
                            diaBorder.StrokeThickness = 1;
                            diaBorder.SetAppThemeColor(Border.StrokeProperty, 
                                Color.FromArgb("#E5E5EA"), // Light Mode: SystemGrey5
                                Color.FromArgb("#38383A")); // Dark Mode: SystemGrey5
                        }
                    }
                    
                    var diaStack = new VerticalStackLayout 
                    { 
                        Spacing = 2, 
                        VerticalOptions = LayoutOptions.Start,
                        InputTransparent = true // Let touches pass
                    };
                    
                    // Day Label
                    var lblDia = new Label 
                    { 
                        Text = diaAtual.ToString(), 
                        FontSize = 24, 
                        FontAttributes = FontAttributes.Bold,
                        HorizontalTextAlignment = TextAlignment.Start,
                        InputTransparent = true
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
                    
                    // Content Stack (Centered)
                    var contentStack = new VerticalStackLayout
                    {
                        Spacing = 2,
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalOptions = LayoutOptions.Center,
                        InputTransparent = true
                    };

                    // Normal Hours
                    if (totalNormais > 0)
                    {
                        var lblNormais = new Label 
                        { 
                            Text = $"{totalNormais:0.##}h", 
                            FontSize = 18, 
                            TextColor = Color.FromArgb("#34C759"),
                            HorizontalTextAlignment = TextAlignment.Center,
                            FontAttributes = FontAttributes.Bold,
                            InputTransparent = true
                        };
                        contentStack.Add(lblNormais);
                    }

                    // Extra Hours
                    if (totalExtras > 0)
                    {
                        var lblExtras = new Label 
                        { 
                            Text = $"{totalExtras:0.##}h", 
                            FontSize = 18, 
                            TextColor = Color.FromArgb("#FF9500"),
                            HorizontalTextAlignment = TextAlignment.Center,
                            FontAttributes = FontAttributes.Bold,
                            InputTransparent = true
                        };
                        contentStack.Add(lblExtras);
                    }

                    // Absences / Icons
                    // var ausencias = ... (Already calculated above)
                    if (ausencias.Any())
                    {
                        var primeiraAusencia = ausencias.First();
                        var icon = primeiraAusencia.Icon; // Use the Icon from DB
                        
                        if (!string.IsNullOrEmpty(icon))
                        {
                            var lblAusencia = new Label 
                            { 
                                Text = icon, 
                                FontFamily = "FA7Solid", // Use FontAwesome
                                FontSize = 24, // Large icon
                                HorizontalTextAlignment = TextAlignment.Center,
                                InputTransparent = true,
                                TextColor = Color.FromArgb("#FF2D55") // Red color for absences
                            };
                            contentStack.Add(lblAusencia);
                        }
                    }
                    
                    diaStack.Add(contentStack);
                    diaBorder.Content = diaStack;

                    cellGrid.Add(diaBorder);

                    // CLICK FIX: Transparent Button Overlay
                    // This is the most robust way to handle clicks in MacCatalyst grids
                    var btnOverlay = new Button
                    {
                        BackgroundColor = Colors.Transparent, 
                        BorderColor = Colors.Transparent,
                        BorderWidth = 0,
                        CornerRadius = 0,
                        ZIndex = 100,
                        Margin = 0,
                        Padding = 0,
                        InputTransparent = false, // Explicitly capture input
                        HorizontalOptions = LayoutOptions.Fill,
                        VerticalOptions = LayoutOptions.Fill
                    };
                    
                    btnOverlay.Clicked += async (s, e) => 
                    {
                        System.Diagnostics.Debug.WriteLine($"[Calendar] Button Clicked on day {data:dd/MM}");
                        try
                        {
                            await diaBorder.FadeTo(0.5, 50);
                            await diaBorder.FadeTo(1.0, 50);
                        }
                        catch { }
                        await OnDiaCalendarioTapped(data, horasDoDia);
                    };
                    
                    cellGrid.Add(btnOverlay);

                    grid.Add(cellGrid, col, semana);
                    
                    diaAtual++;
                }
            }
        }

        return grid;
    }

    private async Task OnDiaCalendarioTapped(DateTime data, List<HoraColaborador> horasExistentes)
    {
        _overlayDate = data;
        
        // Update Header
        OverlayDateLabel.Text = data.ToString("dd 'de' MMMM", new System.Globalization.CultureInfo("pt-PT"));
        
        bool isWeekend = data.DayOfWeek == DayOfWeek.Saturday || data.DayOfWeek == DayOfWeek.Sunday;
        OverlayWeekendLabel.IsVisible = isWeekend;
        
        // Calculate Totals (Match iOS Logic)
        double totalNormal = horasExistentes?.Sum(h => h.HorasTrab) ?? 0;
        double totalExtra = horasExistentes?.Sum(h => h.HorasExtras) ?? 0;
        double total = totalNormal + totalExtra;
        
        OverlayTotalHoursLabel.Text = $"{AppResources.Hours_Total}: {total:0.00}h ({totalNormal:0.00}h + {totalExtra:0.00}h {AppResources.Hours_LegendExtra.ToLower()})";

        // Update List
        OverlayEntriesCollectionView.ItemsSource = horasExistentes;

        // Update Collaborator Name Visibility
        ShowCollaboratorName = _vm.ColaboradorSelecionado == null || _vm.ColaboradorSelecionado.ID == 0;

        // Show Overlay
        DailySummaryOverlay.IsVisible = true;
        
        // Animate
        DailySummaryOverlay.Opacity = 0;
        await DailySummaryOverlay.FadeTo(1, 250);
    }

    private async void OnOverlayAddClicked(object sender, EventArgs e)
    {
        DailySummaryOverlay.IsVisible = false;
        
        var novaHora = new HoraColaborador
        {
            DataTrabalho = _overlayDate,
            HorasTrab = 0,
            HorasExtras = 0,
            IdColaborador = _vm.ColaboradorSelecionado?.ID ?? 0
        };
        await AbrirNovaHoraPopupAsync(novaHora);
    }

    private async void OnOverlayEditClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is HoraColaborador hora)
        {
            DailySummaryOverlay.IsVisible = false;
            await AbrirNovaHoraPopupAsync(hora);
        }
    }

    private async void OnOverlayDeleteClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is HoraColaborador hora)
        {
            await _vm.EliminarHoraCommand.ExecuteAsync(hora);

            if (!_vm.HorasList.Contains(hora))
            {
                var updatedList = _vm.HorasList.Where(h => h.DataTrabalho.Date == _overlayDate.Date).ToList();
                
                if (updatedList.Any())
                {
                    OverlayEntriesCollectionView.ItemsSource = updatedList;
                    
                    double totalNormal = updatedList.Sum(h => h.HorasTrab);
                    double totalExtra = updatedList.Sum(h => h.HorasExtras);
                    double total = totalNormal + totalExtra;
                    OverlayTotalHoursLabel.Text = $"Total: {total:0.00}h ({totalNormal:0.00}h + {totalExtra:0.00}h extra)";
                }
                else
                {
                    DailySummaryOverlay.IsVisible = false;
                }
                
                if (_vm.TabAtiva == 3) 
                {
                    MainThread.BeginInvokeOnMainThread(() => CarregarTab3Calendario());
                }
            }
        }
    }

    private List<Button> _monthButtons = new();

    private async Task AtualizarFiltroListaAsync()
    {
        var inicio = new DateTime(_selectedYear, _selectedMonth, 1);
        var fim = inicio.AddMonths(1).AddDays(-1);
        
        _vm.DefinirPeriodo(inicio, fim);
        
        AtualizarEstiloBotoesMes();
    }

    private void AtualizarEstiloBotoesMes()
    {
        for (int i = 0; i < _monthButtons.Count; i++)
        {
            var btn = _monthButtons[i];
            bool isSelected = (i + 1) == _selectedMonth;
            
            btn.BackgroundColor = isSelected ? Color.FromArgb("#0A84FF") : Colors.Transparent;
            
            if (isSelected)
            {
                btn.TextColor = Colors.White;
            }
            else
            {
                btn.SetAppThemeColor(Button.TextColorProperty, Colors.Black, Colors.White);
            }
        }
    }

    private View CriarSeletorData()
    {
        var container = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Auto), new(GridLength.Star) },
            ColumnSpacing = 12,
            HeightRequest = 44
        };

        // Year Selector
        var yearGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto) },
            ColumnSpacing = 4,
            VerticalOptions = LayoutOptions.Center
        };

        var lblYear = new Label { Text = _selectedYear.ToString(), FontAttributes = FontAttributes.Bold, VerticalTextAlignment = TextAlignment.Center, FontSize = 16 };
        lblYear.SetAppThemeColor(Label.TextColorProperty, Colors.Black, Colors.White);

        var btnPrevYear = new Button { Text = "◀", BackgroundColor = Colors.Transparent, TextColor = Color.FromArgb("#0A84FF"), Padding = 0, WidthRequest = 30 };
        btnPrevYear.Clicked += async (s, e) => { _selectedYear--; lblYear.Text = _selectedYear.ToString(); await AtualizarFiltroListaAsync(); };

        var btnNextYear = new Button { Text = "▶", BackgroundColor = Colors.Transparent, TextColor = Color.FromArgb("#0A84FF"), Padding = 0, WidthRequest = 30 };
        btnNextYear.Clicked += async (s, e) => { _selectedYear++; lblYear.Text = _selectedYear.ToString(); await AtualizarFiltroListaAsync(); };

        yearGrid.Add(btnPrevYear, 0, 0);
        yearGrid.Add(lblYear, 1, 0);
        yearGrid.Add(btnNextYear, 2, 0);

        container.Add(yearGrid, 0, 0);

        // Month Selector
        var scroll = new ScrollView { Orientation = ScrollOrientation.Horizontal, HorizontalScrollBarVisibility = ScrollBarVisibility.Never };
        var stack = new HorizontalStackLayout { Spacing = 8 };
        
        _monthButtons.Clear();
        string[] months = { "JAN", "FEV", "MAR", "ABR", "MAI", "JUN", "JUL", "AGO", "SET", "OUT", "NOV", "DEZ" };
        
        for (int i = 0; i < 12; i++)
        {
            int monthIndex = i + 1;
            var btn = new Button
            {
                Text = months[i],
                FontSize = 12,
                CornerRadius = 8,
                Padding = new Thickness(12, 0),
                HeightRequest = 36,
                MinimumWidthRequest = 60
            };
            
            btn.Clicked += async (s, e) => 
            {
                _selectedMonth = monthIndex;
                await AtualizarFiltroListaAsync();
            };
            
            _monthButtons.Add(btn);
            stack.Add(btn);
        }
        
        AtualizarEstiloBotoesMes();
        
        scroll.Content = stack;
        container.Add(scroll, 1, 0);

        return container;
    }
}
