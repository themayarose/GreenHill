using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace GreenHill.Views;

public abstract partial class TimelineBasePage : BasePage<TimelineViewModel> {}

[ObservableObject]
public partial class TimelinePage : TimelineBasePage {
    public override TimelineViewModel ViewModel { get; } = App.GetService<TimelineViewModel>();

    public Deferral? RefreshDeferral { get; set; }



    public TimelinePage() => InitializeComponent();

    public async void RefreshTimeline(RefreshContainer _, RefreshRequestedEventArgs args) {
        // while (RefreshDeferral is not null) { }

        RefreshDeferral = args.GetDeferral();

        await ViewModel.RefreshTimelineCommand.ExecuteAsync(null);

        RefreshDeferral.Complete();
        RefreshDeferral.Dispose();
        RefreshDeferral = null;
    }
}
