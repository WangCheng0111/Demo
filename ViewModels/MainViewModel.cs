using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Demo.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isSidebarExpanded = true;

    [ObservableProperty]
    private bool _isSettingsOpen;

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarExpanded = !IsSidebarExpanded;
    }

    [RelayCommand]
    private void OpenSettings()
    {
        IsSettingsOpen = true;
    }

    [RelayCommand]
    private void CloseSettings()
    {
        IsSettingsOpen = false;
    }
}
