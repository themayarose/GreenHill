namespace GreenHill.Views;

public abstract partial class TimelineBasePage : BasePage<TimelineViewModel> {}

[ObservableObject]
public partial class TimelinePage : TimelineBasePage {
    public override TimelineViewModel ViewModel { get; } = App.GetService<TimelineViewModel>();

    public TimelinePage() => InitializeComponent();
}
