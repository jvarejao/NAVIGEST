using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
using NAVIGEST.Android.Models;

namespace NAVIGEST.Android.Popups;

public partial class DailySummaryPopup : Popup
{
    public bool ShowCollaboratorName { get; set; }

    private DateTime _date;

    public DailySummaryPopup(DateTime date, List<HoraColaborador> entries, bool showCollaboratorName)
    {
        InitializeComponent();
        
        _date = date;
        ShowCollaboratorName = showCollaboratorName;
        
        // Setup Date Label
        DateLabel.Text = date.ToString("dd 'de' MMMM", new System.Globalization.CultureInfo("pt-PT"));

        // Weekend Indicator
        if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
        {
            WeekendLabel.IsVisible = true;
        }

        // Setup List
        EntriesCollectionView.ItemsSource = entries;

        // Calculate Totals
        float totalNormal = entries.Sum(e => e.HorasTrab);
        float totalExtra = entries.Sum(e => e.HorasExtras);
        float total = totalNormal + totalExtra;

        TotalHoursLabel.Text = $"Total: {total:0.00}h ({totalNormal:0.00}h + {totalExtra:0.00}h extra)";
    }

    private void OnCloseClicked(object sender, EventArgs e)
    {
        Close(null);
    }

    private void OnAddClicked(object sender, EventArgs e)
    {
        Close(_date);
    }

    private void OnEditClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is HoraColaborador hora)
        {
            Close(hora);
        }
    }
}
