using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;

namespace Demo.Models;

public partial class ReaderParagraph : ObservableObject
{
    public string Text { get; init; } = "";
    public bool IsTitle { get; init; }
    public bool IsBody => !IsTitle;

    [ObservableProperty]
    public partial double BodyFontSize { get; set; }

    [ObservableProperty]
    public partial double TitleFontSize { get; set; }

    [ObservableProperty]
    public partial FontFamily? FontFamily { get; set; }
}
