using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel; // MainThread, DeviceInfo
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes; // RoundRectangle
using Microsoft.Maui.Graphics;

namespace NAVIGEST.iOS
{
    public enum ToastTipo { Info, Sucesso, Aviso, Erro }
    public enum ToastPos  { Top, Center, Bottom }
    public enum WarningHue { Orange, Yellow }

    /// <summary>
    /// Toast global responsivo (MAUI) com Font Awesome 7 (Solid) e contraste por tema.
    /// Ajuste: ícone e texto agora centrados (vertical stack) dentro do toast.
    /// </summary>
    public static class GlobalToast
    {
        private const double MaxWidthPhone = 360;
        private const double MaxWidthLarge = 480;
        private const double MinWidth      = 180;
        private const int    MaxQueue      = 4;

        private static readonly SemaphoreSlim _queueSem = new(1, 1);
        private static readonly Queue<(string msg, ToastTipo tipo, int ms, ToastPos pos)> _queue = new();
        private static bool _showing;

        public static WarningHue AvisoHue { get; set; } = WarningHue.Orange;

        public static Task ShowAsync(string mensagem, ToastTipo tipo, int ms = 2600)
            => ShowAsync(mensagem, tipo, ms, ToastPos.Bottom);

        public static async Task ShowAsync(string mensagem, ToastTipo tipo, int ms, ToastPos pos)
        {
            await _queueSem.WaitAsync();
            try
            {
                if (_queue.Count >= MaxQueue) _queue.Dequeue();
                _queue.Enqueue((mensagem, tipo, ms, pos));
                if (!_showing)
                {
                    _showing = true;
                    _ = ProcessQueueAsync();
                }
            }
            finally { _queueSem.Release(); }
        }

        private static async Task ProcessQueueAsync()
        {
            while (true)
            {
                (string msg, ToastTipo tipo, int ms, ToastPos pos) item;
                await _queueSem.WaitAsync();
                try
                {
                    if (_queue.Count == 0) { _showing = false; return; }
                    item = _queue.Dequeue();
                }
                finally { _queueSem.Release(); }

                try { await ShowOneAsync(item.msg, item.tipo, item.ms, item.pos); } catch { }
            }
        }

        private static async Task ShowOneAsync(string mensagem, ToastTipo tipo, int ms, ToastPos pos)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var cp = GetActiveContentPage();
                if (cp == null) return;

                var rootGrid = EnsureRootGrid(cp);
                if (rootGrid == null) return;

                var overlay = GetOrCreateOverlay(rootGrid);
                CleanupOldToasts(overlay);

                var theme = Application.Current?.RequestedTheme ?? AppTheme.Light;
                var (bg, borderColor, textColor, iconCode) = GetColorsAndIcon(tipo, theme);

                var iconLabel = new Label
                {
                    Text = iconCode,
                    FontFamily = "FA7Solid",
                    FontSize = 24, // ligeiro aumento para destaque centrado
                    TextColor = borderColor,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    Margin = new Thickness(0, 0, 0, 4)
                };

                var textLabel = new Label
                {
                    Text = mensagem,
                    TextColor = textColor,
                    FontSize = 15,
                    LineBreakMode = LineBreakMode.WordWrap,
                    HorizontalTextAlignment = TextAlignment.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                };

                // Conteúdo agora empilhado verticalmente e centrado
                var content = new VerticalStackLayout
                {
                    Spacing = 6,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    Children = { iconLabel, textLabel }
                };

                var toast = new Border
                {
                    Stroke = borderColor,
                    StrokeThickness = 2,
                    Background = new SolidColorBrush(bg),
                    StrokeShape = new RoundRectangle { CornerRadius = 18 },
                    Padding = new Thickness(18, 20),
                    Opacity = 0,
                    Content = content,
                    HorizontalOptions = LayoutOptions.Center,
                    InputTransparent = true,
                    AutomationId = "__toast_item",
                    Shadow = new Shadow { Radius = 12, Opacity = (theme == AppTheme.Dark ? 0.5f : 0.25f) }
                };

                double availableWidth = rootGrid.Width > 0 ? rootGrid.Width :
                                        cp.Width > 0 ? cp.Width :
                                        Application.Current?.Windows?.FirstOrDefault()?.Page?.Width ?? 400;

                toast.WidthRequest = GetResponsiveWidth(availableWidth);

                int row = pos switch
                {
                    ToastPos.Top => 0,
                    ToastPos.Center => 1,
                    _ => 2
                };
                Grid.SetRow(toast, row);

