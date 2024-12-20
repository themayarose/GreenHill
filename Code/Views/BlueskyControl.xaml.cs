using FishyFlip.Models;
using GreenHill.Services;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using GreenHill.Helpers;

namespace GreenHill.Views;

public partial class BlueskyControl : UserControl, IBaseView<BlueskyViewModel> {
    public BlueskyViewModel ViewModel { get; } = App.GetService<BlueskyViewModel>();

    private string? EmptyNavState { get; set; }

    public BlueskyControl() {
        InitializeComponent();

        ViewModel.NavigationRequested += NavigationRequested;
        ViewModel.GoBackRequested += GoBackRequested;
    }

    ~BlueskyControl() {
        ViewModel.NavigationRequested -= NavigationRequested;
        ViewModel.GoBackRequested -= GoBackRequested;
    }

    public void NavigationRequested(object? sender, NavigationRequestedEventArgs args) {
        if (args.Request?.Target is not Type target) return;

        NavigationTransitionInfo info = (EmptyNavState is null) ?
            new SuppressNavigationTransitionInfo() :
            new SlideNavigationTransitionInfo() {
                Effect = SlideNavigationTransitionEffect.FromRight
            };

        EmptyNavState ??= ProfileControlNav.GetNavigationState();

        ProfileControlNav.Navigate(
            target,
            args.Request,
            info
        );

        ViewModel.CanGoBack = ProfileControlNav.CanGoBack;
    }

    public void GoBackRequested(object? _, EventArgs __) {
        ProfileControlNav.GoBack(
            new SlideNavigationTransitionInfo() {
                Effect = SlideNavigationTransitionEffect.FromRight
            }
        );

        ViewModel.CanGoBack = ProfileControlNav.CanGoBack;
    }

    public void FlyoutClosed() {
        if (EmptyNavState is not null) ProfileControlNav.SetNavigationState(EmptyNavState);
    }

    public static readonly DependencyProperty ConnectionProperty =
        DependencyProperty.Register(
            "Connection",
            typeof(SkyConnection),
            typeof(BlueskyControl),
            new (defaultValue: null, propertyChangedCallback: (s, a) => {
                if (s is not BlueskyControl self) return;

                self.ViewModel.Connection = (SkyConnection?) a.NewValue;
            })
        );

    public SkyConnection? Connection {
        get => (SkyConnection?) GetValue(ConnectionProperty);
        set => SetValue(ConnectionProperty, value);
    }

    public static readonly DependencyProperty RequestProperty =
        DependencyProperty.Register(
            "Request",
            typeof(PageRequest),
            typeof(BlueskyControl),
            new (defaultValue: null, propertyChangedCallback: (s, a) => {
                if (s is not BlueskyControl self) return;
                if (a.NewValue is not PageRequest request) return;

                self.ViewModel.RequestNavigation(request);
            })
        );

    public PageRequest? Request {
        get => (PageRequest?) GetValue(RequestProperty);
        set => SetValue(RequestProperty, value);
    }
}