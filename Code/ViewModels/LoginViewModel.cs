using GreenHill.Services;

namespace GreenHill.ViewModels;

public partial class LoginViewModel : BaseViewModel {
    [ObservableProperty] private string userName = string.Empty;
    [ObservableProperty] private string password = string.Empty;
}