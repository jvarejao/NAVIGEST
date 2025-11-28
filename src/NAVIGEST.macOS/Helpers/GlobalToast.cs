using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel; // MainThread, DeviceInfo
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes; // RoundRectangle
using Microsoft.Maui.Graphics;

namespace NAVIGEST.macOS
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
        private const double MaxWidthPhone = 380;
        private const double MaxWidthLarge = 520;
        private const double MinWidth      = 220;
        private const int    MaxQueue      = 4;

        private static readonly SemaphoreSlim _queueSem = new(1, 1);
        private static readonly Queue<(string msg, ToastTipo tipo, int ms, ToastPos pos)> _queue = new();
        private static bool _showing;

        public static WarningHue AvisoHue { get; set; } = WarningHue.Orange;

        public static Task ShowAsync(string mensagem, ToastTipo tipo, int ms = 3000)
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
                overlay.Children.Clear(); // Clear old toasts for clean look

                var theme = Application.Current?.RequestedTheme ?? AppTheme.Light;
                var (bg, accentColor, textColor, iconCode) = GetColorsAndIcon(tipo, theme);

                // --- PREMIUM UI CONSTRUCTION ---
                
                var toastBorder = new Border
                {
                    StrokeShape = new RoundRectangle { CornerRadius = 18 },
                    StrokeThickness = 2,
                    Stroke = accentColor,
                    BackgroundColor = bg,
                    Padding = new Thickness(20, 16),
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = pos == ToastPos.Top ? LayoutOptions.Start : (pos == ToastPos.Center ? LayoutOptions.Center : LayoutOptions.End),
                    Margin = new Thickness(20, pos == ToastPos.Top ? 60 : 20, 20, pos == ToastPos.Bottom ? 40 : 20),
                    Opacity = 0,
                    TranslationY = pos == ToastPos.Top ? -50 : 50, // Start offset for animation
                    WidthRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? MaxWidthPhone : MaxWidthLarge,
                    Shadow = new Shadow
                    {
                        Brush = Brush.Black,
                        Opacity = 0.25f,
                        Radius = 12,
                        Offset = new Point(0, 6)
                    },
                    InputTransparent = false // Ensure the toast itself is interactive (for close button)
                };

                var grid = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitionCollection
                    {
                        new ColumnDefinition { Width = GridLength.Auto }, // Icon
                        new ColumnDefinition { Width = GridLength.Star }, // Message
                        new ColumnDefinition { Width = GridLength.Auto }  // Close
                    },
                    ColumnSpacing = 16
                };

                // 1. Icon Container (Circle background)
                var iconContainer = new Border
                {
                    StrokeShape = new RoundRectangle { CornerRadius = 24 },
                    StrokeThickness = 0,
                    BackgroundColor = accentColor.WithAlpha(0.15f),
                    HeightRequest = 48,
                    WidthRequest = 48,
                    VerticalOptions = LayoutOptions.Center
                };
                
                var iconLabel = new Label
                {
                    Text = iconCode,
                    FontFamily = "FA7Solid",
                    FontSize = 24,
                    TextColor = accentColor,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                };
                iconContainer.Content = iconLabel;

                // 2. Message
                var msgLabel = new Label
                {
                    Text = mensagem,
                    TextColor = textColor,
                    FontSize = 16,
                    FontAttributes = FontAttributes.Bold,
                    VerticalOptions = LayoutOptions.Center,
                    LineBreakMode = LineBreakMode.WordWrap
                };

                // 3. Close Button
                var closeBtn = new Label
                {
                    Text = "✕",
                    TextColor = textColor.WithAlpha(0.5f),
                    FontSize = 16,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.End,
                    Padding = new Thickness(4) // Hit target
                };
                var closeTap = new TapGestureRecognizer();
                closeBtn.GestureRecognizers.Add(closeTap);

                grid.Add(iconContainer, 0, 0);
                grid.Add(msgLabel, 1, 0);
                grid.Add(closeBtn, 2, 0);

                toastBorder.Content = grid;
                overlay.Add(toastBorder);

                // --- ANIMATION ---
                
                // Slide In & Fade In
                await Task.WhenAll(
                    toastBorder.FadeTo(1, 250, Easing.CubicOut),
                    toastBorder.TranslateTo(0, 0, 250, Easing.CubicOut)
                );

                // Wait or Close
                var tcs = new TaskCompletionSource<bool>();
                using var cts = new CancellationTokenSource();
                
                // Auto dismiss timer
                var timerTask = Task.Delay(ms, cts.Token);
                
                // Manual dismiss
                closeTap.Tapped += (s, e) => tcs.TrySetResult(true);

                var completedTask = await Task.WhenAny(timerTask, tcs.Task);

                if (completedTask == tcs.Task)
                {
                    cts.Cancel(); // Cancel timer if manually closed
                }

                // Slide Out & Fade Out
                await Task.WhenAll(
                    toastBorder.FadeTo(0, 200, Easing.CubicIn),
                    toastBorder.TranslateTo(0, pos == ToastPos.Top ? -20 : 20, 200, Easing.CubicIn)
                );

                overlay.Children.Remove(toastBorder);
                
                // Cleanup overlay if empty to ensure no Z-Index blocking issues
                if (overlay.Children.Count == 0)
                {
                    var parent = overlay.Parent as Grid;
                    parent?.Children.Remove(overlay);
                }
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
                InputTransparent = true, // Allow clicks to pass through empty areas
                CascadeInputTransparent = false, // IMPORTANT: Allow children (toasts) to be interactive
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

        private static (Color bg, Color accent, Color text, string icon) GetColorsAndIcon(ToastTipo tipo, AppTheme theme)
        {
            bool isDark = theme == AppTheme.Dark;
            
            // Background: White vs Dark Grey
            Color bg = isDark ? Color.FromArgb("#2C2C2E") : Colors.White;
            Color text = isDark ? Colors.White : Color.FromArgb("#1C1C1E");

            return tipo switch
            {
                ToastTipo.Sucesso => (bg, Color.FromArgb("#34C759"), text, "\uf00c"), // Check
                ToastTipo.Erro    => (bg, Color.FromArgb("#FF3B30"), text, "\uf00d"), // X
                ToastTipo.Aviso   => (bg, Color.FromArgb("#FF9500"), text, "\uf12a"), // !
                _                 => (bg, Color.FromArgb("#0A84FF"), text, "\uf05a")  // i
            };
        }
    }
}
