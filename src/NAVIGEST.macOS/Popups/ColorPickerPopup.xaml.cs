using CommunityToolkit.Maui.Views;
using NAVIGEST.macOS.Models;
using NAVIGEST.macOS.Services;

namespace NAVIGEST.macOS.Popups;

public partial class ColorPickerPopup : Popup
{
    private List<Cor> _allColors = new();

    public ColorPickerPopup()
    {
        InitializeComponent();
        LoadColors();
    }

    private async void LoadColors()
    {
        LoadingIndicator.IsRunning = true;
        try
        {
            _allColors = await DatabaseService.GetCoresAsync();
            ColorsList.ItemsSource = _allColors;
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
            ColorsList.ItemsSource = _allColors;
        }
        else
        {
            var lower = e.NewTextValue.ToLower();
            ColorsList.ItemsSource = _allColors.Where(c => 
                (c.NomeCor?.ToLower().Contains(lower) ?? false)).ToList();
        }
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Cor selected)
        {
            Close(selected);
        }
    }
}