                var (safeTop, safeBottom) = GetSafeOffsets();
                toast.Margin = row switch
                {
                    0 => new Thickness(0, 24 + safeTop, 0, 0),
                    2 => new Thickness(0, 0, 0, 24 + safeBottom),
                    _ => new Thickness(0)
                };

                overlay.ZIndex = 9999;
                toast.ZIndex   = 10000;

                overlay.Children.Add(toast);

                const double slide = 30;
                toast.TranslationY = row == 0 ? -slide : (row == 2 ? slide : 0);
                await toast.FadeTo(1, 160, Easing.CubicOut);
                await toast.TranslateTo(toast.TranslationX, 0, 180, Easing.CubicOut);

                try { await Task.Delay(ms); } catch { }

                await toast.FadeTo(0, 160, Easing.CubicIn);
                overlay.Children.Remove(toast);
            });
        }

        // === Helpers ===
        private static double GetResponsiveWidth(double availableWidth)
        {
            bool isPhone = DeviceInfo.Idiom == DeviceIdiom.Phone;
            double max   = isPhone ? MaxWidthPhone : MaxWidthLarge;
            double target = Math.Min(max, availableWidth * 0.75);
            return Math.Max(MinWidth, target);
        }

        private static (double safeTop, double safeBottom) GetSafeOffsets()
            => DeviceInfo.Platform == DevicePlatform.iOS ? (12, 12) : (0, 0);

        private static ContentPage? GetActiveContentPage()
        {
            var main = Application.Current?.Windows?.FirstOrDefault()?.Page;
            if (main == null) return null;
            if (main is ContentPage cp) return cp;
            if (main is Shell sh && sh.CurrentPage is ContentPage scp) return scp;
            if (main is NavigationPage nav && nav.CurrentPage is ContentPage ncp) return ncp;
            if (main is IPageContainer<Page> cont && cont.CurrentPage is ContentPage c2) return c2;
            return null;
        }

        private static Grid? EnsureRootGrid(ContentPage cp)
        {
            if (cp.Content is Grid g) return g;
            if (cp.Content is View existing)
            {
                var wrapper = new Grid
                {
                    RowDefinitions = { new RowDefinition { Height = GridLength.Star } },
                    ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star } }
                };
                wrapper.Children.Add(existing);
                Grid.SetRow(existing, 0); Grid.SetColumn(existing, 0);
                cp.Content = wrapper;
                return wrapper;
            }
            return null;
        }

        private static Grid GetOrCreateOverlay(Grid root)
        {
            foreach (var child in root.Children)
                if (child is Grid g && g.AutomationId == "__toast_overlay_grid") return g;
            var overlay = new Grid
            {
                AutomationId = "__toast_overlay_grid",
                InputTransparent = true,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            };
            overlay.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            overlay.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            overlay.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetRow(overlay, 0); Grid.SetColumn(overlay, 0);
            Grid.SetRowSpan(overlay, Math.Max(1, root.RowDefinitions.Count == 0 ? 1 : root.RowDefinitions.Count));
            Grid.SetColumnSpan(overlay, Math.Max(1, root.ColumnDefinitions.Count == 0 ? 1 : root.ColumnDefinitions.Count));
            overlay.ZIndex = 9999; root.Children.Add(overlay); return overlay;
        }

        private static void CleanupOldToasts(Grid overlay)
        {
            try
            {
                for (int i = overlay.Children.Count - 1; i >= 0; i--)
                    if (overlay.Children[i] is Border b && b.AutomationId == "__toast_item" && b.Opacity <= 0.01)
                        overlay.Children.RemoveAt(i);
            }
            catch { }
        }

        private static (Color BG, Color Border, Color Text, string IconCode)
            GetColorsAndIcon(ToastTipo tipo, AppTheme theme)
        {
            Color border = tipo switch
            {
                ToastTipo.Erro    => Color.FromArgb("#EF4444"),
                ToastTipo.Sucesso => Color.FromArgb("#22C55E"),
                ToastTipo.Aviso   => (AvisoHue == WarningHue.Yellow ? Color.FromArgb("#FACC15") : Color.FromArgb("#F59E0B")),
                _                 => Color.FromArgb("#2563EB"),
            };
            float alpha = (theme == AppTheme.Dark) ? 0.45f : 0.55f;
            Color bg = border.WithAlpha(alpha);
            Color text = (theme == AppTheme.Dark) ? Colors.White : Colors.Black;
            string icon = tipo switch
            {
                ToastTipo.Erro    => "\uf057",
                ToastTipo.Sucesso => "\uf058",
                ToastTipo.Aviso   => "\uf071",
                _                 => "\uf05a"
            };
            return (bg, border, text, icon);
        }
    }
}
