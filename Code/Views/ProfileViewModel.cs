using FishyFlip.Models;
using Humanizer;

namespace GreenHill.ViewModels;

public partial class ProfileViewModel : BasePageViewModel {
    [NotifyPropertyChangedFor(
        nameof(HasPinnedPost),
        nameof(FollowersCountText),
        nameof(FollowsCountText),
        nameof(PostsCountText)
    )]
    [ObservableProperty] public partial FeedViewPost? PinnedPost { get; set; }

    public bool HasPinnedPost => PinnedPost is not null;

    public string? FollowersCountText => Profile?.FollowersCount.ToMetric(decimals: 1);
    public string? FollowsCountText => Profile?.FollowsCount.ToMetric(decimals: 1);
    public string? PostsCountText => Profile?.PostsCount.ToMetric(decimals: 1);

    [RelayCommand]
    public void GoToUserPosts() {
        if (Parent is null) return;

        Parent.RequestNavigation(
            new PageRequest.ProfileFeedPage() { Profile = Profile }
        );
    }

    public override async Task UpdateWithRequestAsync(PageRequest request) {
        await base.UpdateWithRequestAsync(request);

        if (request is PageRequest.ProfilePage sRequest) Profile = sRequest.Profile;

        if (Connection is null) return;

        PinnedPost = null;

        if (Profile?.PinnedPost?.Uri is not null) {
            var list = await Connection.GetPostsAsync([Profile.PinnedPost.Uri]);

            if (list.FirstOrDefault(null as PostView) is PostView post) {
                PinnedPost = new (post, null, null, null);
            }
        }
    }
}