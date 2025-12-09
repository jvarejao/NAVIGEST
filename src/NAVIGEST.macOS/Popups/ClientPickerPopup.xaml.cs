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

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.NewTextValue))
        {
            ClientsList.ItemsSource = _allClients;
        }
        else
        {
            var lower = e.NewTextValue.ToLower();
            ClientsList.ItemsSource = _allClients.Where(c => 
                (c.CLINOME?.ToLower().Contains(lower) ?? false) || 
                (c.CLICODIGO?.ToLower().Contains(lower) ?? false)).ToList();
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