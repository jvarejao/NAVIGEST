using NAVIGEST.Android.Models;
using NAVIGEST.Android.Services;
using System.Collections.ObjectModel;
using NAVIGEST.Shared.Resources.Strings;

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
        string result = await DisplayPromptAsync(AppResources.Absence_NewTypeTitle, AppResources.Absence_NewTypeMessage);
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
            string result = await DisplayPromptAsync(AppResources.Absence_EditTypeTitle, AppResources.Absence_DescriptionLabel, initialValue: type.Descricao);
            if (result != null) // Cancel returns null
            {
                if (string.IsNullOrWhiteSpace(result))
                {
                    await DisplayAlert(AppResources.Common_Error, AppResources.Absence_ErrorEmptyDescription, AppResources.Common_OK);
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
            bool confirm = await DisplayAlert(AppResources.Common_Delete, string.Format(AppResources.Absence_ConfirmDelete, type.Descricao), AppResources.Common_Yes, AppResources.Common_No);
            if (confirm)
            {
                await DatabaseService.DeleteAbsenceTypeAsync(type.Id);
                await LoadDataAsync();
            }
        }
    }
}
