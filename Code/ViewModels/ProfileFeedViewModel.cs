using System.Collections.ObjectModel;
using FishyFlip.Models;
using FishyFlip.Tools;
using Ipfs;

namespace GreenHill.ViewModels;


public partial class ProfileFeedViewModel : BasePageViewModel {
    public ObservableCollection<FeedViewPost> UserPosts = new ();

    [RelayCommand]
    public async Task UpdatePostsAsync() {
        if (Connection is null) return;

        if (Profile?.Did is not null) {
            var (feed, err) = await Connection.Protocol.Feed.GetAuthorFeedAsync(Profile.Did);

            if (feed is null) throw new InvalidDataException(err?.Detail?.Message ?? string.Empty);

            IEnumerable<Cid> repeatedRootPosts =
                from post in feed.Feed
                where post.Reason is null
                where post.Reply?.Root?.Cid is not null
                select post.Reply!.Root!.Cid
            ;

            IEnumerable<Cid> repeatedParentPosts =
                from post in feed.Feed
                where post.Reason is null
                where post.Reply?.Parent?.Cid is not null
                select post.Reply!.Parent!.Cid
            ;

            IEnumerable<Cid> repeatedPosts = repeatedRootPosts.Concat(repeatedParentPosts);

            foreach (var post in feed.Feed) {
                if (repeatedPosts.Contains(post.Post?.Cid)) continue;

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