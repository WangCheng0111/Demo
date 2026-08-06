using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Demo.Services;
using System.ComponentModel;
using Windows.Storage;

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
        IsMarkdownDark = ReadMarkdownDarkTheme();

        LocalizationService.Instance.PropertyChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(AppTitleText));
            OnPropertyChanged(nameof(SearchBoxPlaceholder));
            OnPropertyChanged(nameof(ContentPlaceholderText));
            OnPropertyChanged(nameof(SettingsPanelTitle));
        };
    }
    public SettingsViewModel Settings { get; } = new();

    public DocumentViewModel Document { get; } = new();

    [ObservableProperty]
    public partial bool IsSidebarExpanded { get; set; } = true;

    [ObservableProperty]
    public partial bool IsSettingsOpen { get; set; }

    [ObservableProperty]
    public partial string SearchText { get; set; } = "";

    [ObservableProperty]
    public partial bool IsMarkdownDark { get; set; }

    public bool IsSearchNotEmpty => !string.IsNullOrEmpty(SearchText);

    partial void OnSearchTextChanged(string value)
    {
        OnPropertyChanged(nameof(IsSearchNotEmpty));
    }

    partial void OnIsMarkdownDarkChanged(bool value)
    {
        SaveMarkdownDarkTheme(value);
    }

    private static bool ReadMarkdownDarkTheme()
    {
        try
        {
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue("MarkdownDarkTheme", out var saved) &&
                saved is bool b)
            {
                return b;
            }
        }
        catch { }
        return false;
    }

    private static void SaveMarkdownDarkTheme(bool dark)
    {
        try
        {
            ApplicationData.Current.LocalSettings.Values["MarkdownDarkTheme"] = dark;
        }
        catch { }
    }

    [RelayCommand]
    private void ToggleMarkdownTheme()
    {
        IsMarkdownDark = !IsMarkdownDark;
    }

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

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
    }
}
