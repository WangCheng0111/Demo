using Demo.Models;
using Demo.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using System;

namespace Demo.Views;

public sealed partial class BooksPage : Page
{
    private readonly Storyboard _show = new();
    private readonly Storyboard _hide = new();

    public event EventHandler? CloseRequested;

    public BooksPage()
    {
        InitializeComponent();
        DataContext = BookLibrary.Instance;
        SetupAnimations();
        Loaded += BooksPage_Loaded;
    }

    private void SetupAnimations()
    {
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        var duration = TimeSpan.FromMilliseconds(200);

        var overlayShowOpacity = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = duration,
            EasingFunction = ease
        };
        Storyboard.SetTarget(overlayShowOpacity, overlay);
        Storyboard.SetTargetProperty(overlayShowOpacity, "Opacity");
        _show.Children.Add(overlayShowOpacity);

        var panelShowY = new DoubleAnimation
        {
            From = 50,
            To = 0,
            Duration = duration,
            EasingFunction = ease
        };
        Storyboard.SetTarget(panelShowY, panelTransform);
        Storyboard.SetTargetProperty(panelShowY, "Y");
        _show.Children.Add(panelShowY);

        var panelShowOpacity = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = duration,
            EasingFunction = ease
        };
        Storyboard.SetTarget(panelShowOpacity, panel);
        Storyboard.SetTargetProperty(panelShowOpacity, "Opacity");
        _show.Children.Add(panelShowOpacity);

        var overlayHideOpacity = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = duration,
            EasingFunction = ease
        };
        Storyboard.SetTarget(overlayHideOpacity, overlay);
        Storyboard.SetTargetProperty(overlayHideOpacity, "Opacity");
        _hide.Children.Add(overlayHideOpacity);

        var panelHideOpacity = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = duration,
            EasingFunction = ease
        };
        Storyboard.SetTarget(panelHideOpacity, panel);
        Storyboard.SetTargetProperty(panelHideOpacity, "Opacity");
        _hide.Children.Add(panelHideOpacity);

        var panelHideY = new DoubleAnimation
        {
            From = 0,
            To = -50,
            Duration = duration,
            EasingFunction = ease
        };
        Storyboard.SetTarget(panelHideY, panelTransform);
        Storyboard.SetTargetProperty(panelHideY, "Y");
        _hide.Children.Add(panelHideY);

        _hide.Completed += (_, _) =>
        {
            overlay.Visibility = Visibility.Collapsed;
        };
    }

    private void BooksPage_Loaded(object sender, RoutedEventArgs e)
    {
        overlay.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(Overlay_PointerPressed), true);
    }

    public void Show()
    {
        overlay.Visibility = Visibility.Visible;
        panelTransform.Y = 50;
        panel.Opacity = 0;

        panelTitle.Text = LocalizationService.Instance["BooksPanel.Title"];
        emptyHint.Text = LocalizationService.Instance["BooksPanel.EmptyHint"];
        emptyHint.Visibility = BookLibrary.Instance.Books.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        bookList.SelectedItem = BookLibrary.Instance.CurrentBook;

        _show.Begin();
    }

    public void Hide()
    {
        _hide.Begin();
    }

    private void Overlay_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (object.ReferenceEquals(e.OriginalSource, overlay))
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private void BookList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is Book book)
        {
            BookLibrary.Instance.SetCurrentBook(book);
        }
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
