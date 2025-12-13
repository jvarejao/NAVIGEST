using CommunityToolkit.Maui.Views;
using NAVIGEST.macOS.Models;
using NAVIGEST.macOS.Services;

namespace NAVIGEST.macOS.Popups;

public partial class ClientPickerPopup : Popup
{
    private List<Cliente> _allClients = new();

    public ClientPickerPopup()
    {
        InitializeComponent();
        LoadClients();
    }

    private async void LoadClients()
    {
        LoadingIndicator.IsRunning = true;
        try
        {
            _allClients = await DatabaseService.GetClientesAsync();
            ClientsList.ItemsSource = _allClients;
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

    private void OnCancelClicked(object sender, EventArgs e)
    {
        Close(null);
    }

    private void OnClearSearchClicked(object sender, EventArgs e)
    {
        SearchEntry.Text = string.Empty;
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = e.NewTextValue;
        if (ClearButton != null)
            ClearButton.IsVisible = !string.IsNullOrEmpty(searchText);

        if (string.IsNullOrWhiteSpace(searchText))
        {
            ClientsList.ItemsSource = _allClients;
        }
        else
        {
            var lower = searchText.ToLower();
            ClientsList.ItemsSource = _allClients.Where(c => 
                (c.CLINOME?.ToLower().Contains(lower) ?? false) || 
                (c.CLICODIGO?.ToLower().Contains(lower) ?? false)
            ).ToList();
        }
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Cliente selected)
        {
            Close(selected);
        }
    }
}