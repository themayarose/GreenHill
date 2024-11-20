using System.Collections.ObjectModel;
using FishyFlip.Models;
using FishyFlip.Tools;

namespace GreenHill.ViewModels;


public partial class ProfileFeedViewModel : BasePageViewModel {
    public ObservableCollection<FeedViewPost> UserPosts = new ();

    [RelayCommand]
    public async Task UpdatePostsAsync() {
        if (Connection is null) return;

        if (Profile?.Did is not null) {
            var (feed, err) = await Connection.Protocol.Feed.GetAuthorFeedAsync(Profile.Did);

            if (feed is null) throw new InvalidDataException(err?.Detail?.Message ?? string.Empty);

            foreach (var post in feed.Feed) {
                UserPosts.Add(post);
            }
        }
    }

    public override async Task UpdateWithRequestAsync(PageRequest request) {
        await base.UpdateWithRequestAsync(request);

        if (request is PageRequest.ProfileFeedPage sRequest) {
            Profile = sRequest.Profile;

            UserPosts.Clear();
            
            await UpdatePostsCommand.ExecuteAsync(null);
        }
    }
}