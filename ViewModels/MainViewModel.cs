using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Demo.Services;
using System.ComponentModel;

namespace Demo.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public Services.LocalizationService Loc => LocalizationService.Instance;

    public string AppTitleText => Loc["AppTitle.Text"];
    public string SearchBoxPlaceholder => Loc["SearchBox.PlaceholderText"];
    public string ContentPlaceholderText => Loc["ContentPlaceholder.Text"];
    public string SettingsPanelTitle => Loc["SettingsPanel.Title"];

    public MainViewModel()
    {
        LocalizationService.Instance.PropertyChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(AppTitleText));
            OnPropertyChanged(nameof(SearchBoxPlaceholder));
            OnPropertyChanged(nameof(ContentPlaceholderText));
            OnPropertyChanged(nameof(SettingsPanelTitle));
        };
    }
    public SettingsViewModel Settings { get; } = new();

    [ObservableProperty]
    public partial bool IsSidebarExpanded { get; set; } = true;

    [ObservableProperty]
    public partial bool IsSettingsOpen { get; set; }

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
