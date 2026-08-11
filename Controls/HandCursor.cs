using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;

namespace Demo.Controls;

public class HandCursor : Button
{
    public HandCursor()
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
    }
}
