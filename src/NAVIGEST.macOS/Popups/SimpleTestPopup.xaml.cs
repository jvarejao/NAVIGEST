using CommunityToolkit.Maui.Views;

namespace NAVIGEST.macOS.Popups;

public partial class SimpleTestPopup : Popup
{
	private readonly TaskCompletionSource<string?> _tcs = new();

	public SimpleTestPopup(string titulo, string labelCampo, string? valorInicial = null)
	{
		InitializeComponent();
		TitleLabel.Text = titulo;
		FieldLabel.Text = labelCampo;
		InputEntry.Text = valorInicial ?? string.Empty;

		Opened += (_, _) => InputEntry.Focus();
	}

	public Task<string?> WaitForResultAsync() => _tcs.Task;

	private void OnOkClicked(object sender, EventArgs e)
	{
		_tcs.TrySetResult(InputEntry.Text?.Trim());
		Close(InputEntry.Text?.Trim());
	}

	private void OnCancelClicked(object sender, EventArgs e)
	{
		_tcs.TrySetResult(null);
		Close(null);
	}
}