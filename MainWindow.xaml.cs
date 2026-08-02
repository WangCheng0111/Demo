using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
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

        private readonly Storyboard _expandStoryboard = new();
        private readonly Storyboard _collapseStoryboard = new();
        private readonly Storyboard _borderFocusIn = new();
        private readonly Storyboard _borderFocusOut = new();

        public MainWindow()
        {
            InitializeComponent();

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
                searchBoxBorder.AddHandler(UIElement.PointerEnteredEvent, new PointerEventHandler(SearchBox_PointerEntered), true);
                searchBoxBorder.AddHandler(UIElement.PointerExitedEvent, new PointerEventHandler(SearchBox_PointerExited), true);
                searchBoxBorder.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(SearchBox_PointerPressed), true);
                searchBoxBorder.Shadow = new Microsoft.UI.Xaml.Media.ThemeShadow();
                searchBoxBorder.Translation = new Vector3(0, 0, 4);
            };
        }

        private void SetupAnimations()
        {
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            var duration = TimeSpan.FromMilliseconds(250);

            var expandAnim = new DoubleAnimation
            {
                From = -261,
                To = 0,
                Duration = duration,
                EasingFunction = ease
            };
            Storyboard.SetTarget(expandAnim, sidebarTransform);
            Storyboard.SetTargetProperty(expandAnim, "X");
            _expandStoryboard.Children.Add(expandAnim);

            var expandOpacity = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = duration,
                EasingFunction = ease
            };
            Storyboard.SetTarget(expandOpacity, sidebarContent);
            Storyboard.SetTargetProperty(expandOpacity, "Opacity");
            _expandStoryboard.Children.Add(expandOpacity);

            var expandContentMargin = new DoubleAnimation
            {
                From = 0.0,
                To = 261.0,
                Duration = duration,
                EasingFunction = ease,
                EnableDependentAnimation = true
            };
            Storyboard.SetTarget(expandContentMargin, contentSpacer);
            Storyboard.SetTargetProperty(expandContentMargin, "Width");
            _expandStoryboard.Children.Add(expandContentMargin);

            var collapseAnim = new DoubleAnimation
            {
                From = 0,
                To = -261,
                Duration = duration,
                EasingFunction = ease
            };
            Storyboard.SetTarget(collapseAnim, sidebarTransform);
            Storyboard.SetTargetProperty(collapseAnim, "X");
            _collapseStoryboard.Children.Add(collapseAnim);

            var collapseOpacity = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = duration,
                EasingFunction = ease
            };
            Storyboard.SetTarget(collapseOpacity, sidebarContent);
            Storyboard.SetTargetProperty(collapseOpacity, "Opacity");
            _collapseStoryboard.Children.Add(collapseOpacity);

            var collapseContentMargin = new DoubleAnimation
            {
                From = 261.0,
                To = 0.0,
                Duration = duration,
                EasingFunction = ease,
                EnableDependentAnimation = true
            };
            Storyboard.SetTarget(collapseContentMargin, contentSpacer);
            Storyboard.SetTargetProperty(collapseContentMargin, "Width");
            _collapseStoryboard.Children.Add(collapseContentMargin);
            _collapseStoryboard.Completed += (_, _) =>
            {
                if (!ViewModel.IsSidebarExpanded)
                {
                    sidebarSeparator.Visibility = Visibility.Collapsed;
                }
            };

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
            if (ViewModel.IsSidebarExpanded)
            {
                _collapseStoryboard.Stop();
                sidebarSeparator.Visibility = Visibility.Visible;
                _expandStoryboard.Begin();
            }
            else
            {
                _expandStoryboard.Stop();
                _collapseStoryboard.Begin();
            }
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
            AnimateBackground(Color.FromArgb(0xFF, 0xF4, 0xF4, 0xF4), 150);
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

            transform = SettingsButton.TransformToVisual(null);
            bounds = transform.TransformBounds(new Rect(0, 0,
                SettingsButton.ActualWidth, SettingsButton.ActualHeight));
            Windows.Graphics.RectInt32 SettingsRect = GetRect(bounds, scaleAdjustment);

            var rectArray = new Windows.Graphics.RectInt32[] { SidebarRect, SettingsRect };

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