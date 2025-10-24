using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace NAVIGEST.macOS.Behaviors
{
    /// <summary>
    /// Pequenas animações universais:
    /// - OnAttached: fade + slide-up
    /// - Pressed/Released: scale 0.98 -> 1.00
    /// </summary>
    public sealed class ButtonMicroAnimationsBehavior : Behavior<Button>
    {
        private Button? _button;

        protected override void OnAttachedTo(Button bindable)
        {
            base.OnAttachedTo(bindable);
            _button = bindable;

            // Animação de entrada
            _ = AnimateOnLoad(bindable);

            // Press/release
            bindable.Pressed += OnPressed;
            bindable.Released += OnReleased;
        }

        protected override void OnDetachingFrom(Button bindable)
        {
            base.OnDetachingFrom(bindable);

            bindable.Pressed -= OnPressed;
            bindable.Released -= OnReleased;
            _button = null;
        }

        private async Task AnimateOnLoad(VisualElement v)
        {
            try
            {
                v.Opacity = 0;
                v.TranslationY = 8;
                await Task.Delay(30);
                await v.FadeTo(1, 160, Easing.CubicOut);
                await v.TranslateTo(0, 0, 160, Easing.CubicOut);
            }
            catch { /* ignora anim cancel */ }
        }

        private async void OnPressed(object? sender, EventArgs e)
        {
            if (_button == null) return;
            try { await _button.ScaleTo(0.98, 80, Easing.CubicOut); } catch {}
        }

        private async void OnReleased(object? sender, EventArgs e)
        {
            if (_button == null) return;
            try { await _button.ScaleTo(1.0, 120, Easing.CubicOut); } catch {}
        }
    }
}
