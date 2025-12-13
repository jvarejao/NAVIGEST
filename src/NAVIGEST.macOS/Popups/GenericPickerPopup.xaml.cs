using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;

namespace NAVIGEST.macOS.Popups;

public partial class GenericPickerPopup : Popup
{
    private List<string> _allItems;
    private TaskCompletionSource<string?> _tcs;

    public GenericPickerPopup(string title, List<string> items)
    {
        InitializeComponent();
        
        TitleLabel.Text = title;
        _allItems = items;
        ItemsCollectionView.ItemsSource = _allItems;
        
        _tcs = new TaskCompletionSource<string?>();
        
        // Focus search
        Opened += (s, e) => SearchEntry.Focus();
    }

    public Task<string?> WaitForResultAsync()
    {
        return _tcs.Task;
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = e.NewTextValue;
        ClearButton.IsVisible = !string.IsNullOrEmpty(searchText);

        if (string.IsNullOrWhiteSpace(searchText))
        {
            ItemsCollectionView.ItemsSource = _allItems;
        }
        else
        {
            ItemsCollectionView.ItemsSource = _allItems
                .Where(i => i.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }
    }

    private void OnClearSearchClicked(object sender, EventArgs e)
    {
        SearchEntry.Text = string.Empty;
    }

    private void OnItemTapped(object sender, EventArgs e)
    {
        if (sender is Element element && element.BindingContext is string selectedItem)
        {
            _tcs.TrySetResult(selectedItem);
            Close(selectedItem);
        }
    }

    private void OnCancelClicked(object sender, EventArgs e)
    {
        _tcs.TrySetResult(null);
        Close(null);
    }
}