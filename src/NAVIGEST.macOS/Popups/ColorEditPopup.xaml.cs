using CommunityToolkit.Maui.Views;
using NAVIGEST.macOS.Models;
using Microsoft.Maui.Graphics;
using Color = Microsoft.Maui.Graphics.Color;

namespace NAVIGEST.macOS.Popups;

public partial class ColorEditPopup : Popup
{
    private bool _suppressSliderUpdate;
    private bool _suppressCodeUpdate;
    private bool _suppressNameUpdate;

    public ColorEditPopup(Cor? existing = null, string? generatedId = null)
    {
        InitializeComponent();
        if (existing != null)
        {
            IdEntry.Text = existing.IdCor;
            NameEntry.Text = existing.NomeCor;
            CodeEntry.Text = existing.CodigoHex;
            RefEntry.Text = existing.Referencia;
            if (TryParseHex(existing.CodigoHex, out var color))
            {
                ApplyColor(color, updateRef: false);
            }
        }
        else
        {
            IdEntry.Text = generatedId ?? string.Empty;
            ApplyColor(Colors.White, updateRef: false);
        }

        // ID deve ser apenas leitura
        IdEntry.IsEnabled = false;
    }

    private void OnCancel(object sender, EventArgs e)
    {
        Close(null);
    }

    private void OnNameChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressNameUpdate) return;

        var upper = e.NewTextValue?.ToUpperInvariant();
        if (upper == e.NewTextValue) return;

        _suppressNameUpdate = true;
        NameEntry.Text = upper;
        NameEntry.CursorPosition = Math.Min(NameEntry.CursorPosition, upper?.Length ?? 0);
        _suppressNameUpdate = false;
    }

    private void OnSave(object sender, EventArgs e)
    {
        var id = IdEntry.Text?.Trim();
        var nome = NameEntry.Text?.Trim()?.ToUpperInvariant();
        var codigo = CodeEntry.Text?.Trim();
        var referencia = RefEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(nome))
        {
            Close(null);
            return;
        }

        var result = new Cor
        {
            IdCor = id,
            NomeCor = nome,
            CodigoHex = NormalizeHex(codigo),
            Referencia = string.IsNullOrWhiteSpace(referencia) ? null : referencia
        };

        Close(result);
    }

    private void OnCodeChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressCodeUpdate) return;
        if (TryParseHex(e.NewTextValue, out var color))
        {
            ApplyColor(color, updateRef: false);
        }
    }

    private void OnRgbChanged(object sender, ValueChangedEventArgs e)
    {
        if (_suppressSliderUpdate) return;
        var color = Color.FromRgb(
            (int)Math.Round(SliderR.Value),
            (int)Math.Round(SliderG.Value),
            (int)Math.Round(SliderB.Value));
        ApplyColor(color);
    }

    private void ApplyColor(Color color, bool updateRef = true, bool updateSliders = true)
    {
        var hex = ToRgbHex(color);
        if (CodeEntry.Text != hex)
        {
            _suppressCodeUpdate = true;
            CodeEntry.Text = hex;
            _suppressCodeUpdate = false;
        }

        SelectedColorBox.Color = color;
        HexPreviewLabel.Text = hex;

        if (updateSliders)
        {
            _suppressSliderUpdate = true;
            SliderR.Value = Math.Round(color.Red * 255);
            SliderG.Value = Math.Round(color.Green * 255);
            SliderB.Value = Math.Round(color.Blue * 255);
            _suppressSliderUpdate = false;
        }

        if (updateRef && string.IsNullOrWhiteSpace(RefEntry.Text))
        {
            RefEntry.Text = hex;
        }
    }

    private static bool TryParseHex(string? value, out Color color)
    {
        color = Colors.Transparent;
        if (string.IsNullOrWhiteSpace(value)) return false;

        var hex = value.Trim();
        if (!hex.StartsWith("#"))
            hex = "#" + hex;

        // Accept #RRGGBB or #AARRGGBB; Color.FromArgb handles both
        try
        {
            color = Color.FromArgb(hex);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string ToRgbHex(Color color)
    {
        int r = (int)Math.Round(color.Red * 255);
        int g = (int)Math.Round(color.Green * 255);
        int b = (int)Math.Round(color.Blue * 255);
        return $"#{r:X2}{g:X2}{b:X2}";
    }

    private static string? NormalizeHex(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var hex = value.Trim();
        if (!hex.StartsWith("#"))
            hex = "#" + hex;

        // Normalize to #RRGGBB when #AARRGGBB is provided
        if (hex.Length == 9)
            hex = "#" + hex.Substring(3);

        return hex.ToUpperInvariant();
    }
}
