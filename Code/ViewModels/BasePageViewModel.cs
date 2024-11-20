using FishyFlip.Models;
using GreenHill.Helpers;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace GreenHill.ViewModels;

public abstract partial class BasePageViewModel : BaseViewModel {
    [NotifyPropertyChangedFor(nameof(Parent))]
    [ObservableProperty]
    private WeakReference<BlueskyViewModel>? parentViewModel;

    public BlueskyViewModel? Parent => ParentViewModel?.TryGetTarget(out var vm) ?? false ?
        vm :
        null;

    [ObservableProperty] private SkyConnection? connection;

    [NotifyPropertyChangedFor(nameof(Handle))]
    [NotifyPropertyChangedFor(nameof(Avatar))]
    [NotifyPropertyChangedFor(nameof(Banner))]
    [NotifyPropertyChangedFor(nameof(DescriptionFacets))]
    [ObservableProperty]
    private FeedProfile? profile;

    public string Handle => (Profile?.Handle is not null) ?
        $"@{Profile.Handle}" :
        string.Empty;

    public ImageSource? Avatar => (Profile?.Avatar is not null) ?
        new BitmapImage() { UriSource = new (Profile.Avatar) } :
        null;

    public ImageSource? Banner => (Profile?.Banner is not null) ?
        new BitmapImage() { UriSource = new (Profile.Banner) } :
        null;

    public IEnumerable<Facet> DescriptionFacets {
        get {
            if (Profile?.Description is null) return [];
            if (Connection is null) return [];

            var task = Task.Run(
                async () => await Profile.Description.GetFacetsAsync(Connection)
            );

            task.Wait();

            return task.Result;
        }
    }

    public virtual async Task UpdateWithRequestAsync(PageRequest request) {
        Connection = request.Connection;
        ParentViewModel = request.ParentViewModel;

        await Task.CompletedTask;
    }

}