using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using GreenHill.Services;

namespace GreenHill.Views;

public abstract partial class BasePage<T> : Page, IBaseView<T> where T : BasePageViewModel {
    public abstract T ViewModel { get; }

    protected override async void OnNavigatedTo(NavigationEventArgs e) {
        base.OnNavigatedTo(e);

        if (e.Parameter is not PageRequest request) return;

        await ViewModel.UpdateWithRequestAsync(request);
    }
}
