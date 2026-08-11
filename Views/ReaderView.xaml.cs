using Demo.Models;
using Demo.Services;
using Demo.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Demo.Views;

public sealed partial class ReaderView : UserControl
{
    private ScrollViewer? _readerScroller;
    private int _loadToken;

    public ReaderView()
    {
        InitializeComponent();
        Loaded += ReaderView_Loaded;
    }

    private MainViewModel? VM => DataContext as MainViewModel;

    private void ReaderView_Loaded(object sender, RoutedEventArgs e)
    {
        if (VM != null)
        {
            VM.PropertyChanged += OnViewModelPropertyChanged;
            VM.Settings.PropertyChanged += OnSettingsPropertyChanged;
        }

        _readerScroller = FindDescendant<ScrollViewer>(readerView);
        if (_readerScroller != null)
        {
            _readerScroller.ViewChanged += (_, e) => OnReaderScrollerViewChanged(e);
        }

        if (BookLibrary.Instance.CurrentBook != null)
        {
            LoadCurrentChapter();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.SelectedChapter))
        {
            LoadCurrentChapter();
        }
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(SettingsViewModel.BodyFontSize) &&
            e.PropertyName != nameof(SettingsViewModel.TitleFontSize) &&
            e.PropertyName != nameof(SettingsViewModel.SelectedFont))
        {
            return;
        }
        if (VM is not { } vm) return;

        var fontFamily = new FontFamily(vm.Settings.FontFamilySource);
        foreach (var p in vm.Paragraphs)
        {
            p.BodyFontSize = vm.Settings.BodyFontSize;
            p.TitleFontSize = vm.Settings.TitleFontSize;
            p.FontFamily = fontFamily;
        }
    }

    private void OnReaderScrollerViewChanged(ScrollViewerViewChangedEventArgs e)
    {
        if (e.IsIntermediate) return;
        if (VM is not { } vm) return;
        if (readerView.Items.Count == 0) return;

        var topIndex = GetTopVisibleParagraphIndex();
        if (topIndex < 0) return;

        vm.CurrentParagraphIndex = topIndex;
        var book = BookLibrary.Instance.CurrentBook;
        if (book != null)
        {
            book.CurrentParagraphIndex = topIndex;
        }

        BookLibrary.Instance.SaveDebounced();
    }

    private int GetTopVisibleParagraphIndex()
    {
        for (int i = 0; i < readerView.Items.Count; i++)
        {
            if (readerView.ContainerFromIndex(i) is not ListViewItem container) continue;
            var y = container.TransformToVisual(readerView).TransformPoint(new Point(0, 0)).Y;
            if (y >= -1) return i;
        }
        return -1;
    }

    private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) return match;
            var result = FindDescendant<T>(child);
            if (result != null) return result;
        }
        return null;
    }

    private async void ContentEmptyHint_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (VM is not { } vm || vm.IsImporting) return;

        var file = await BookImporter.PickTxtFileAsync(App.MainWindow!);
        if (file == null) return;

        vm.IsImporting = true;
        try
        {
            var error = await BookImporter.ImportAsync(file.Path);
            if (error != null)
            {
                vm.Paragraphs.Clear();
                vm.Paragraphs.Add(new ReaderParagraph { Text = string.Format(vm.Loc["Import.Error"], error.Message) });
            }
        }
        finally
        {
            vm.IsImporting = false;
        }
    }

    private async void LoadCurrentChapter()
    {
        if (VM is not { } vm) return;

        var book = BookLibrary.Instance.CurrentBook;
        if (book == null)
        {
            vm.Paragraphs.Clear();
            return;
        }
        if (book.Chapters.Count == 0) return;

        var chapterIndex = Math.Clamp(book.CurrentChapterIndex, 0, book.Chapters.Count - 1);
        var token = ++_loadToken;
        try
        {
            var paragraphs = await Task.Run(() => TxtParser.ReadChapterParagraphs(book, chapterIndex));
            if (token != _loadToken) return;

            var fontFamily = new FontFamily(vm.Settings.FontFamilySource);
            foreach (var p in paragraphs)
            {
                p.BodyFontSize = vm.Settings.BodyFontSize;
                p.TitleFontSize = vm.Settings.TitleFontSize;
                p.FontFamily = fontFamily;
            }
            vm.Paragraphs.Clear();
            foreach (var p in paragraphs)
            {
                vm.Paragraphs.Add(p);
            }

            var savedIndex = Math.Clamp(book.CurrentParagraphIndex, 0, paragraphs.Count - 1);
            if (savedIndex > 0 && paragraphs.Count > 0)
            {
                readerView.ScrollIntoView(paragraphs[savedIndex], ScrollIntoViewAlignment.Leading);
            }
        }
        catch (Exception ex)
        {
            if (token != _loadToken) return;
            vm.Paragraphs.Clear();
            vm.Paragraphs.Add(new ReaderParagraph { Text = string.Format(vm.Loc["Reader.LoadError"], ex.Message) });
        }
    }
}
