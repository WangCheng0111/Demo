using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Demo.Controls;

public class HandCursorButton : Button
{
    public HandCursorButton()
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
        IsTabStop = false;
    }

    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        _ = VisualStateManager.GoToState(this, "Pressed", true);
        CapturePointer(e.Pointer);
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerRoutedEventArgs e)
    {
        ReleasePointerCapture(e.Pointer);
        var pos = e.GetCurrentPoint(this).Position;
        var isOver = pos.X >= 0 && pos.Y >= 0 && pos.X <= ActualWidth && pos.Y <= ActualHeight;
        _ = VisualStateManager.GoToState(this, isOver ? "PointerOver" : "Normal", true);
        if (isOver && Command != null && Command.CanExecute(CommandParameter))
        {
            Command.Execute(CommandParameter);
        }
        e.Handled = true;
    }

    protected override void OnPointerExited(PointerRoutedEventArgs e)
    {
        base.OnPointerExited(e);
        _ = VisualStateManager.GoToState(this, "Normal", true);
    }
}
