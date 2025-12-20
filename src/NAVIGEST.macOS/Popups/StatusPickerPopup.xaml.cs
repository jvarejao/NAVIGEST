using CommunityToolkit.Maui.Views;
using NAVIGEST.macOS.Models;

namespace NAVIGEST.macOS.Popups;

public partial class StatusPickerPopup : Popup
{
    private List<ServiceStatus> _allOptions;

    public StatusPickerPopup(IEnumerable<ServiceStatus> options)
    {
        InitializeComponent();
        _allOptions = options?.ToList() ?? new List<ServiceStatus>();
        StatusList.ItemsSource = _allOptions;
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = e.NewTextValue;
        if (ClearButton != null)
            ClearButton.IsVisible = !string.IsNullOrEmpty(searchText);

        StatusList.ItemsSource = string.IsNullOrWhiteSpace(searchText)
            ? _allOptions
            : _allOptions
                .Where(s => s.Descricao?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true)
                .ToList();
    }

    private void OnCancelClicked(object sender, EventArgs e)
    {
        Close(null);
    }

    private void OnClearSearchClicked(object sender, EventArgs e)
    {
        SearchEntry.Text = string.Empty;
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is ServiceStatus selectedStatus)
        {
            Close(selectedStatus.Descricao);
        }
    }
}