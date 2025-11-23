using System;
using System.IO;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Storage;

namespace NAVIGEST.Android.Popups;

public partial class LogViewerPopup : Popup
{
    private readonly string _logPath;

    public LogViewerPopup()
    {
        InitializeComponent();
        _logPath = Path.Combine(FileSystem.AppDataDirectory, "logs", "app.log");
        LoadLogs();
    }

    private void LoadLogs()
    {
        try
        {
            if (File.Exists(_logPath))
            {
                var text = File.ReadAllText(_logPath);
                // Show last 10000 chars if too long
                if (text.Length > 10000)
                {
                    text = "..." + text.Substring(text.Length - 10000);
                }
                LogLabel.Text = text;
            }
            else
            {
                LogLabel.Text = "No logs found.";
            }
        }
        catch (Exception ex)
        {
            LogLabel.Text = $"Error reading logs: {ex.Message}";
        }
    }

    private void OnRefreshClicked(object sender, EventArgs e)
    {
        LoadLogs();
    }

    private void OnClearClicked(object sender, EventArgs e)
    {
        try
        {
            if (File.Exists(_logPath))
            {
                File.Delete(_logPath);
                LogLabel.Text = "Logs cleared.";
            }
        }
        catch (Exception ex)
        {
            LogLabel.Text = $"Error clearing logs: {ex.Message}";
        }
    }

    private void OnCloseClicked(object sender, EventArgs e)
    {
        Close();
    }
}
