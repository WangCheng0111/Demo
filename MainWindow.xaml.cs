using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Demo.Services;
using Demo.ViewModels;
using Demo.Views;
using System;
using System.Numerics;
using Windows.Foundation;
using Windows.Graphics;
using Windows.UI;
using Rect = Windows.Foundation.Rect;

namespace Demo
{
    public sealed partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; } = new();

        private readonly Storyboard _borderFocusIn = new();
        private readonly Storyboard _borderFocusOut = new();

        private const double MinSidebarWidth = 180;
        private double _sidebarWidth = 261;
        private Storyboard? _sidebarStoryboard;
        private bool _isResizingSidebar;
        private double _resizeStartPointerX;
        private double _resizeStartWidth;

        public MainWindow()
        {
            InitializeComponent();

            rootGrid.DataContext = ViewModel;

            ExtendsContentIntoTitleBar = true;
            if (ExtendsContentIntoTitleBar == true)
            {
                AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
            }

            SetupAnimations();

            AppWindow.Changed += AppWindow_Changed;
            Activated += MainWindow_Activated;
            AppTitleBar.SizeChanged += AppTitleBar_SizeChanged;
            AppTitleBar.Loaded += AppTitleBar_Loaded;

            ViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.IsSidebarExpanded))
                {
                    AnimateSidebar();
                }
                else if (e.PropertyName == nameof(MainViewModel.IsSettingsOpen))
                {
                    if (ViewModel.IsSettingsOpen)
                    {
                        settingsHost.Show();
                    }
                    else
                    {
                        settingsHost.Hide();
                    }
                }
                else if (e.PropertyName == nameof(MainViewModel.IsBooksOpen))
                {
                    if (ViewModel.IsBooksOpen)
                    {
                        booksHost.Show();
                    }
                    else
                    {
                        booksHost.Hide();
                    }
                }
                else if (e.PropertyName == nameof(MainViewModel.SelectedChapter))
                {
                    if (ViewModel.IsProgrammaticChapterSelection)
                    {
                        ScrollToSelectedChapter();
                    }
                }
            };

            var displayArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
            var workArea = displayArea.WorkArea;
            var width = (int)(workArea.Width * 0.75);
            var height = (int)(workArea.Height * 0.80);
            var winX = workArea.X + (workArea.Width - width) / 2;
            var winY = workArea.Y + (workArea.Height - height) / 2;
            AppWindow.MoveAndResize(new RectInt32(winX, winY, width, height));

            rootGrid.Loaded += (_, _) =>
            {
                settingsHost.DataContext = ViewModel.Settings;
                settingsHost.CloseRequested += (_, _) => ViewModel.CloseSettingsCommand.Execute(null);
                booksHost.CloseRequested += (_, _) => ViewModel.CloseBooksCommand.Execute(null);
                searchBoxBorder.AddHandler(UIElement.PointerEnteredEvent, new PointerEventHandler(SearchBox_PointerEntered), true);
                searchBoxBorder.AddHandler(UIElement.PointerExitedEvent, new PointerEventHandler(SearchBox_PointerExited), true);
                searchBoxBorder.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(SearchBox_PointerPressed), true);
                searchBoxBorder.Shadow = new Microsoft.UI.Xaml.Media.ThemeShadow();
                searchBoxBorder.Translation = new Vector3(0, 0, 4);

                ScrollToSelectedChapter();
            };

            Closed += (_, _) => BookLibrary.Instance.Save();
        }

        private void ScrollToSelectedChapter()
        {
            var chapter = ViewModel.SelectedChapter;
            if (chapter == null) return;

            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                if (ViewModel.SelectedChapter != chapter) return;
                chapterList.ScrollIntoView(chapter, ScrollIntoViewAlignment.Leading);
            });
        }

        private void SetupAnimations()
        {
            var borderEase = new CubicEase { EasingMode = EasingMode.EaseOut };
            var borderDuration = TimeSpan.FromMilliseconds(200);

            var focusIn = new ColorAnimation
            {
                To = Color.FromArgb(0xFF, 0x70, 0x70, 0x70),
                Duration = borderDuration,
                EasingFunction = borderEase
            };
            Storyboard.SetTarget(focusIn, searchBoxBorder);
            Storyboard.SetTargetProperty(focusIn, "(Border.BorderBrush).(SolidColorBrush.Color)");
            _borderFocusIn.Children.Add(focusIn);

            var focusOut = new ColorAnimation
            {
                To = Color.FromArgb(0x60, 0x90, 0x90, 0x90),
                Duration = borderDuration,
                EasingFunction = borderEase
            };
            Storyboard.SetTarget(focusOut, searchBoxBorder);
            Storyboard.SetTargetProperty(focusOut, "(Border.BorderBrush).(SolidColorBrush.Color)");
            _borderFocusOut.Children.Add(focusOut);
        }

        private void AnimateBackground(Color to, int durationMs)
        {
            var anim = new ColorAnimation
            {
                To = to,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            var sb = new Storyboard();
            sb.Children.Add(anim);
            Storyboard.SetTarget(anim, searchBoxBorder);
            Storyboard.SetTargetProperty(anim, "(Border.Background).(SolidColorBrush.Color)");
            sb.Begin();
        }

        private void AnimateSidebar()
        {
            _sidebarStoryboard?.Stop();
            var storyboard = BuildSidebarStoryboard(ViewModel.IsSidebarExpanded);
            _sidebarStoryboard = storyboard;
            if (ViewModel.IsSidebarExpanded)
            {
                sidebarSeparator.Visibility = Visibility.Visible;
            }
            storyboard.Begin();
        }

        private Storyboard BuildSidebarStoryboard(bool expand)
        {
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            var duration = TimeSpan.FromMilliseconds(250);

            var storyboard = new Storyboard();

            var xAnim = new DoubleAnimation
            {
                From = expand ? -_sidebarWidth : 0,
                To = expand ? 0 : -_sidebarWidth,
                Duration = duration,
                EasingFunction = ease
            };
            Storyboard.SetTarget(xAnim, sidebarTransform);
            Storyboard.SetTargetProperty(xAnim, "X");
            storyboard.Children.Add(xAnim);

            var opacityAnim = new DoubleAnimation
            {
                From = expand ? 0 : 1,
                To = expand ? 1 : 0,
                Duration = duration,
                EasingFunction = ease
            };
            Storyboard.SetTarget(opacityAnim, sidebarContent);
            Storyboard.SetTargetProperty(opacityAnim, "Opacity");
            storyboard.Children.Add(opacityAnim);

            var widthAnim = new DoubleAnimation
            {
                From = expand ? 0 : _sidebarWidth,
                To = expand ? _sidebarWidth : 0,
                Duration = duration,
                EasingFunction = ease,
                EnableDependentAnimation = true
            };
            Storyboard.SetTarget(widthAnim, contentSpacer);
            Storyboard.SetTargetProperty(widthAnim, "Width");
            storyboard.Children.Add(widthAnim);

            if (!expand)
            {
                storyboard.Completed += (_, _) =>
                {
                    if (!ViewModel.IsSidebarExpanded)
                    {
                        sidebarSeparator.Visibility = Visibility.Collapsed;
                    }
                };
            }

            return storyboard;
        }

        private double MaxSidebarWidth => rootGrid.ActualWidth * 0.5;

        private void SidebarResizeHandle_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (!ViewModel.IsSidebarExpanded) return;

            _sidebarStoryboard?.Stop();
            sidebarTransform.X = 0;
            sidebarContent.Opacity = 1;
            contentSpacer.Width = _sidebarWidth;
            sidebarSeparator.Visibility = Visibility.Visible;

            _isResizingSidebar = true;
            _resizeStartPointerX = e.GetCurrentPoint(rootGrid).Position.X;
            _resizeStartWidth = _sidebarWidth;
            sidebarResizeHandle.CapturePointer(e.Pointer);
            e.Handled = true;
        }

        private void SidebarResizeHandle_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_isResizingSidebar) return;

            var currentX = e.GetCurrentPoint(rootGrid).Position.X;
            var newWidth = Math.Clamp(_resizeStartWidth + (currentX - _resizeStartPointerX), MinSidebarWidth, MaxSidebarWidth);
            if (Math.Abs(newWidth - _sidebarWidth) < 0.5) return;

            _sidebarWidth = newWidth;
            sidebarContainer.Width = newWidth;
            contentSpacer.Width = newWidth;
            sidebarTransform.X = 0;
        }

        private void SidebarResizeHandle_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (!_isResizingSidebar) return;
            _isResizingSidebar = false;
            sidebarResizeHandle.ReleasePointerCapture(e.Pointer);
        }

        private void SidebarResizeHandle_PointerCanceled(object sender, PointerRoutedEventArgs e)
        {
            if (!_isResizingSidebar) return;
            _isResizingSidebar = false;
            sidebarResizeHandle.ReleasePointerCapture(e.Pointer);
        }

        

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            _borderFocusOut.Stop();
            _borderFocusIn.Begin();
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            _borderFocusOut.Begin();
        }

        private void SearchBox_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            var color = searchBox.FocusState != FocusState.Unfocused
                ? Color.FromArgb(0xFF, 0xFB, 0xFB, 0xFA)
                : Color.FromArgb(0xFF, 0xF4, 0xF4, 0xF4);
            AnimateBackground(color, 150);
        }

        private void SearchBox_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            AnimateBackground(Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF), 150);
        }

        private void SearchBox_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            AnimateBackground(Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF), 80);
        }

        

        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            var foreground = args.WindowActivationState == WindowActivationState.Deactivated
                ? (SolidColorBrush)App.Current.Resources["WindowCaptionForegroundDisabled"]
                : (SolidColorBrush)App.Current.Resources["WindowCaptionForeground"];

            TitleBarTextBlock.Foreground = foreground;
            SidebarToggleButton.Foreground = foreground;
            BooksButton.Foreground = foreground;
            SettingsButton.Foreground = foreground;
        }

        private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
        {
            if (args.DidPresenterChange)
            {
                switch (sender.Presenter.Kind)
                {
                    case AppWindowPresenterKind.CompactOverlay:
                        AppTitleBar.Visibility = Visibility.Collapsed;
                        sender.TitleBar.ResetToDefault();
                        break;

                    case AppWindowPresenterKind.FullScreen:
                        AppTitleBar.Visibility = Visibility.Collapsed;
                        sender.TitleBar.ExtendsContentIntoTitleBar = true;
                        break;

                    case AppWindowPresenterKind.Overlapped:
                        AppTitleBar.Visibility = Visibility.Visible;
                        sender.TitleBar.ExtendsContentIntoTitleBar = true;
                        break;

                    default:
                        sender.TitleBar.ResetToDefault();
                        break;
                }
            }
        }

        private void AppTitleBar_Loaded(object sender, RoutedEventArgs e)
        {
            if (ExtendsContentIntoTitleBar == true)
            {
                SetRegionsForCustomTitleBar();
            }
        }

        private void AppTitleBar_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ExtendsContentIntoTitleBar == true)
            {
                SetRegionsForCustomTitleBar();
            }
        }

        private void SetRegionsForCustomTitleBar()
        {
            double scaleAdjustment = AppTitleBar.XamlRoot.RasterizationScale;

            RightPaddingColumn.Width = new GridLength(AppWindow.TitleBar.RightInset / scaleAdjustment);
            LeftPaddingColumn.Width = new GridLength(AppWindow.TitleBar.LeftInset / scaleAdjustment);

            GeneralTransform transform = SidebarToggleButton.TransformToVisual(null);
            Rect bounds = transform.TransformBounds(new Rect(0, 0,
                SidebarToggleButton.ActualWidth, SidebarToggleButton.ActualHeight));
            Windows.Graphics.RectInt32 SidebarRect = GetRect(bounds, scaleAdjustment);

            transform = BooksButton.TransformToVisual(null);
            bounds = transform.TransformBounds(new Rect(0, 0,
                BooksButton.ActualWidth, BooksButton.ActualHeight));
            Windows.Graphics.RectInt32 BooksRect = GetRect(bounds, scaleAdjustment);

            transform = SettingsButton.TransformToVisual(null);
            bounds = transform.TransformBounds(new Rect(0, 0,
                SettingsButton.ActualWidth, SettingsButton.ActualHeight));
            Windows.Graphics.RectInt32 SettingsRect = GetRect(bounds, scaleAdjustment);

            var rectArray = new Windows.Graphics.RectInt32[] { SidebarRect, BooksRect, SettingsRect };

            InputNonClientPointerSource nonClientInputSrc =
                InputNonClientPointerSource.GetForWindowId(AppWindow.Id);
            nonClientInputSrc.SetRegionRects(NonClientRegionKind.Passthrough, rectArray);
        }

        private Windows.Graphics.RectInt32 GetRect(Rect bounds, double scale)
        {
            return new Windows.Graphics.RectInt32(
                _X: (int)Math.Round(bounds.X * scale),
                _Y: (int)Math.Round(bounds.Y * scale),
                _Width: (int)Math.Round(bounds.Width * scale),
                _Height: (int)Math.Round(bounds.Height * scale));
        }
    }
}