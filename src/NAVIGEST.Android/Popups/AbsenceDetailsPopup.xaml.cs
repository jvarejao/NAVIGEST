using System.Collections.Generic;
using CommunityToolkit.Maui.Views;
using NAVIGEST.Android.Models;

namespace NAVIGEST.Android.Popups
{
    public partial class AbsenceDetailsPopup : Popup
    {
        public AbsenceDetailsPopup(AbsenceSummary summary, List<string> details)
        {
            InitializeComponent();
            TitleLabel.Text = $"{summary.Tipo} ({summary.Dias} dias)";
            TitleLabel.TextColor = Microsoft.Maui.Graphics.Color.FromArgb(summary.Cor);
            
            DetailsList.ItemsSource = details;
        }

        private void OnCloseClicked(object sender, System.EventArgs e)
        {
            Close();
        }
    }
}
