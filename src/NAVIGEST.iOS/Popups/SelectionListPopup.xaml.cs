using CommunityToolkit.Maui.Views;

namespace NAVIGEST.iOS.Popups;

public partial class SelectionListPopup : Popup
{
    private List<string> _allItems;

    public SelectionListPopup(string title, IEnumerable<string> items)
    {
        InitializeComponent();
        TitleLabel.Text = title;
        _allItems = items.ToList();
        ItemsCollectionView.ItemsSource = _allItems;
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.NewTextValue))
        {
            ItemsCollectionView.ItemsSource = _allItems;
        }
        else
        {
            ItemsCollectionView.ItemsSource = _allItems
                .Where(i => i.Contains(e.NewTextValue, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is string selectedItem)
        {
            Close(selectedItem);
        }
    }

    private void OnCancelClicked(object sender, EventArgs e)
    {
        Close(null);
    }
}