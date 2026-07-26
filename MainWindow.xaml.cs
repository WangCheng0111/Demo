using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Demo.ViewModels;
using System;
using System.Numerics;
using Windows.Graphics;
using Windows.UI;

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
            SetTitleBar(AppTitleBar);
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;

            SetupAnimations();

            ViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.IsSidebarExpanded))
                {
                    AnimateSidebar();
                }
            };

            rootGrid.Loaded += (_, _) =>
            {
                searchBoxBorder.AddHandler(UIElement.PointerEnteredEvent, new PointerEventHandler(SearchBox_PointerEntered), true);
                searchBoxBorder.AddHandler(UIElement.PointerExitedEvent, new PointerEventHandler(SearchBox_PointerExited), true);
                searchBoxBorder.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(SearchBox_PointerPressed), true);
                searchBoxBorder.Shadow = new Microsoft.UI.Xaml.Media.ThemeShadow();
                searchBoxBorder.Translation = new Vector3(0, 0, 4);

                var displayArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
                var workArea = displayArea.WorkArea;

                var scale = rootGrid.XamlRoot.RasterizationScale;
                var width = (int)(workArea.Width * 0.75);
                var height = (int)(workArea.Height * 0.80);
                var x = workArea.X + (workArea.Width - width) / 2;
                var y = workArea.Y + (workArea.Height - height) / 2;

                AppWindow.MoveAndResize(new RectInt32(x, y, width, height));
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
                From = -261,
                To = 0,
                Duration = duration,
                EasingFunction = ease
            };
            Storyboard.SetTarget(expandContentMargin, contentTransform);
            Storyboard.SetTargetProperty(expandContentMargin, "X");
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
                From = 0,
                To = -261,
                Duration = duration,
                EasingFunction = ease
            };
            Storyboard.SetTarget(collapseContentMargin, contentTransform);
            Storyboard.SetTargetProperty(collapseContentMargin, "X");
            _collapseStoryboard.Children.Add(collapseContentMargin);
            _collapseStoryboard.Completed += (_, _) =>
            {
                sidebarSeparator.Visibility = Visibility.Collapsed;
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
                sidebarSeparator.Visibility = Visibility.Visible;
                _expandStoryboard.Begin();
            }
            else
            {
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
    }
}
