using FishyFlip.Models;

namespace GreenHill.ViewModels;

public partial class ProfileViewModel : BasePageViewModel {
    [NotifyPropertyChangedFor(nameof(HasPinnedPost))]
    [ObservableProperty] private FeedViewPost? pinnedPost;

    public bool HasPinnedPost => PinnedPost is not null;

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