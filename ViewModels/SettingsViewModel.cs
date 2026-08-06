using CommunityToolkit.Mvvm.ComponentModel;
using Demo.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Demo.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly LocalizationService _loc = LocalizationService.Instance;

    public string Title => _loc["SettingsPanel.Title"];
    public string LanguageLabel => _loc["Settings.LanguageLabel"];
    public string SectionDesktopTitle => _loc["Settings.SectionDesktop"];
    public string ShortcutsPlaceholder => _loc["Settings.ShortcutsPlaceholder"];

    public ObservableCollection<LanguageOption> LanguageOptions { get; } = new();

    [ObservableProperty]
    private LanguageOption? _selectedLanguage;

    public List<SettingsCategory> Categories { get; } = new()
    {
        new("Settings.CategoryGeneral", "\uE779", "General"),
        new("Settings.CategoryShortcuts", "\uE765", "Shortcuts"),
    };

    [ObservableProperty]
    private SettingsCategory _selectedCategory = null!;

    public bool IsGeneralSelected => SelectedCategory?.Tag == "General";
    public bool IsShortcutsSelected => SelectedCategory?.Tag == "Shortcuts";

    public SettingsViewModel()
    {
        _selectedCategory = Categories[0];

        _loc.PropertyChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(LanguageLabel));
            OnPropertyChanged(nameof(SectionDesktopTitle));
            OnPropertyChanged(nameof(ShortcutsPlaceholder));
            foreach (var c in Categories)
            {
                c.RefreshName();
            }
        };

        foreach (var option in _loc.GetLanguageOptions())
        {
            LanguageOptions.Add(option);
        }

        SelectedLanguage = LanguageOptions.FirstOrDefault(o => o.Code == _loc.CurrentLanguage);
    }

    partial void OnSelectedLanguageChanged(LanguageOption? value)
    {
        if (value != null)
        {
            _loc.SetLanguage(value.Code);
        }
    }

    partial void OnSelectedCategoryChanged(SettingsCategory value)
    {
        OnPropertyChanged(nameof(IsGeneralSelected));
        OnPropertyChanged(nameof(IsShortcutsSelected));
    }
}

public partial class SettingsCategory : ObservableObject
{
    private readonly LocalizationService _loc = LocalizationService.Instance;

    public string NameKey { get; }
    public string Icon { get; }
    public string Tag { get; }

    public string Name => _loc[NameKey];

    public SettingsCategory(string nameKey, string icon, string tag)
    {
        NameKey = nameKey;
        Icon = icon;
        Tag = tag;
    }

    public void RefreshName() => OnPropertyChanged(nameof(Name));
}
