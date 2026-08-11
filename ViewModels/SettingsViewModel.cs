using CommunityToolkit.Mvvm.ComponentModel;
using Demo.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Storage;

namespace Demo.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private const string BodyFontSizeKey = "ReaderBodyFontSize";
    private const string TitleFontSizeKey = "ReaderTitleFontSize";
    private const string FontFamilyKey = "ReaderFontFamily";
    private const string DefaultFontSource = "ms-appx:///Fonts/HarmonyOS_SansSC_Regular.ttf#HarmonyOS Sans SC";

    private readonly LocalizationService _loc = LocalizationService.Instance;

    public string Title => _loc["SettingsPanel.Title"];
    public string LanguageLabel => _loc["Settings.LanguageLabel"];
    public string SectionDesktopTitle => _loc["Settings.SectionDesktop"];
    public string ShortcutsPlaceholder => _loc["Settings.ShortcutsPlaceholder"];
    public string BodyFontSizeLabel => _loc["Settings.BodyFontSize"];
    public string TitleFontSizeLabel => _loc["Settings.TitleFontSize"];
    public string FontFamilyLabel => _loc["Settings.FontFamily"];

    public ObservableCollection<LanguageOption> LanguageOptions { get; } = new();
    public ObservableCollection<FontOption> FontOptions { get; } = new();

    [ObservableProperty]
    private LanguageOption? _selectedLanguage;

    [ObservableProperty]
    public partial double BodyFontSize { get; set; } = 15;

    [ObservableProperty]
    public partial double TitleFontSize { get; set; } = 19;

    [ObservableProperty]
    public partial FontOption? SelectedFont { get; set; }

    public string FontFamilySource => SelectedFont?.Source ?? DefaultFontSource;

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
        BodyFontSize = LoadSetting(BodyFontSizeKey, 15);
        TitleFontSize = LoadSetting(TitleFontSizeKey, 19);

        FontOptions.Add(new FontOption("ms-appx:///Fonts/HarmonyOS_SansSC_Regular.ttf#HarmonyOS Sans SC", "HarmonyOS Sans SC"));
        FontOptions.Add(new FontOption("Microsoft YaHei", "微软雅黑"));
        FontOptions.Add(new FontOption("SimSun", "宋体"));
        FontOptions.Add(new FontOption("KaiTi", "楷体"));
        SelectedFont = FontOptions.FirstOrDefault(f => f.Source == LoadStringSetting(FontFamilyKey))
            ?? FontOptions.First(f => f.Source == DefaultFontSource);

        _loc.PropertyChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(LanguageLabel));
            OnPropertyChanged(nameof(SectionDesktopTitle));
            OnPropertyChanged(nameof(ShortcutsPlaceholder));
            OnPropertyChanged(nameof(BodyFontSizeLabel));
            OnPropertyChanged(nameof(TitleFontSizeLabel));
            OnPropertyChanged(nameof(FontFamilyLabel));
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

    partial void OnBodyFontSizeChanged(double value)
    {
        SaveSetting(BodyFontSizeKey, value);
    }

    partial void OnTitleFontSizeChanged(double value)
    {
        SaveSetting(TitleFontSizeKey, value);
    }

    partial void OnSelectedFontChanged(FontOption? value)
    {
        if (value != null)
        {
            SaveStringSetting(FontFamilyKey, value.Source);
        }
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

    private static double LoadSetting(string key, double fallback)
    {
        try
        {
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue(key, out var value))
            {
                if (value is double d) return d;
                if (value is int i) return i;
            }
        }
        catch { }
        return fallback;
    }

    private static void SaveSetting(string key, double value)
    {
        try
        {
            ApplicationData.Current.LocalSettings.Values[key] = value;
        }
        catch { }
    }

    private static string? LoadStringSetting(string key)
    {
        try
        {
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue(key, out var value) && value is string s)
            {
                return s;
            }
        }
        catch { }
        return null;
    }

    private static void SaveStringSetting(string key, string value)
    {
        try
        {
            ApplicationData.Current.LocalSettings.Values[key] = value;
        }
        catch { }
    }
}

public record FontOption(string Source, string DisplayName);

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
