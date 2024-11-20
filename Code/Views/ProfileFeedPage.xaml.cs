namespace GreenHill.Views;

public abstract partial class ProfileFeedBasePage : BasePage<ProfileFeedViewModel> {}

[ObservableObject]
public partial class ProfileFeedPage : ProfileFeedBasePage {
    public override ProfileFeedViewModel ViewModel { get; } = App.GetService<ProfileFeedViewModel>();

    public ProfileFeedPage() => InitializeComponent();
}