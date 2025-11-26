using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
using NAVIGEST.macOS.Models;

namespace NAVIGEST.macOS.Popups;

public partial class DailySummaryPopup : Popup
{
    public bool ShowCollaboratorName { get; set; }

    private DateTime _date;

    public DailySummaryPopup(DateTime date, List<HoraColaborador> entries, bool showCollaboratorName)
    {
        Console.WriteLine($"[DailySummaryPopup] Constructor called for date: {date}");
        try
        {
            InitializeComponent();
            
            _date = date;
            ShowCollaboratorName = showCollaboratorName;
            
            /* 
            // TEMPORARILY COMMENTED OUT FOR DEBUGGING - "NUCLEAR OPTION"
            
            // Setup Date Label
            if (DateLabel != null)
            {
                DateLabel.Text = date.ToString("dd 'de' MMMM", new System.Globalization.CultureInfo("pt-PT"));
            }
            else
            {
                Console.WriteLine("[DailySummaryPopup] DateLabel is NULL!");
            }

            // Weekend Indicator
            if (WeekendLabel != null && (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday))
            {
                WeekendLabel.IsVisible = true;
            }

            // Setup List
            if (EntriesCollectionView != null)
            {
                EntriesCollectionView.ItemsSource = entries;
            }

            // Calculate Totals
            float totalNormal = entries.Sum(e => e.HorasTrab);
            float totalExtra = entries.Sum(e => e.HorasExtras);
            float total = totalNormal + totalExtra;

            if (TotalHoursLabel != null)
            {
                TotalHoursLabel.Text = $"Total: {total:0.00}h ({totalNormal:0.00}h + {totalExtra:0.00}h extra)";
            }
            */
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DailySummaryPopup] CRITICAL ERROR IN CONSTRUCTOR: {ex}");
            throw; // RETHROW TO ALLOW CALLER TO HANDLE
        }
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
