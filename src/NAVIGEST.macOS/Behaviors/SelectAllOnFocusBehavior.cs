using Microsoft.Maui.Controls;

namespace NAVIGEST.macOS.Behaviors;

public class SelectAllOnFocusBehavior : Behavior<Entry>
{
    protected override void OnAttachedTo(Entry bindable)
    {
        base.OnAttachedTo(bindable);
        bindable.Focused += OnFocused;
    }

    protected override void OnDetachingFrom(Entry bindable)
    {
        base.OnDetachingFrom(bindable);
        bindable.Focused -= OnFocused;
    }

    private void OnFocused(object? sender, FocusEventArgs e)
    {
        if (sender is Entry entry && e.IsFocused)
        {
            // Small delay to ensure focus is fully processed
            entry.Dispatcher.Dispatch(() =>
            {
                entry.CursorPosition = 0;
                entry.SelectionLength = entry.Text?.Length ?? 0;
            });
        }
    }
}
