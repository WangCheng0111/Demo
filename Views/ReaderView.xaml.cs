using Demo.Models;
using Demo.Services;
using Demo.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Demo.Views;

public sealed partial class ReaderView : UserControl
{
    private ScrollViewer? _readerScroller;
    private DispatcherQueueTimer? _saveTimer;

    public ReaderView()
    {
        InitializeComponent();
        Loaded += ReaderView_Loaded;
    }

    private MainViewModel? VM => DataContext as MainViewModel;

    private void ReaderView_Loaded(object sender, RoutedEventArgs e)
    {
        BookLibrary.Instance.CurrentBookChanged += (_, _) => LoadCurrentChapter();
        if (VM != null)
        {
            VM.PropertyChanged += OnViewModelPropertyChanged;
        }

        _readerScroller = FindDescendant<ScrollViewer>(readerView);
        if (_readerScroller != null)
        {
            _readerScroller.ViewChanged += (_, e) => OnReaderScrollerViewChanged(e);
        }

        _saveTimer = DispatcherQueue.CreateTimer();
        _saveTimer.Interval = TimeSpan.FromMilliseconds(800);
        _saveTimer.Tick += (_, _) =>
        {
            _saveTimer.Stop();
            BookLibrary.Instance.Save();
        };

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

        _saveTimer?.Stop();
        _saveTimer?.Start();
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

        var picker = new FileOpenPicker();
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindow!));
        picker.FileTypeFilter.Add(".txt");
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

        var file = await picker.PickSingleFileAsync();
        if (file == null) return;

        vm.IsImporting = true;
        try
        {
            await BookLibrary.Instance.ImportBookAsync(file.Path);
        }
        catch (Exception ex)
        {
            vm.Paragraphs.Clear();
            vm.Paragraphs.Add(new ReaderParagraph { Text = string.Format(vm.Loc["Import.Error"], ex.Message) });
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
        try
        {
            var paragraphs = await Task.Run(() => TxtParser.ReadChapterParagraphs(book, chapterIndex));
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
            vm.Paragraphs.Clear();
            vm.Paragraphs.Add(new ReaderParagraph { Text = string.Format(vm.Loc["Import.Error"], ex.Message) });
        }
    }
}
