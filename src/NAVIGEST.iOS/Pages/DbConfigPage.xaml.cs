using NAVIGEST.iOS.Models;
using NAVIGEST.iOS.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using MySqlConnector;
using System.Net.Sockets;
using System.Diagnostics;

namespace NAVIGEST.iOS.Pages;

public partial class DbConfigPage : ContentPage
{
    private readonly AppSettingsService _settingsService = new();
    private DbSettings _settings = new();

    public DbConfigPage()
    {
        InitializeComponent();
        _settings = _settingsService.Load();
        BindingContext = _settings;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // micro animação discreta
        if (Card != null)
        {
            Card.Opacity = 0;
            await Card.FadeTo(1, 220, Easing.CubicOut);
        }
    }

    // ---------------- helpers de recursos/cores ----------------

    private static Color AsColor(object? value, Color fallback)
    {
        if (value is Color c) return c;
        if (value is SolidColorBrush b) return b.Color;
        return fallback;
    }

    private static Brush BrushFrom(Color c) => new SolidColorBrush(c);

    private static Color GetAppColor(string key, Color fallback)
    {
        // procura só no dicionário global da App (DesignSystem.xaml)
        var appRes = Application.Current?.Resources;
        if (appRes != null && appRes.TryGetValue(key, out var obj))
            return AsColor(obj, fallback);

        return fallback;
    }

    // Usa as keys definidas no Resources/Styles/DesignSystem.xaml
    // Color.Primary, Field.Border, Color.Warning, Color.Danger
    private Brush GetPrimaryBrush() =>
        BrushFrom(GetAppColor("Color.Primary", Colors.Blue));

    private Brush GetStrokeNeutralBrush() =>
        BrushFrom(GetAppColor("Field.Border", Colors.LightGray));

    private Brush GetWarnBrush() =>
        BrushFrom(GetAppColor("Color.Warning", Colors.Orange));

    private Brush GetErrorBrush() =>
        BrushFrom(GetAppColor("Color.Danger", Colors.Red));

    // ---------------- eventos de foco/edição ----------------

    private void OnEntryFocused(object? sender, FocusEventArgs e)
    {
        if (sender is not Entry entry) return;
        var b = FindParentBorder(entry);
        if (b is null) return;

        b.Stroke = GetPrimaryBrush();
        b.StrokeThickness = 1;
    }

    private void OnEntryUnfocused(object? sender, FocusEventArgs e)
    {
        if (sender is not Entry entry) return;
        ValidateField(entry); // repõe a cor adequada
    }

