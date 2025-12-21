using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Maui.Views;
using NAVIGEST.macOS.Models;
using NAVIGEST.macOS.Popups;
using NAVIGEST.macOS.Services;

namespace NAVIGEST.macOS.PageModels;

public class ColorsPageModel : BindableObject
{
    private ObservableCollection<Cor> _colors = new();
    public ObservableCollection<Cor> Colors
    {
        get => _colors;
        set { _colors = value; OnPropertyChanged(); }
    }

    public ICommand AddColorCommand { get; }
    public ICommand EditColorCommand { get; }
    public ICommand DeleteColorCommand { get; }

    public ColorsPageModel()
    {
        AddColorCommand = new Command(async () => await OnAddAsync());
        EditColorCommand = new Command<Cor>(async c => await OnEditAsync(c));
        DeleteColorCommand = new Command<Cor>(async c => await OnDeleteAsync(c));

        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var list = await DatabaseService.GetCoresAsync();
        Colors = new ObservableCollection<Cor>(list);
    }

    private async Task OnAddAsync()
    {
        var newId = await DatabaseService.GetNextCorIdAsync();
        var popup = new ColorEditPopup(existing: null, generatedId: newId);
        var result = await Shell.Current.ShowPopupAsync(popup) as Cor;
        if (result == null) return;

        await DatabaseService.AddCorAsync(result);
        await LoadAsync();
    }

    private async Task OnEditAsync(Cor? existing)
    {
        if (existing == null) return;
        var popup = new ColorEditPopup(existing);
        var result = await Shell.Current.ShowPopupAsync(popup) as Cor;
        if (result == null) return;

        await DatabaseService.UpdateCorAsync(result);
        await LoadAsync();
    }

    private async Task OnDeleteAsync(Cor? existing)
    {
        if (existing == null) return;
        bool confirm = await Shell.Current.DisplayAlert("Eliminar", $"Eliminar cor '{existing.NomeCor}'?", "Sim", "Não");
        if (!confirm) return;

        await DatabaseService.DeleteCorAsync(existing.IdCor);
        await LoadAsync();
    }
}
