using CommunityToolkit.Maui.Views;
using NAVIGEST.macOS.Models;
using NAVIGEST.macOS.Services;

namespace NAVIGEST.macOS.Popups;

public partial class SizePickerPopup : Popup
{
    private List<Tamanho> _allSizes = new();

    public SizePickerPopup()
    {
        InitializeComponent();
        LoadSizes();
    }

    private async void LoadSizes()
    {
        LoadingIndicator.IsRunning = true;
        try
        {
            _allSizes = await DatabaseService.GetTamanhosAsync();
            SizesList.ItemsSource = _allSizes;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.NewTextValue))
        {
            SizesList.ItemsSource = _allSizes;
        }
        else
        {
            var lower = e.NewTextValue.ToLower();
            SizesList.ItemsSource = _allSizes.Where(c => 
                (c.NomeTamanho?.ToLower().Contains(lower) ?? false)).ToList();
        }
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Tamanho selected)
        {
            Close(selected);
        }
    }
}