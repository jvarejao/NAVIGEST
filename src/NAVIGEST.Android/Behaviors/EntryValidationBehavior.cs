#nullable enable
using System.Text.RegularExpressions;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
#if ANDROID
using Microsoft.Maui.Platform;
using Android.Content.Res;
using AndroidX.AppCompat.Widget;
#endif
#if WINDOWS
using Microsoft.Maui.Platform;
using WinBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using WinThickness = Microsoft.UI.Xaml.Thickness;
#endif

namespace NAVIGEST.Android.Behaviors;

public enum ValidationKind
{
    Auto,
    None,
    Email,
    Phone,
    Numeric,
    Password
}

/// <summary>
/// Behavior para Entries: altera cor do underline conforme foco e validao.
/// Android: usa BackgroundTintList.
/// Windows: simula underline via BorderBrush + BorderThickness (0,0,0,2) no TextBox nativo.
/// Outras plataformas: ajusta apenas TextColor (no h underline nativo).
/// Estados: Foco -> Azul (#2563EB), Vlido -> Verde (#22C55E), Invlido -> Vermelho (#EF4444), Neutro -> Placeholder cinza.
/// </summary>
public class EntryValidationBehavior : Behavior<Entry>
{
    private Entry? _entry;

    // Paleta (alinhada com GlobalToast)
    private static readonly Color FocusColor       = Color.FromArgb("#2563EB");
    private static readonly Color ValidColor       = Color.FromArgb("#22C55E");
    private static readonly Color InvalidColor     = Color.FromArgb("#EF4444");
    private static readonly Color NeutralTextLight = Color.FromArgb("#111827");
    private static readonly Color NeutralTextDark  = Colors.White;
    private static readonly Color PlaceholderLight = Color.FromArgb("#9CA3AF");
    private static readonly Color PlaceholderDark  = Color.FromArgb("#6B7280");
    private static readonly Color UnderlineNeutral = Color.FromArgb("#D1D5DB"); // fallback neutro

    public static readonly BindableProperty ValidationKindProperty = BindableProperty.Create(
        nameof(ValidationKind), typeof(ValidationKind), typeof(EntryValidationBehavior), ValidationKind.Auto);

    public ValidationKind ValidationKind
    {
        get => (ValidationKind)GetValue(ValidationKindProperty);
        set => SetValue(ValidationKindProperty, value);
    }

    protected override void OnAttachedTo(Entry bindable)
    {
        base.OnAttachedTo(bindable);
        _entry = bindable;
        bindable.Focused += OnFocused;
        bindable.Unfocused += OnUnfocused;
        bindable.TextChanged += OnTextChanged;
        bindable.HandlerChanged += OnHandlerChanged;
        ApplyState();
    }

    protected override void OnDetachingFrom(Entry bindable)
    {
        base.OnDetachingFrom(bindable);
        bindable.Focused -= OnFocused;
        bindable.Unfocused -= OnUnfocused;
        bindable.TextChanged -= OnTextChanged;
        bindable.HandlerChanged -= OnHandlerChanged;
        _entry = null;
    }

    private void OnHandlerChanged(object? sender, System.EventArgs e) => ApplyState();

    private void OnFocused(object? sender, FocusEventArgs e)
    {
        if (_entry == null) return;
        SetVisual(FocusColor, GetPlaceholderColor());
    }

    private void OnUnfocused(object? sender, FocusEventArgs e) => ApplyState();

    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_entry?.IsFocused == true) return; // evita flicker durante digitao
        ApplyState();
    }

    private void ApplyState()
    {
        if (_entry == null) return;
        var text = _entry.Text ?? string.Empty;
        bool empty = string.IsNullOrWhiteSpace(text);

        if (empty)
        {
            SetVisual(UnderlineNeutral, GetPlaceholderColor());
            return;
        }

        var vk = ResolveKind();
        if (vk == ValidationKind.None)
        {
            SetVisual(UnderlineNeutral, GetPlaceholderColor());
            return;
        }

        bool ok = Validate(vk, text);
        SetVisual(ok ? ValidColor : InvalidColor, GetPlaceholderColor());
    }

    private ValidationKind ResolveKind()
    {
        if (_entry == null) return ValidationKind.None;
        if (ValidationKind != ValidationKind.Auto) return ValidationKind;
        if (_entry.IsPassword) return ValidationKind.Password;
        if (_entry.Keyboard == Keyboard.Email) return ValidationKind.Email;
        if (_entry.Keyboard == Keyboard.Telephone) return ValidationKind.Phone;
        if (_entry.Keyboard == Keyboard.Numeric) return ValidationKind.Numeric;
        return ValidationKind.None;
    }

    private static bool Validate(ValidationKind kind, string text)
    {
        try
        {
            return kind switch
            {
                ValidationKind.Email => Regex.IsMatch(text, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase),
                ValidationKind.Phone => Regex.IsMatch(text, @"^[+]?([0-9][\s-]?){6,}[0-9]$"),
                ValidationKind.Numeric => double.TryParse(text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out _),
                ValidationKind.Password => text.Length >= 8,
                _ => true
            };        
        }
        catch { return false; }
    }

    private void SetVisual(Color underlineColor, Color placeholder)
    {
        if (_entry == null) return;

        // Texto s fica vermelho em erro; caso contrrio neutro
        if (underlineColor == InvalidColor)
            _entry.TextColor = underlineColor;
        else
            _entry.TextColor = GetNeutralTextColor();

        _entry.PlaceholderColor = placeholder.WithAlpha(0.8f);
        UpdatePlatformUnderline(underlineColor);
    }

    private void UpdatePlatformUnderline(Color color)
    {
#if ANDROID
        try
        {
            if (_entry?.Handler?.PlatformView is AndroidX.AppCompat.Widget.AppCompatEditText native)
            {
                var aColor = color.ToPlatform();
                native.BackgroundTintList = global::Android.Content.Res.ColorStateList.ValueOf(aColor);
                if (OperatingSystem.IsAndroidVersionAtLeast(29))
                    native.TextCursorDrawable?.SetTint(aColor);
            }
        }
        catch { }
#endif
#if WINDOWS
        try
        {
            if (_entry?.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.TextBox native)
            {
                // Simula underline ajustando a borda inferior
                native.BorderThickness = new WinThickness(0, 0, 0, 2);
                var winColor = color.ToWindowsColor();
                native.BorderBrush = new WinBrush(winColor);
            }
        }
        catch { }
#endif
    }

    private static Color GetPlaceholderColor()
        => Microsoft.Maui.Controls.Application.Current?.RequestedTheme == AppTheme.Dark ? PlaceholderDark : PlaceholderLight;

    private static Color GetNeutralTextColor()
        => Microsoft.Maui.Controls.Application.Current?.RequestedTheme == AppTheme.Dark ? NeutralTextDark : NeutralTextLight;
}
