using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace NAVIGEST.macOS.Behaviors;

public class MacInputBehavior : Behavior<View>
{
    protected override void OnAttachedTo(View bindable)
    {
        base.OnAttachedTo(bindable);
        if (bindable is VisualElement element)
        {
            element.Loaded += OnInputLoaded;
            element.Focused += OnInputLoaded;
            element.Unfocused += OnInputLoaded;
        }
    }

    protected override void OnDetachingFrom(View bindable)
    {
        base.OnDetachingFrom(bindable);
        if (bindable is VisualElement element)
        {
            element.Loaded -= OnInputLoaded;
            element.Focused -= OnInputLoaded;
            element.Unfocused -= OnInputLoaded;
        }
    }

    private async void OnInputLoaded(object sender, EventArgs e)
    {
#if MACCATALYST
        // Pequeno delay para garantir que executamos DEPOIS do sistema desenhar a borda nativa
        await Task.Delay(50);

        if (sender is IView view && view.Handler?.PlatformView is UIKit.UIView platformView)
        {
            if (platformView is UIKit.UITextField tf)
            {
                tf.BorderStyle = UIKit.UITextBorderStyle.None;
                tf.BackgroundColor = UIKit.UIColor.Clear;
                tf.Layer.BorderWidth = 0;
                tf.Layer.BorderColor = UIKit.UIColor.Clear.CGColor;
                tf.Background = null;
            }
            else if (platformView is UIKit.UITextView tv)
            {
                tv.BackgroundColor = UIKit.UIColor.Clear;
                tv.Layer.BorderWidth = 0;
                tv.Layer.BorderColor = UIKit.UIColor.Clear.CGColor;
            }
        }
#endif
    }
}
