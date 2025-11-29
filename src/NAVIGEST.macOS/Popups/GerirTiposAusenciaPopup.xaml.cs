using NAVIGEST.macOS.Models;
using NAVIGEST.macOS.Services;
using System.Collections.ObjectModel;

namespace NAVIGEST.macOS.Popups;

public partial class GerirTiposAusenciaPopup : ContentPage
{
    public ObservableCollection<AbsenceType> Types { get; set; } = new();
    public ObservableCollection<string> Icons { get; set; } = new()
    {
        "\uf073", "\uf5ca", "\uf236", "\uf79f", "\uf0f0", "\uf19d", "\uf0b1", "\uf015", 
        "\uf1b9", "\uf206", "\uf554", "\uf21e", "\uf54e", "\uf0c0", "\uf007", "\uf129", 
        "\uf06a", "\uf059"
    };

    private AbsenceType? _editingType;

    public GerirTiposAusenciaPopup()
    {
        InitializeComponent();
        cvTypes.ItemsSource = Types;
        cvIcons.ItemsSource = Icons;
        
        // Set default selection
        var defaultIcon = Icons.FirstOrDefault();
        if (defaultIcon != null)
        {
            cvIcons.SelectedItem = defaultIcon;
            lblSelectedIcon.Text = defaultIcon;
        }

        // Safe execution of async initialization
        Dispatcher.Dispatch(async () => await LoadDataAsync());
    }

    private void OnIconSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is string icon)
        {
            lblSelectedIcon.Text = icon;
        }
    }

    private void OnIconTapped(object sender, EventArgs e)
    {
        if (sender is Element element && element.BindingContext is string icon)
        {
            cvIcons.SelectedItem = icon;
        }
    }

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            // Ensure table exists before fetching
            await DatabaseService.CreateAbsenceTypesTableAsync();

            Types.Clear();
            var list = await DatabaseService.GetAbsenceTypesAsync();
            foreach (var item in list)
            {
                Types.Add(item);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao carregar tipos de ausência: {ex.Message}");
            if (Shell.Current != null)
                await Shell.Current.DisplayAlert("Erro", "Não foi possível carregar os tipos de ausência.", "OK");
        }
    }

    private async void OnAddClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtNewType.Text))
            return;

        try
        {
            var icon = cvIcons.SelectedItem as string ?? "\uf073";
            
            if (_editingType != null)
            {
                // Update existing
                _editingType.Description = txtNewType.Text.Trim();
                _editingType.Icon = icon;
                var success = await DatabaseService.UpdateAbsenceTypeAsync(_editingType);
                if (success)
                {
                    _editingType = null;
                    txtNewType.Text = string.Empty;
                    btnAddOrUpdate.Text = "Adicionar";
                    
                    // Reset Selection
                    var defaultIcon = Icons.FirstOrDefault();
                    if (defaultIcon != null)
                    {
                        cvIcons.SelectedItem = defaultIcon;
                        lblSelectedIcon.Text = defaultIcon;
                    }
                    
                    await LoadDataAsync();
                }
                else
                {
                    if (Shell.Current != null)
                        await Shell.Current.DisplayAlert("Erro", "Não foi possível atualizar o tipo de ausência.", "OK");
                }
            }
            else
            {
                // Add new
                var success = await DatabaseService.AddAbsenceTypeAsync(txtNewType.Text.Trim(), icon);
                if (success)
                {
                    txtNewType.Text = string.Empty;
                    await LoadDataAsync();
                }
                else
                {
                    if (Shell.Current != null)
                        await Shell.Current.DisplayAlert("Erro", "Não foi possível adicionar o tipo de ausência.", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao adicionar/atualizar: {ex.Message}");
        }
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is AbsenceType type)
        {
            try
            {
                if (Shell.Current == null) return;
                
                bool confirm = await Shell.Current.DisplayAlert("Confirmar", $"Apagar '{type.Description}'?", "Sim", "Não");
                if (confirm)
                {
                    var success = await DatabaseService.DeleteAbsenceTypeAsync(type.Id);
                    if (success)
                    {
                        await LoadDataAsync();
                    }
                    else
                    {
                        await Shell.Current.DisplayAlert("Erro", "Não foi possível apagar o registo.", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao apagar: {ex.Message}");
            }
        }
    }

    private void OnEditClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is AbsenceType type)
        {
            _editingType = type;
            txtNewType.Text = type.Description;
            btnAddOrUpdate.Text = "Atualizar";
            
            // Select the icon
            if (!string.IsNullOrEmpty(type.Icon) && Icons.Contains(type.Icon))
            {
                cvIcons.SelectedItem = type.Icon;
                lblSelectedIcon.Text = type.Icon;
                cvIcons.ScrollTo(type.Icon, position: ScrollToPosition.Center, animate: true);
            }
            
            txtNewType.Focus();
        }
    }
}