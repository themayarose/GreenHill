using GreenHill.Helpers;
using Microsoft.UI.Xaml.Controls;

namespace GreenHill.Views;

public abstract partial class ProfileBasePage : BasePage<ProfileViewModel> {}

[ObservableObject]
public partial class ProfilePage : ProfileBasePage {
    public override ProfileViewModel ViewModel { get; } = App.GetService<ProfileViewModel>();

    public ProfilePage() => InitializeComponent();
}