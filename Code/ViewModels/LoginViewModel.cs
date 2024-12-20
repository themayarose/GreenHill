namespace GreenHill.ViewModels;

public partial class LoginViewModel : BaseViewModel {
    [ObservableProperty] public partial string UserName { get; set; } = string.Empty;
    [ObservableProperty] public partial string Password { get; set; } = string.Empty;
}