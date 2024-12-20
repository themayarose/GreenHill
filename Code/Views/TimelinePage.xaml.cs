using System.Runtime.Serialization;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace GreenHill.Views;

public abstract partial class TimelineBasePage : BasePage<TimelineViewModel> {}

public partial class TimelinePage : TimelineBasePage {
    public override TimelineViewModel ViewModel { get; } = App.GetService<TimelineViewModel>();

    public Deferral? RefreshDeferral { get; set; }

    public TimelinePage() => InitializeComponent();

    public Lock LoadMoreLock { get; } = new ();
    public bool CanLoadMore { get; set; } = true;
    
    public void ListViewLoaded(object? sender, RoutedEventArgs __) {
        if (sender is not ListView listView) return;

        if (listView.FindDescendant("ScrollViewer") is ScrollViewer scroller) {
            scroller.ViewChanged += ListViewportChanged;
        }
    }

    public async void RefreshTimeline(RefreshContainer _, RefreshRequestedEventArgs args) {
        RefreshDeferral = args.GetDeferral();

        await ViewModel.RefreshTimelineCommand.ExecuteAsync(null);

        RefreshDeferral.Complete();
        RefreshDeferral.Dispose();
        RefreshDeferral = null;
    }

    public async void ListViewportChanged(object? sender, ScrollViewerViewChangedEventArgs args) {
        if (sender is not ScrollViewer scroller) return;

        var distanceToEnd = scroller.ExtentHeight - (scroller.VerticalOffset + scroller.ViewportHeight);

        if (distanceToEnd < 0.5 * scroller.ViewportHeight) {
            var canLoadMore = true;

            lock (LoadMoreLock) {
                if (!CanLoadMore) canLoadMore = false;
                else CanLoadMore = false;
            }

            if (canLoadMore) {
                if (!ViewModel.FilterAndAddPostsCommand.IsRunning) {
                    await ViewModel.LoadMoreItemsCommand.ExecuteAsync(null);
                }

                lock (LoadMoreLock) {
                    CanLoadMore = true;
                }
            }
        }
    }
}
