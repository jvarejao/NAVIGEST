using CommunityToolkit.Maui.Views;
using NAVIGEST.iOS.Models;
using NAVIGEST.iOS.Services;
using System.Collections.ObjectModel;

namespace NAVIGEST.iOS.Popups;

public partial class GerirTiposAusenciaPopup : Popup
{
    public ObservableCollection<AbsenceType> Tipos { get; set; } = new();

    public GerirTiposAusenciaPopup()
    {
        InitializeComponent();
        TypesList.ItemsSource = Tipos;
        LoadData();
    }

    private async void LoadData()
    {
        var data = await DatabaseService.GetAbsenceTypesAsync();
        Tipos.Clear();
        foreach (var item in data)
        {
            Tipos.Add(item);
        }
    }

    private void OnCloseClicked(object sender, EventArgs e) => Close();

    private async void OnAddClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NewTypeEntry.Text)) return;

        var success = await DatabaseService.AddAbsenceTypeAsync(NewTypeEntry.Text.Trim());
        if (success)
        {
            NewTypeEntry.Text = string.Empty;
            LoadData();
        }
        else
        {
            // Show error toast or alert
        }
    }

    private async void OnDeleteInvoked(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipeItem && swipeItem.CommandParameter is AbsenceType tipo)
        {
            if (Shell.Current != null)
            {
                bool confirm = await Shell.Current.DisplayAlert("Eliminar", $"Tem a certeza que deseja eliminar '{tipo.Descricao}'?", "Sim", "Não");
                if (confirm)
                {
                    var success = await DatabaseService.DeleteAbsenceTypeAsync(tipo.Id);
                    if (success) LoadData();
                }
            }
        }
    }

    private async void OnEditClicked(object sender, EventArgs e)
    {
        if (sender is ImageButton btn && btn.CommandParameter is AbsenceType tipo)
        {
            if (Shell.Current != null)
            {
                string result = await Shell.Current.DisplayPromptAsync("Editar", "Nova descrição:", initialValue: tipo.Descricao);
                if (!string.IsNullOrWhiteSpace(result) && result != tipo.Descricao)
                {
                    tipo.Descricao = result.Trim();
                    var success = await DatabaseService.UpdateAbsenceTypeAsync(tipo);
                    if (success) LoadData();
                }
            }
        }
    }
}