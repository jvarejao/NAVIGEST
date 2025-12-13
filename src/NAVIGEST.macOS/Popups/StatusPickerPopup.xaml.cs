using CommunityToolkit.Maui.Views;

namespace NAVIGEST.macOS.Popups;

public partial class StatusPickerPopup : Popup
{
    private List<string> _allOptions;

    public StatusPickerPopup(List<string> options)
    {
        InitializeComponent();
        _allOptions = options;
        StatusList.ItemsSource = _allOptions;
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = e.NewTextValue;
        if (ClearButton != null)
            ClearButton.IsVisible = !string.IsNullOrEmpty(searchText);

        if (string.IsNullOrWhiteSpace(searchText))
        {
            StatusList.ItemsSource = _allOptions;
        }
        else
        {
            StatusList.ItemsSource = _allOptions
                .Where(s => s.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                .ToList();
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

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is string selectedStatus)
        {
            Close(selectedStatus);
        }
    }
}