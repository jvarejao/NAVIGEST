using NAVIGEST.Android.Models;
using NAVIGEST.Android.Services;
using System.Collections.ObjectModel;

namespace NAVIGEST.Android.Pages;

public partial class AbsenceTypesPage : ContentPage
{
    public ObservableCollection<AbsenceType> Types { get; set; } = new();

    public AbsenceTypesPage()
    {
        InitializeComponent();
        TypesCollectionView.ItemsSource = Types;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        Types.Clear();
        var list = await DatabaseService.GetAbsenceTypesAsync();
        foreach (var item in list)
        {
            Types.Add(item);
        }
    }

    private async void OnAddClicked(object sender, EventArgs e)
    {
        string result = await DisplayPromptAsync("Novo Tipo", "Descrição do tipo de ausência (ex: Férias, Baixa):");
        if (!string.IsNullOrWhiteSpace(result))
        {
            var newType = new AbsenceType { Descricao = result.Trim() };
            await DatabaseService.SaveAbsenceTypeAsync(newType);
            await LoadDataAsync();
        }
    }

    private async void OnItemTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is AbsenceType type)
        {
            string result = await DisplayPromptAsync("Editar Tipo", "Descrição:", initialValue: type.Descricao);
            if (result != null) // Cancel returns null
            {
                if (string.IsNullOrWhiteSpace(result))
                {
                    await DisplayAlert("Erro", "A descrição não pode estar vazia.", "OK");
                    return;
                }

                type.Descricao = result.Trim();
                await DatabaseService.SaveAbsenceTypeAsync(type);
                await LoadDataAsync();
            }
        }
    }

    private async void OnDeleteInvoked(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipeItem && swipeItem.CommandParameter is AbsenceType type)
        {
            bool confirm = await DisplayAlert("Eliminar", $"Tem a certeza que deseja eliminar '{type.Descricao}'?", "Sim", "Não");
            if (confirm)
            {
                await DatabaseService.DeleteAbsenceTypeAsync(type.Id);
                await LoadDataAsync();
            }
        }
    }
}
