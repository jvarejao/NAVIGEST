using Microsoft.Maui.Controls;
using System;

namespace NAVIGEST.macOS.Pages
{
    public partial class DailySummaryPage : ContentPage
    {
        public DailySummaryPage()
        {
            InitializeComponent();
        }

        private async void OnCloseClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }
}