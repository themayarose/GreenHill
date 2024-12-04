using System.Collections.ObjectModel;
using FishyFlip.Models;
using Ipfs;

namespace GreenHill.ViewModels;

public partial class TimelineViewModel : BasePageViewModel {
    [ObservableProperty] private string cursor = string.Empty;

    public ObservableCollection<FeedViewPost> Posts { get; } = [];

    [RelayCommand]
    public async Task RefreshTimelineAsync() {
        if (Connection is null) return;

        var timeline = await Connection.GetTimelineAsync(string.Empty);

        await FilterAndAddPostsCommand.ExecuteAsync(timeline.Feed);
    }

    [RelayCommand]
    public async Task InitTimelineAsync() {
        if (Connection is null) return;

        var timeline = await Connection.GetTimelineAsync(string.Empty, limit: 100);

        Cursor = timeline.Cursor ?? string.Empty;

        await FilterAndAddPostsCommand.ExecuteAsync(timeline.Feed);
    }

    public override async Task UpdateWithRequestAsync(PageRequest request) {
        await base.UpdateWithRequestAsync(request);

        if (request is not PageRequest.TimelinePage sRequest) return;

        if (Posts.Any()) {
            await RefreshTimelineCommand.ExecuteAsync(null);
        }
        else {
            Cursor = sRequest.Cursor;

            Posts.Clear();

            await InitTimelineCommand.ExecuteAsync(null);
        }
    }

    [RelayCommand]
    public async Task FilterAndAddPostsAsync(IEnumerable<FeedViewPost> incomingPosts) {
        var (filteredIncoming, newRootCids) = incomingPosts.Aggregate(
            ((IEnumerable<FeedViewPost>) [], (IEnumerable<Cid>) []),
            UniqueByRootCid
        );

        var filterCids = GetParentCids(filteredIncoming)
            .Concat(newRootCids)
            .Concat(Posts.Select(p => p.Post?.Cid));

        filteredIncoming = filteredIncoming
            .Where(p => !filterCids.Contains(p.Post?.Cid))
            .Where(FollowingOnly)
            .DistinctBy(p => p.Post?.Cid);
        ;

        if (filteredIncoming.Any()) {
            if (Posts.Any()) {
                var cidsToRemove = GetParentCids(filteredIncoming).Concat(GetRootCids(filteredIncoming));
                List<FeedViewPost> postsToRemove = [..
                    from post in Posts
                    where cidsToRemove.Contains(post.Post?.Cid)
                    select post
                ];

                foreach (var post in postsToRemove) Posts.Remove(post);

                var lastPostsDate = Posts.Last()?.Reason?.IndexedAt ?? Posts.Last()?.Post?.IndexedAt;
                var firstIncomingDate = filteredIncoming.First()?.Reason?.IndexedAt ?? filteredIncoming.First()?.Post?.IndexedAt;
                
                if (lastPostsDate >= firstIncomingDate) {
                    foreach (var post in filteredIncoming) Posts.Add(post);
                }
                else {
                    foreach (var post in filteredIncoming.Reverse()) Posts.Insert(0, post);
                }
            }
            else {
                foreach (var post in filteredIncoming) Posts.Add(post);
            }


        }

        await Task.CompletedTask;
    }

    private bool FollowingOnly(FeedViewPost post) =>
        post.Reply is null || (
            post.Reply.Parent?.Author?.Viewer is Viewer parentViewer &&
            post.Reply.Root?.Author?.Viewer is Viewer rootViewer &&
            parentViewer.Following is not null &&
            rootViewer.Following is not null
        );


    private (IEnumerable<FeedViewPost>, IEnumerable<Cid>) UniqueByRootCid((IEnumerable<FeedViewPost>, IEnumerable<Cid>) acc, FeedViewPost item) {
        if (item.Reason is null && item.Reply?.Root?.Cid is Cid root) {
            if (acc.Item2.Contains(root)) return acc;

            acc.Item1 = acc.Item1.Concat([item]);
            acc.Item2 = acc.Item2.Concat([root]);
        }
        else acc.Item1 = acc.Item1.Concat([item]);
        
        return acc;
    }

    private IEnumerable<Cid> GetParentCids(IEnumerable<FeedViewPost> feed) =>
        from post in feed
        where post.Reason is null
        where post.Reply?.Parent?.Cid is not null
        select post.Reply!.Parent!.Cid
        ;

    private IEnumerable<Cid> GetRootCids(IEnumerable<FeedViewPost> feed) =>
        from post in feed
        where post.Reason is null
        where post.Reply?.Root?.Cid is not null
        select post.Reply!.Root!.Cid
        ;
}