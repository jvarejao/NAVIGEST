using CommunityToolkit.Maui.Views;

namespace NAVIGEST.macOS.Popups;

public partial class SimpleTestPopup : Popup
{
	public SimpleTestPopup()
	{
		InitializeComponent();
	}

    private void OnCloseClicked(object sender, EventArgs e)
    {
        Close();
    }
}