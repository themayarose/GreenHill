using GreenHill.Services;
using Microsoft.UI.Xaml.Controls;

namespace GreenHill.Views;

public partial class LoginDialog : ContentDialog, IBaseView<LoginViewModel> {
    public LoginViewModel ViewModel { get; } = App.GetService<LoginViewModel>();

    public LoginDialog() {
        InitializeComponent();
    }

}