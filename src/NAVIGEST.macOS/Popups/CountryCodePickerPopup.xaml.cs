using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Controls;
using NAVIGEST.macOS.Models;

namespace NAVIGEST.macOS.Popups;

public partial class CountryCodePickerPopup : ContentPage
{
    private readonly Action<DialCodeItem> _onSelected;
    private readonly List<DialCodeItem> _allItems;

    public CountryCodePickerPopup(IEnumerable<DialCodeItem> items, Action<DialCodeItem> onSelected)
    {
        InitializeComponent();
        _allItems = items.ToList();
        _onSelected = onSelected;
        CountriesList.ItemsSource = _allItems;
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
#if MACCATALYST
        if (SearchEntry.Handler?.PlatformView is UIKit.UITextField textField)
        {
            textField.BorderStyle = UIKit.UITextBorderStyle.None;
        }
#endif
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.NewTextValue))
        {
            CountriesList.ItemsSource = _allItems;
        }
        else
        {
            var term = e.NewTextValue.ToLowerInvariant();
            CountriesList.ItemsSource = _allItems
                .Where(x => x.SearchText.Contains(term))
                .ToList();
        }
    }

    private async void OnCountrySelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is DialCodeItem selected)
        {
            _onSelected?.Invoke(selected);
            await Navigation.PopModalAsync();
        }
        
        // Clear selection so it can be selected again if reopened (though page is recreated usually)
        CountriesList.SelectedItem = null;
    }

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}
