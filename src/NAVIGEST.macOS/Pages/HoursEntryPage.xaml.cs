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

namespace NAVIGEST.macOS.Pages;

public partial class HoursEntryPage : ContentPage
{
    private HorasColaboradorViewModel _vm => (HorasColaboradorViewModel)BindingContext;
    
    private DateTime _dataCalendario = DateTime.Today;
    private DateTime _overlayDate;

    // Edit Overlay State
    private HoraColaborador _currentEditingHora;
    private bool _isEditMode;
    private List<Cliente> _clientes = new();
    private List<AbsenceType> _absenceTypes = new();
    private Cliente? _selectedClient;
    private Colaborador? _selectedCollaborator;
    private AbsenceType? _selectedAbsenceType;
    private bool _isAbsenceMode;

    // List Picker State
    private TaskCompletionSource<object?>? _pickerTcs;
    private bool _isPickerActive;

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
            Padding = 16,
            Margin = new Thickness(0, 0, 0, 12),
            StrokeShape = new RoundRectangle { CornerRadius = 12 }
        };
        
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
        infoStack.Add(lblData);
        
        var lblNome = new Label { FontSize = 14, TextColor = Color.FromArgb("#8E8E93") };
        lblNome.SetBinding(Label.TextProperty, new Binding("NomeColaborador"));
        infoStack.Add(lblNome);
        
        var lblCliente = new Label { FontSize = 13, TextColor = Color.FromArgb("#8E8E93") };
        lblCliente.SetBinding(Label.TextProperty, new Binding("Cliente", stringFormat: "🏢 {0}"));
        infoStack.Add(lblCliente);
        
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
        
        var btnEdit = new Button 
        { 
            Text = "\uf044", // fa-pencil-alt
            FontFamily = "FA7Solid",
            BackgroundColor = Color.FromArgb("#0A84FF").WithAlpha(0.1f), 
            TextColor = Color.FromArgb("#0A84FF"),
            CornerRadius = 8,
            WidthRequest = 44,
            HeightRequest = 44,
            FontSize = 18
        };
        btnEdit.Clicked += async (s, e) => 
        {
             if ((s as Button)?.BindingContext is HoraColaborador hora)
                await AbrirNovaHoraPopupAsync(hora);
        };
        actionsStack.Add(btnEdit);

        var btnDelete = new Button 
        { 
            Text = "\uf2ed", // fa-trash-alt
            FontFamily = "FA7Solid",
            BackgroundColor = Color.FromArgb("#FF3B30").WithAlpha(0.1f), 
            TextColor = Color.FromArgb("#FF3B30"),
            CornerRadius = 8,
            WidthRequest = 44,
            HeightRequest = 44,
            FontSize = 18
        };
        btnDelete.Clicked += async (s, e) => 
        {
             if ((s as Button)?.BindingContext is HoraColaborador hora)
                await _vm.EliminarHoraCommand.ExecuteAsync(hora);
        };
        actionsStack.Add(btnDelete);

        itemGrid.Add(actionsStack, 2, 0);
        Grid.SetRowSpan(actionsStack, 2);
        
        itemBorder.Content = itemGrid;
        
        return itemBorder;
    }
       
    private async Task MostrarMenuPeriodoAsync()
    {
        var result = await ShowListPickerAsync("Selecionar Período", new List<string> { "Hoje", "Esta Semana", "Este Mês", "Últimos 30 dias" });
        
        if (result is string action)
        {
            if (action == "Hoje") _vm.SelecionarHojeCommand.Execute(null);
            else if (action == "Esta Semana") _vm.SelecionarEstaSemanaCommand.Execute(null);
            else if (action == "Este Mês") _vm.SelecionarEsteMesCommand.Execute(null);
            else if (action == "Últimos 30 dias") _vm.SelecionarUltimos30DiasCommand.Execute(null);
        }
    }

    private async Task AbrirNovaHoraPopupAsync(HoraColaborador? hora)
    {
        try
        {
            _currentEditingHora = hora ?? new HoraColaborador
            {
                DataTrabalho = DateTime.Today,
                HorasTrab = 0,
                HorasExtras = 0,
                IdColaborador = _vm.ColaboradorSelecionado?.ID ?? 0
            };
            _isEditMode = hora != null;

            // Load data if needed
            if (_vm.Colaboradores.Count == 0)
            {
                Console.WriteLine("DEBUG: Colaboradores list is empty. Loading...");
                await _vm.CarregarColaboradoresAsync();
                Console.WriteLine($"DEBUG: Loaded {_vm.Colaboradores.Count} colaboradores.");
            }
            else
            {
                Console.WriteLine($"DEBUG: Colaboradores list has {_vm.Colaboradores.Count} items.");
            }

            if (_clientes.Count == 0)
            {
                Console.WriteLine("DEBUG: Clientes list is empty. Loading...");
                var clientesDb = await DatabaseService.GetClientesAsync(null);
                if (clientesDb != null && clientesDb.Any())
                {
                    _clientes = clientesDb.OrderBy(c => c.CLINOME).ToList();
                    _clientes.Insert(0, new Cliente { CLICODIGO = "0", CLINOME = "Sem cliente" });
                }
                else
                {
                     _clientes = new List<Cliente> { new Cliente { CLICODIGO = "0", CLINOME = "Sem cliente" } };
                }
                Console.WriteLine($"DEBUG: Loaded {_clientes.Count} clientes.");
            }

            if (_absenceTypes.Count == 0)
            {
                Console.WriteLine("DEBUG: AbsenceTypes list is empty. Loading...");
                _absenceTypes = await DatabaseService.GetAbsenceTypesAsync();
                Console.WriteLine($"DEBUG: Loaded {_absenceTypes.Count} absence types.");
            }

            // Setup UI
            EditOverlayTitleLabel.Text = _isEditMode ? "✏️ EDITAR REGISTO" : "➕ NOVO REGISTO";
            EditOverlayDeleteButton.IsVisible = _isEditMode;
            
            EditOverlayDatePicker.Date = _currentEditingHora.DataTrabalho;
            EditOverlayHoursEntry.Text = _currentEditingHora.HorasTrab > 0 ? _currentEditingHora.HorasTrab.ToString("0.00") : "";
            EditOverlayExtrasEntry.Text = _currentEditingHora.HorasExtras > 0 ? _currentEditingHora.HorasExtras.ToString("0.00") : "";
            EditOverlayObservationsEntry.Text = _currentEditingHora.Observacoes ?? "";

            // Setup Collaborator
            if (_currentEditingHora.IdColaborador > 0)
            {
                _selectedCollaborator = _vm.Colaboradores.FirstOrDefault(c => c.ID == _currentEditingHora.IdColaborador);
            }
            else
            {
                _selectedCollaborator = _vm.ColaboradorSelecionado;
            }
            EditOverlayCollaboratorLabel.Text = _selectedCollaborator?.Nome ?? "Selecione colaborador";

            // Setup Type/Client/Absence
            if (_isEditMode && _currentEditingHora.IdCentroCusto.HasValue)
            {
                // Absence
                _isAbsenceMode = true;
                _selectedAbsenceType = _absenceTypes.FirstOrDefault(a => a.Id == _currentEditingHora.IdCentroCusto.Value);
                EditOverlayTypeLabel.Text = "Ausência";
                EditOverlayAbsenceLabel.Text = _selectedAbsenceType?.Description ?? "Selecione o motivo";
                
                // Reset Client
                _selectedClient = _clientes.FirstOrDefault();
                EditOverlayClientLabel.Text = "Sem cliente";
            }
            else
            {
                // Work
                _isAbsenceMode = false;
                EditOverlayTypeLabel.Text = "Trabalho";
                
                if (!string.IsNullOrEmpty(_currentEditingHora.IdCliente))
                {
                    var idClienteTrim = _currentEditingHora.IdCliente.Trim();
                    _selectedClient = _clientes.FirstOrDefault(c => c.CLICODIGO?.Trim() == idClienteTrim) ?? _clientes.FirstOrDefault();
                }
                else
                {
                    _selectedClient = _clientes.FirstOrDefault();
                }
                EditOverlayClientLabel.Text = _selectedClient?.CLINOME ?? "Sem cliente";
            }

            UpdateEditOverlayUI();
            EditHourOverlay.IsVisible = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao abrir popup nova hora: {ex.Message}");
            await DisplayAlert("Erro", $"Erro ao abrir popup: {ex.Message}", "OK");
        }
    }

    private void UpdateEditOverlayUI()
    {
        EditOverlayClientContainer.IsVisible = !_isAbsenceMode;
        EditOverlayHoursContainer.IsVisible = !_isAbsenceMode;
        EditOverlayExtrasContainer.IsVisible = !_isAbsenceMode;
        EditOverlayAbsenceContainer.IsVisible = _isAbsenceMode;
    }

    private void OnCloseEditOverlayClicked(object sender, EventArgs e)
    {
        EditHourOverlay.IsVisible = false;
    }

    private async void OnEditOverlaySaveClicked(object sender, EventArgs e)
    {
        Console.WriteLine("DEBUG: OnEditOverlaySaveClicked called");
        try
        {
            if (_selectedCollaborator == null)
            {
                await DisplayAlert("Erro", "Selecione um colaborador.", "OK");
                return;
            }

            if (_isAbsenceMode)
            {
                if (_selectedAbsenceType == null)
                {
                    await DisplayAlert("Erro", "Selecione o motivo da ausência.", "OK");
                    return;
                }
                
                _currentEditingHora.IdCentroCusto = _selectedAbsenceType.Id;
                _currentEditingHora.IdCliente = "";
                _currentEditingHora.HorasTrab = 0;
                _currentEditingHora.HorasExtras = 0;
            }
            else
            {
                if (!double.TryParse(EditOverlayHoursEntry.Text, out double horas)) horas = 0;
                if (!double.TryParse(EditOverlayExtrasEntry.Text, out double extras)) extras = 0;

                if (horas == 0 && extras == 0)
                {
                    await DisplayAlert("Erro", "Insira horas normais ou extras.", "OK");
                    return;
                }

                _currentEditingHora.IdCentroCusto = null;
                _currentEditingHora.IdCliente = _selectedClient?.CLICODIGO ?? "";
                _currentEditingHora.Cliente = _selectedClient?.CLINOME ?? "";
                _currentEditingHora.HorasTrab = (float)horas;
                _currentEditingHora.HorasExtras = (float)extras;
            }

            _currentEditingHora.DataTrabalho = EditOverlayDatePicker.Date;
            _currentEditingHora.IdColaborador = _selectedCollaborator.ID;
            _currentEditingHora.NomeColaborador = _selectedCollaborator.Nome;
            _currentEditingHora.Observacoes = EditOverlayObservationsEntry.Text;

            // Save
            await DatabaseService.UpsertHoraColaboradorAsync(_currentEditingHora);
            
            EditHourOverlay.IsVisible = false;
            await AppShell.DisplayToastAsync("Registo guardado com sucesso!");
            
            // Refresh
            await _vm.CarregarHorasCommand.ExecuteAsync(null);
            
            // Force Calendar Refresh
            if (_vm.TabAtiva == 3) 
            {
                MainThread.BeginInvokeOnMainThread(() => CarregarTab3Calendario());
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", $"Erro ao guardar: {ex.Message}", "OK");
        }
    }

    private async void OnEditOverlayDeleteClicked(object sender, EventArgs e)
    {
        Console.WriteLine("DEBUG: OnEditOverlayDeleteClicked called");
        bool confirm = await DisplayAlert("Confirmar", "Tem a certeza que deseja eliminar este registo?", "Sim", "Não");
        if (confirm)
        {
            await _vm.EliminarHoraCommand.ExecuteAsync(_currentEditingHora);
            EditHourOverlay.IsVisible = false;
            await AppShell.DisplayToastAsync("Registo eliminado com sucesso!");
            
            // Refresh
            await _vm.CarregarHorasCommand.ExecuteAsync(null);
            
            // Force Calendar Refresh
            if (_vm.TabAtiva == 3) 
            {
                MainThread.BeginInvokeOnMainThread(() => CarregarTab3Calendario());
            }
        }
    }

    // --- Pickers Logic ---

    private async void OnEditOverlayTypeTapped(object sender, EventArgs e)
    {
        var result = await ShowListPickerAsync("Tipo de Registo", new List<string> { "Trabalho", "Ausência" });
        if (result is string type)
        {
            _isAbsenceMode = type == "Ausência";
            EditOverlayTypeLabel.Text = type;
            UpdateEditOverlayUI();
        }
    }

    private async void OnEditOverlayCollaboratorTapped(object sender, EventArgs e)
    {
        var names = _vm.Colaboradores.Select(c => c.Nome ?? "").ToList();
        Console.WriteLine($"DEBUG: Opening Collaborator Picker with {names.Count} items.");
        var result = await ShowListPickerAsync("Selecione Colaborador", names);
        if (result is string name)
        {
            var colab = _vm.Colaboradores.FirstOrDefault(c => c.Nome == name);
            if (colab != null)
            {
                _selectedCollaborator = colab;
                EditOverlayCollaboratorLabel.Text = colab.Nome;
            }
        }
    }

    private async void OnEditOverlayClientTapped(object sender, EventArgs e)
    {
        var result = await ShowListPickerAsync("Selecione Cliente", _clientes.Select(c => c.CLINOME ?? "").ToList());
        if (result is string name)
        {
            var client = _clientes.FirstOrDefault(c => c.CLINOME == name);
            if (client != null)
            {
                _selectedClient = client;
                EditOverlayClientLabel.Text = client.CLINOME;
            }
        }
    }

    private async void OnEditOverlayAbsenceTapped(object sender, EventArgs e)
    {
        var result = await ShowListPickerAsync("Selecione Motivo", _absenceTypes.Select(a => a.Description ?? "").ToList());
        if (result is string desc)
        {
            var absence = _absenceTypes.FirstOrDefault(a => a.Description == desc);
            if (absence != null)
            {
                _selectedAbsenceType = absence;
                EditOverlayAbsenceLabel.Text = absence.Description;
            }
        }
    }

    // --- Generic List Picker ---

    private Task<object?> ShowListPickerAsync(string title, List<string> items)
    {
        _pickerTcs = new TaskCompletionSource<object?>();
        
        ListPickerTitleLabel.Text = title;
        ListPickerCollectionView.ItemsSource = items;
        ListPickerOverlay.IsVisible = true;
        
        return _pickerTcs.Task;
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
        var mainStack = new VerticalStackLayout { Spacing = 24, Padding = 24 };
        
        var lblTitulo = new Label 
        { 
            Text = "📅 Calendário do Mês", 
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
            HeightRequest = 44
        };
        
        pickerContainer.SetAppThemeColor(Border.BackgroundColorProperty, Colors.White, Color.FromArgb("#111827"));
        pickerContainer.SetAppThemeColor(Border.StrokeProperty, Color.FromArgb("#E5E5EA"), Color.FromArgb("#38383A"));

        var pickerGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Auto) }
        };

        var lblPicker = new Label
        {
            Text = _vm.ColaboradorSelecionado?.Nome ?? "Selecione Colaborador",
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
            var result = await ShowListPickerAsync("Selecione Colaborador", _vm.Colaboradores.Select(c => c.Nome).ToList());
            
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
            Margin = new Thickness(0,0,0,8)
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
        
        legendaStack.Add(CriarItemLegenda("#34C759", "Normal"));
        legendaStack.Add(CriarItemLegenda("#FF9500", "Extra"));
        legendaStack.Add(CriarItemLegenda("#8E8E93", "Fim de Semana"));
        
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
            ColumnSpacing = 8
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
                        var icon = GetAbsenceIcon(primeiraAusencia.DescCentroCusto, primeiraAusencia.Cliente);
                        
                        if (!string.IsNullOrEmpty(icon))
                        {
                            var lblAusencia = new Label 
                            { 
                                Text = icon, 
                                FontSize = 32, // Large icon
                                HorizontalTextAlignment = TextAlignment.Center,
                                InputTransparent = true
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
        
        OverlayTotalHoursLabel.Text = $"Total: {total:0.00}h ({totalNormal:0.00}h + {totalExtra:0.00}h extra)";

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

    private string GetAbsenceIcon(string? description, string? clientName)
    {
        if (string.IsNullOrEmpty(description)) return string.Empty;
        var desc = description.ToLower();
        
        if (desc.Contains("férias") || desc.Contains("ferias")) return "🏖️";
        if (desc.Contains("doença") || desc.Contains("doenca") || desc.Contains("médico") || desc.Contains("medico") || desc.Contains("hospital")) return "🏥";
        if (desc.Contains("pai") || desc.Contains("mãe") || desc.Contains("parental") || desc.Contains("filho")) return "👶";
        if (desc.Contains("luto") || desc.Contains("falecimento") || desc.Contains("funeral")) return "⚫";
        if (desc.Contains("formação") || desc.Contains("formacao") || desc.Contains("curso")) return "🎓";
        if (desc.Contains("outros")) return "⚠️";

        return string.Empty;
    }
}
