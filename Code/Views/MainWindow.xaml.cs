using Microsoft.UI.Xaml.Controls;

using GreenHill.Services;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Input;
using GreenHill.Helpers;

namespace GreenHill.Views;

public sealed partial class MainWindow : Window, IBaseView<MainWindowViewModel> {
    public MainWindowViewModel ViewModel { get; } =
        App.GetService<MainWindowViewModel>();

    private SpringVector3NaturalMotionAnimation? PersonPictureAnimation { get; set; }

    public MainWindow() {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(TitleBar);

        Title = "GreenHill for BlueSky";

        Activated += ActivationStateChanged;
        SizeChanged += MainWindowSizeChanged;

        if (Content is FrameworkElement root) {
            root.Loaded += ContentLoaded;
        }

        TitleBar.Loaded += FixDragArea;
        TitleBar.SizeChanged += FixDragArea;
        SearchBox.SizeChanged += FixDragArea;
        LoginArea.SizeChanged += FixDragArea;

        NavView.DisplayModeChanged += (s, a) => {
            TitleBar.Margin = TitleBar.Margin with {
                Left = s.CompactPaneLength * (
                    s.DisplayMode == NavigationViewDisplayMode.Minimal ? 2 : 1
                )
            };
        };

    }

    private void FixDragArea(object sender, RoutedEventArgs args) =>
        App.MainWindow.SetDragRectangles(TitleBar, SearchBox, LoginArea);

    private double SearchBoxWidthRatio { get; } = 0.4;
    private double NavBarWidthRatio { get; } = 0.225;

    public async void ContentLoaded(object s, RoutedEventArgs args) {
        if (s is not FrameworkElement root) return;

        TitleBarHelper.UpdateTitleBar(root.RequestedTheme);

        ViewModel.SearchBox = SearchBox;

        var compositor = App.MainWindow.Compositor;

        if (compositor is not null) {
            PersonPictureAnimation = compositor.CreateSpringVector3Animation();
            PersonPictureAnimation.Target = "Scale";
            PersonPictureAnimation.Period = TimeSpan.FromMilliseconds(25);
        }

        SearchBox.CenterPoint = new (
                SearchBox.ActualSize.X / 2f,
                SearchBox.ActualSize.Y / 2f,
                1f
        );

        NavView.OpenPaneLength = root.ActualSize.X * NavBarWidthRatio;
        SearchBox.Width = root.ActualSize.X * SearchBoxWidthRatio;

        await ViewModel.InitCommand.ExecuteAsync(root);
    }


    public void MainWindowSizeChanged(object _, WindowSizeChangedEventArgs args) {
        var windowWidth = args.Size.Width;

        NavView.OpenPaneLength = windowWidth * NavBarWidthRatio;
        SearchBox.Width = windowWidth * SearchBoxWidthRatio;
    }

    public void PersonPicturePointerEntered(object sender, PointerRoutedEventArgs _) {
        if (PersonPictureAnimation is null) return;
        if (sender is not UIElement picture) return;

        PersonPictureAnimation.FinalValue = new (1.1f);

        picture.CenterPoint = new (
            picture.ActualSize.X / 2f,
            picture.ActualSize.Y / 2f,
            1f
        );

        picture.StartAnimation(PersonPictureAnimation);
    }

    public void PersonPicturePointerExited(object sender, PointerRoutedEventArgs _) {
        if (PersonPictureAnimation is null) return;
        if (sender is not UIElement picture) return;

        PersonPictureAnimation.FinalValue = new (1.0f);

        picture.CenterPoint = new (
            picture.ActualSize.X / 2f,
            picture.ActualSize.Y / 2f,
            1f
        );

        picture.StartAnimation(PersonPictureAnimation);
    }

    public void PersonPicturePointerPressed(object sender, PointerRoutedEventArgs _) {
        if (PersonPictureAnimation is null) return;
        if (sender is not UIElement picture) return;

        PersonPictureAnimation.FinalValue = new (.9f);

        picture.CenterPoint = new (
            picture.ActualSize.X / 2f,
            picture.ActualSize.Y / 2f,
            1f
        );

        picture.StartAnimation(PersonPictureAnimation);
    }

    public void PersonPicturePointerReleased(object sender, PointerRoutedEventArgs _) {
        if (PersonPictureAnimation is null) return;
        if (sender is not UIElement picture) return;

        PersonPictureAnimation.FinalValue = new (1.0f);

        picture.CenterPoint = new (
            picture.ActualSize.X / 2f,
            picture.ActualSize.Y / 2f,
            1f
        );

        picture.StartAnimation(PersonPictureAnimation);

        if (picture.GetValue(FlyoutBase.AttachedFlyoutProperty) is not Flyout flyout) return;

        flyout.Content = new BlueskyControl() {
            Connection = ViewModel.Connection,
            Request = new PageRequest.ProfilePage() { Profile = ViewModel.CurrentProfile },
            Width = 450
        };

        flyout.ShowAt(sender as FrameworkElement, new() {
            ShowMode = FlyoutShowMode.Standard,
            Placement = FlyoutPlacementMode.Bottom
        });
    }

    private void ActivationStateChanged(object sender, WindowActivatedEventArgs args) {
        ViewModel.IsActive = args.WindowActivationState != WindowActivationState.Deactivated;
    }

    private void ProfileFlyoutClosing(object _, FlyoutBaseClosingEventArgs args) {
        args.Cancel = !ViewModel.IsActive;
    }

    private void ProfileFlyoutClosed(object sender, object _) {
        if (sender is not Flyout f) return;
        if (f.Content is not BlueskyControl control) return;

        ViewModel.UserQuery = string.Empty;

        control.FlyoutClosed();
    }

}