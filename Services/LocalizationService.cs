using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Windows.Storage;

namespace Demo.Services;

public record LanguageOption(string Code, string DisplayName);

public partial class LocalizationService : ObservableObject
{
    public static LocalizationService Instance { get; } = new();

    private const string DefaultLanguage = "en-US";
    private const string LanguageSettingKey = "AppLanguage";

    private readonly Dictionary<string, Dictionary<string, string>> _allStrings = new();

    private static readonly Dictionary<string, string> _languageDisplayNames = new()
    {
        ["zh-CN"] = "中文（简体）",
        ["zh-TW"] = "中文（繁體）",
        ["en-US"] = "English",
        ["ja-JP"] = "日本語",
    };

    [ObservableProperty]
    private string _currentLanguage = DefaultLanguage;

    private LocalizationService()
    {
        LoadAllResources();
        CurrentLanguage = GetSavedOrSystemLanguage();
    }

    public string this[string key] => GetString(key);

    public string GetString(string key)
    {
        if (_allStrings.TryGetValue(CurrentLanguage, out var langDict) &&
            langDict.TryGetValue(key, out var value))
        {
            return value;
        }

        if (_allStrings.TryGetValue(DefaultLanguage, out var fallback) &&
            fallback.TryGetValue(key, out var fallbackValue))
        {
            return fallbackValue;
        }

        return key;
    }

    public void SetLanguage(string language)
    {
        if (CurrentLanguage == language) return;
        if (!_allStrings.ContainsKey(language)) return;

        CurrentLanguage = language;
        SaveLanguagePreference(language);
        OnPropertyChanged(string.Empty);
    }

    public IReadOnlyList<string> AvailableLanguages => _allStrings.Keys.ToList();

    public List<LanguageOption> GetLanguageOptions()
    {
        return _allStrings.Keys
            .Select(code => new LanguageOption(
                code,
                _languageDisplayNames.TryGetValue(code, out var name) ? name : code))
            .ToList();
    }

    private void LoadAllResources()
    {
        var stringsPath = Path.Combine(AppContext.BaseDirectory, "Strings");
        if (!Directory.Exists(stringsPath)) return;

        foreach (var langDir in Directory.GetDirectories(stringsPath))
        {
            var langCode = Path.GetFileName(langDir);
            var reswPath = Path.Combine(langDir, "Resources.resw");
            if (!File.Exists(reswPath)) continue;

            var dict = new Dictionary<string, string>();
            var xml = XDocument.Load(reswPath);
            foreach (var data in xml.Descendants("data"))
            {
                var name = data.Attribute("name")?.Value;
                var value = data.Element("value")?.Value;
                if (!string.IsNullOrEmpty(name) && value != null)
                {
                    dict[name] = value;
                }
            }

            if (dict.Count > 0)
            {
                _allStrings[langCode] = dict;
            }
        }
    }

    private string GetSavedOrSystemLanguage()
    {
        try
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.TryGetValue(LanguageSettingKey, out var saved) &&
                saved is string savedLang &&
                _allStrings.ContainsKey(savedLang))
            {
                return savedLang;
            }
        }
        catch { }

        var systemLang = CultureInfo.CurrentUICulture.Name;
        if (_allStrings.TryGetValue(systemLang, out _)) return systemLang;

        var parentLang = systemLang.Substring(0, 2);
        var match = _allStrings.Keys.FirstOrDefault(k => k.StartsWith(parentLang));
        if (match != null) return match;

        return DefaultLanguage;
    }

    private void SaveLanguagePreference(string language)
    {
        try
        {
            ApplicationData.Current.LocalSettings.Values[LanguageSettingKey] = language;
        }
        catch { }
    }
}
