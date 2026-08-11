using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Demo.Models;
using Demo.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Demo.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public Services.LocalizationService Loc => LocalizationService.Instance;

    public string CurrentBookTitle => BookLibrary.Instance.CurrentBook?.Title ?? Loc["AppTitle.Text"];
    public string EmptyHintText => Loc["Content.EmptyHint"];
    public string ParsingText => Loc["Import.Parsing"];
    public string SearchBoxPlaceholder => Loc["SearchBox.PlaceholderText"];
    public string SidebarEmptyHint => Loc["Sidebar.EmptyHint"];
    public bool HasCurrentBook => BookLibrary.Instance.CurrentBook != null;
    public bool ShowEmptyState => !HasCurrentBook;
    public bool HasChapters => Chapters.Count > 0;
    public bool ShowSidebarEmpty => !HasChapters;

    public string ChapterPositionText
    {
        get
        {
            var book = BookLibrary.Instance.CurrentBook;
            if (book == null || book.Chapters.Count == 0) return "";
            return string.Format(Loc["Reader.ChapterPosition"], book.CurrentChapterIndex + 1, book.Chapters.Count);
        }
    }

    public string ReadingPercentText
    {
        get
        {
            if (Paragraphs.Count == 0) return "0%";
            var index = Math.Clamp(CurrentParagraphIndex, 0, Paragraphs.Count - 1);
            return $"{(int)((double)index / Paragraphs.Count * 100)}%";
        }
    }

    public ObservableCollection<ReaderParagraph> Paragraphs { get; } = new();
    public ObservableCollection<BookChapter> Chapters { get; } = new();

    private List<BookChapter> _allChapters = new();

    public MainViewModel()
    {
        LocalizationService.Instance.PropertyChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(CurrentBookTitle));
            OnPropertyChanged(nameof(EmptyHintText));
            OnPropertyChanged(nameof(ParsingText));
            OnPropertyChanged(nameof(SearchBoxPlaceholder));
            OnPropertyChanged(nameof(SidebarEmptyHint));
            OnPropertyChanged(nameof(ChapterPositionText));
        };

        BookLibrary.Instance.CurrentBookChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(CurrentBookTitle));
            OnPropertyChanged(nameof(HasCurrentBook));
            OnPropertyChanged(nameof(ShowEmptyState));
            OnPropertyChanged(nameof(ChapterPositionText));
            OnPropertyChanged(nameof(ReadingPercentText));
            RefreshChapters();
        };

        Paragraphs.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(ReadingPercentText));
        };

        RefreshChapters();
    }

    public SettingsViewModel Settings { get; } = new();

    [ObservableProperty]
    public partial bool IsSidebarExpanded { get; set; } = false;

    [ObservableProperty]
    public partial bool IsSettingsOpen { get; set; }

    [ObservableProperty]
    public partial bool IsBooksOpen { get; set; }

    [ObservableProperty]
    public partial bool IsImporting { get; set; }

    [ObservableProperty]
    public partial string SearchText { get; set; } = "";

    [ObservableProperty]
    public partial BookChapter? SelectedChapter { get; set; }

    [ObservableProperty]
    public partial int CurrentParagraphIndex { get; set; }

    public bool IsProgrammaticChapterSelection { get; internal set; }

    public bool IsSearchNotEmpty => !string.IsNullOrEmpty(SearchText);

    partial void OnSearchTextChanged(string value)
    {
        OnPropertyChanged(nameof(IsSearchNotEmpty));
        ApplyChapterFilter();
    }

    partial void OnCurrentParagraphIndexChanged(int value)
    {
        OnPropertyChanged(nameof(ReadingPercentText));
    }

    partial void OnSelectedChapterChanged(BookChapter? value)
    {
        if (value == null) return;

        var book = BookLibrary.Instance.CurrentBook;
        if (book == null) return;

        var index = book.Chapters.IndexOf(value);
        if (index >= 0)
        {
            if (index != book.CurrentChapterIndex)
            {
                book.CurrentChapterIndex = index;
                book.CurrentParagraphIndex = 0;
                CurrentParagraphIndex = 0;
                BookLibrary.Instance.Save();
            }
            OnPropertyChanged(nameof(ChapterPositionText));
            OnPropertyChanged(nameof(ReadingPercentText));
        }
    }

    private void RefreshChapters()
    {
        _allChapters = BookLibrary.Instance.CurrentBook?.Chapters ?? new List<BookChapter>();
        CurrentParagraphIndex = BookLibrary.Instance.CurrentBook?.CurrentParagraphIndex ?? 0;
        ApplyChapterFilter();
    }

    private void ApplyChapterFilter()
    {
        var keyword = SearchText?.Trim() ?? "";
        IEnumerable<BookChapter> filtered = _allChapters;
        if (keyword.Length > 0)
        {
            filtered = _allChapters.Where(c => c.Title.Contains(keyword, StringComparison.CurrentCultureIgnoreCase));
        }

        Chapters.Clear();
        foreach (var chapter in filtered)
        {
            Chapters.Add(chapter);
        }

        OnPropertyChanged(nameof(HasChapters));
        OnPropertyChanged(nameof(ShowSidebarEmpty));

        var book = BookLibrary.Instance.CurrentBook;
        BookChapter? current = null;
        if (book != null && book.Chapters.Count > 0)
        {
            var index = Math.Clamp(book.CurrentChapterIndex, 0, book.Chapters.Count - 1);
            current = book.Chapters[index];
        }

        IsProgrammaticChapterSelection = true;
        SelectedChapter = current != null && Chapters.Contains(current) ? current : null;
        IsProgrammaticChapterSelection = false;
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
    private void OpenBooks()
    {
        IsBooksOpen = true;
    }

    [RelayCommand]
    private void CloseBooks()
    {
        IsBooksOpen = false;
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
    }
}
