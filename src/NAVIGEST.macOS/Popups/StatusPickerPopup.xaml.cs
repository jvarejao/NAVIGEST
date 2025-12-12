using CommunityToolkit.Maui.Views;

namespace NAVIGEST.macOS.Popups;

public partial class StatusPickerPopup : Popup
{
    public StatusPickerPopup(List<string> options)
    {
        InitializeComponent();
        StatusList.ItemsSource = options;
    }

    private void OnCloseClicked(object sender, EventArgs e)
    {
        Close(null);
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is string selectedStatus)
        {
            Close(selectedStatus);
        }
    }
}