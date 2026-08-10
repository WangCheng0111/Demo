using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;

namespace Demo.Controls;

public class SidebarResizeHandle : Grid
{
    public SidebarResizeHandle()
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
    }
}
