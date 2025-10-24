#if WINDOWS
using System;
using System.Reflection;
using Microsoft.Maui.Controls;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace NAVIGEST.Android.Behaviors
{
    /// <summary>
    /// Cursor "mão" (Hand) no Windows/WinUI 3.
    /// Usa reflexão para definir FrameworkElement.ProtectedCursor, que é protegido em WinUI3.
    /// </summary>
    public sealed class HandCursorBehavior : Behavior<View>
    {
        private FrameworkElement? _fe;
        private static readonly PropertyInfo? ProtectedCursorProp =
            typeof(FrameworkElement).GetProperty("ProtectedCursor", BindingFlags.Instance | BindingFlags.NonPublic);

        protected override void OnAttachedTo(View bindable)
        {
            base.OnAttachedTo(bindable);
            bindable.HandlerChanged += OnHandlerChanged;
        }

        protected override void OnDetachingFrom(View bindable)
        {
            bindable.HandlerChanged -= OnHandlerChanged;
            Unsubscribe();
            base.OnDetachingFrom(bindable);
        }

        private void OnHandlerChanged(object? sender, EventArgs e)
        {
            if (sender is not View view) return;

            Unsubscribe();

            if (view.Handler?.PlatformView is FrameworkElement fe)
            {
                _fe = fe;
                _fe.PointerEntered += OnPointerEntered;
                _fe.PointerMoved += OnPointerMoved;
                _fe.PointerExited += OnPointerExited;
            }
        }

        private void SetHand() => ProtectedCursorProp?.SetValue(_fe, InputSystemCursor.Create(InputSystemCursorShape.Hand));
        private void SetArrow() => ProtectedCursorProp?.SetValue(_fe, null); // null = cursor padrão

        private void OnPointerEntered(object sender, PointerRoutedEventArgs e) => SetHand();
        private void OnPointerMoved(object sender, PointerRoutedEventArgs e) => SetHand();
        private void OnPointerExited(object sender, PointerRoutedEventArgs e) => SetArrow();

        private void Unsubscribe()
        {
            if (_fe is null) return;
            _fe.PointerEntered -= OnPointerEntered;
            _fe.PointerMoved -= OnPointerMoved;
            _fe.PointerExited -= OnPointerExited;
            SetArrow();
            _fe = null;
        }
    }
}
#else
namespace NAVIGEST.Android.Behaviors
{
    // No-op noutras plataformas
    public sealed class HandCursorBehavior : Behavior<Microsoft.Maui.Controls.View> { }
}
#endif

