using CommunityToolkit.Mvvm.ComponentModel;
using Demo.Services;
using System.Collections.ObjectModel;
using System.Linq;

namespace Demo.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly LocalizationService _loc = LocalizationService.Instance;

    public string Title => _loc["SettingsPanel.Title"];
    public string LanguageLabel => _loc["Settings.LanguageLabel"];

    public ObservableCollection<LanguageOption> LanguageOptions { get; } = new();

    [ObservableProperty]
    private LanguageOption? _selectedLanguage;

    public SettingsViewModel()
    {
        _loc.PropertyChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(LanguageLabel));
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
}