    private void OnEntryTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not Entry entry) return;
        ValidateField(entry);
    }

    private void OnEntryCompleted(object? sender, EventArgs e)
    {
        if (sender is not Entry entry) return;
        ValidateField(entry);
    }

    private Border? FindParentBorder(VisualElement element)
    {
        Element? p = element;
        while (p is not null)
        {
            if (p is Border b) return b;
            p = p.Parent;
        }
        return null;
    }

    // ---------------- validações ----------------

    private bool ValidateField(Entry entry)
    {
        if (entry == ServerEntry) return ValidateRequired(entry, ServerBorder, ServerError);
        if (entry == PortEntry) return ValidatePort();
        if (entry == DbEntry) return ValidateRequired(entry, DbBorder, DbError);
        if (entry == UserEntry) return ValidateRequired(entry, UserBorder, UserError);
        if (entry == PasswordEntry) return ValidateRequired(entry, PasswordBorder, PasswordError);
        if (entry == EmailEntry) return ValidateEmail();
        return true;
    }

    private bool ValidateRequired(Entry entry, Border border, Label errorLabel)
    {
        var ok = !string.IsNullOrWhiteSpace(entry.Text);
        errorLabel.IsVisible = !ok;

        border.Stroke = ok ? GetStrokeNeutralBrush() : GetWarnBrush();
        border.StrokeThickness = 1;
        return ok;
    }

    private bool ValidatePort()
    {
        var text = PortEntry?.Text?.Trim();
        var ok = int.TryParse(text, out var p) && p is >= 1 and <= 65535;

        if (PortError != null) PortError.IsVisible = !ok;
        if (PortBorder != null)
        {
            PortBorder.Stroke = ok ? GetStrokeNeutralBrush() : GetWarnBrush();
            PortBorder.StrokeThickness = 1;
        }
        return ok;
    }

    private bool ValidateEmail()
    {
        var txt = EmailEntry?.Text?.Trim();
        var ok = string.IsNullOrEmpty(txt) || IsValidEmail(txt!);

        if (EmailErrorLabel != null) EmailErrorLabel.IsVisible = !ok;
        if (EmailBorder != null)
        {
            EmailBorder.Stroke = ok ? GetStrokeNeutralBrush() : GetErrorBrush();
            EmailBorder.StrokeThickness = 1;
        }
        return ok;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch { return false; }
    }

    // ---------------- Guardar e Testar ----------------

    private async void OnSaveAndTestClicked(object sender, EventArgs e)
    {
        var allOk =
            ValidateRequired(ServerEntry, ServerBorder, ServerError) &
            ValidatePort() &
            ValidateRequired(DbEntry, DbBorder, DbError) &
            ValidateRequired(UserEntry, UserBorder, UserError) &
            ValidateRequired(PasswordEntry, PasswordBorder, PasswordError) &
            ValidateEmail();

        if (!allOk)
        {
            await AppShell.DisplayToastAsync("Corrija os campos destacados.");
            return;
        }

        _settings.Server = ServerEntry.Text?.Trim() ?? "";
        _settings.Port = (uint)int.Parse(PortEntry.Text!.Trim());
        _settings.Database = DbEntry.Text?.Trim() ?? "";
        _settings.UserId = UserEntry.Text?.Trim() ?? "";
        _settings.Password = PasswordEntry.Text ?? "";

        _settingsService.Save(_settings);

        bool ok = await DatabaseService.TestConnectionAsync();
        if (ok)
        {
            await AppShell.DisplayToastAsync("Ligação estabelecida com sucesso!");
            await Shell.Current.GoToAsync("//WelcomePage");
        }
        else
        {
            await AppShell.DisplayToastAsync("Ainda não foi possível ligar à BD.");
        }
    }

    // ---------------- Diagnóstico Tailscale / Multi-endpoint ----------------

    // Ajuste estes endpoints (coloque apenas os válidos depois de confirmar)
    private static readonly (string host, int port, string etiqueta)[] CandidateEndpoints =
    {
        ("100.81.152.95", 3306, "Tailscale:3306"), // IP anterior
        ("100.81.152.95", 3307, "Tailscale:3307"),
        ("100.81.125.95", 3306, "Tailscale (variação):3306"),
        ("100.81.125.95", 3307, "Tailscale (variação):3307"),
        ("192.168.1.200",    3306, "LAN"),
        ("192.168.1.200",    3307, "LAN:3307")
    };

    private async Task<string> TcpProbeAsync(string host, int port, int timeoutMs)
    {
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(host, port);
            var delayTask = Task.Delay(timeoutMs);
            var finished = await Task.WhenAny(connectTask, delayTask);

            if (finished == delayTask)
                return "TCP timeout";

            if (connectTask.IsFaulted)
            {
                if (connectTask.Exception?.InnerException is SocketException sex)
                    return $"TCP {sex.SocketErrorCode}";
                return "TCP erro: " + connectTask.Exception?.InnerException?.Message;
            }

            return client.Connected ? "TCP OK" : "TCP falhou";
        }
        catch (SocketException ex)
        {
            return $"TCP {ex.SocketErrorCode}";
        }
        catch (Exception ex)
        {
            return "TCP erro: " + ex.Message;
        }
    }

    private async Task<(bool ok, string log)> TryEndpointsAsync()
    {
        var sb = new System.Text.StringBuilder();
        bool anySuccess = false;

        foreach (var (host, port, tag) in CandidateEndpoints)
        {
            sb.AppendLine($"[{tag}] {host}:{port}");
            var tcp = await TcpProbeAsync(host, port, 4500);
            sb.AppendLine("  - Probe: " + tcp);

            if (!tcp.StartsWith("TCP OK"))
            {
                sb.AppendLine("  - Ignorado (sem ligação TCP bem sucedida).");
                continue;
            }

            try
            {
                var csb = new MySqlConnectionStringBuilder
                {
                    Server = host,
                    Port = (uint)port,
                    Database = _settings.Database,
                    UserID = _settings.UserId,
                    Password = _settings.Password ?? "",
                    SslMode = MySqlSslMode.Preferred,
                    AllowPublicKeyRetrieval = true,
                    ConnectionTimeout = 12,
                    DefaultCommandTimeout = 60
                };
                using var conn = new MySqlConnection(csb.ConnectionString);
                var sw = Stopwatch.StartNew();
                await conn.OpenAsync();
                sw.Stop();
                sb.AppendLine($"  - MySQL OK ({sw.ElapsedMilliseconds} ms) Versão: {conn.ServerVersion}");
                anySuccess = true;
                break;
            }
            catch (Exception ex)
            {
                sb.AppendLine("  - MySQL erro: " + ex.Message);
            }
        }

        if (!anySuccess)
            sb.AppendLine("Nenhum endpoint respondeu com sucesso.");
        return (anySuccess, sb.ToString());
    }

    private async void BtnTestarDb_Clicked(object sender, EventArgs e)
    {
        BtnTestarDb.IsEnabled = false;
        LblResultado.Text = "A testar ligação (multi-endpoints)...";
        try
        {
            // Primeiro tenta o settings atual
            var primaryOk = await DatabaseService.TestConnectionAsync();
            if (primaryOk)
            {
                await DisplayAlert("DB", "Ligação OK com configuração atual (TestConnection).", "Fechar");
                LblResultado.Text = "Primário OK.";
                return;
            }

            // Faz fallback multi-endpoint
            var (ok, log) = await TryEndpointsAsync();
            LblResultado.Text = log;
            await DisplayAlert("Diagnóstico", log, "Fechar");
        }
        catch (Exception ex)
        {
            await DisplayAlert("DB", $"Falhou: {ex.GetType().Name}\n{ex.Message}", "Fechar");
            LblResultado.Text = "Erro: " + ex.Message;
        }
        finally
        {
            BtnTestarDb.IsEnabled = true;
        }
    }
}

#if WINDOWS
// Código Windows específico (exemplo: animações, navegação, layouts)
#endif
#if ANDROID
// Código Android específico (exemplo: animações, navegação, layouts)
#endif
#if IOS
// Código iOS específico (exemplo: animações, navegação, layouts)
#endif
