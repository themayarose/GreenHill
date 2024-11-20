using FishyFlip.Models;
using GreenHill.Helpers;
using Microsoft.UI.Xaml.Controls;

namespace GreenHill.ViewModels;

public abstract record PageRequest {
    public abstract Type Target { get; }

    // These properties are set on RequestNavigation and don't need to be
    // set by the creator of the view request.
    public WeakReference<BlueskyViewModel>? ParentViewModel { get; set; }
    public SkyConnection? Connection { get; set; }

    public record ProfilePage : PageRequest {
        public override Type Target { get; } = typeof(Views.ProfilePage);
        public FeedProfile? Profile { get; set; }
    }

    public record ProfileFeedPage : PageRequest {
        public override Type Target { get; } = typeof(Views.ProfileFeedPage);
        public FeedProfile? Profile { get; set; }
    }
}

public record NavigationRequestedEventArgs(PageRequest request) {
    public PageRequest Request { get; } = request;
}

public partial class BlueskyViewModel : BaseViewModel {
    [ObservableProperty] private SkyConnection? connection;
    [ObservableProperty] private bool canGoBack = false;

    public event EventHandler<NavigationRequestedEventArgs>? NavigationRequested;
    public event EventHandler? GoBackRequested;

    [RelayCommand]
    public async Task HyperlinkClickedAsync(HyperlinkClickedArgs args) {
        if (Connection is null) return;

        List<FacetFeature> feats = [..
            from feat in args.Facet?.Features ?? []
            where feat.Type is RichTextExtensions.MentionType or
                RichTextExtensions.LinkType or
                RichTextExtensions.HashtagType
            select feat
        ];

        var feature = feats.FirstOrDefault(null as FacetFeature);

        if (feature is null) return;

        if (feature?.Type is RichTextExtensions.MentionType) {
            var did = feature.Did;

            if (did is null) {
                var handle = args.Text?[1..];

                if (handle is null) return;

                did = await Connection.GetDidAsync(handle);
            }

            if (did is not null) await DisplayProfileWithDidCommand.ExecuteAsync(did);
        }
        else if (feature?.Type is RichTextExtensions.HashtagType) {

        }
    }
    
    [RelayCommand]
    public async Task DisplayProfileWithDidAsync(ATDid did) {
        if (Connection is null) return;

        var profile = await Connection.GetProfileAsync(did);

        DisplayProfileCommand.Execute(profile);
    }

    [RelayCommand]
    public void DisplayProfile(FeedProfile profile) {
        (App.Current as App)!.MainWindow?.DispatcherQueue.TryEnqueue(() => {
            RequestNavigation(
                new PageRequest.ProfilePage() { Profile = profile }
            );
        });
    }

    public void RequestNavigation(PageRequest request) {
        NavigationRequested?.Invoke(this, new (
            request with {
                ParentViewModel = new (this),
                Connection = Connection
            }
        ));
    }

    [RelayCommand]
    public void RequestGoBack() {
        GoBackRequested?.Invoke(this, EventArgs.Empty);
    }
}