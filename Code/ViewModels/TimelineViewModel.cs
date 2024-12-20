using System.Collections;
using CommunityToolkit.WinUI.Collections;
using FishyFlip.Models;
using Ipfs;

namespace GreenHill.ViewModels;

public class PostDateComparer : IComparer {
    public int Compare(object? f, object? s) {
        if (f is not FeedViewPost first) return 0;
        if (s is not FeedViewPost second) return 0;

        var firstDate = first?.Reason?.IndexedAt ?? first?.Post?.IndexedAt;
        var secondDate = second?.Reason?.IndexedAt ?? second?.Post?.IndexedAt;

        if (firstDate is null || secondDate is null) return 0;
        if (firstDate == secondDate) return 0;
        else if (firstDate > secondDate) return 1;
        else return -1;
    }
}

public partial class TimelineViewModel : BasePageViewModel {
    [ObservableProperty] private partial string Cursor { get; set; } = string.Empty;

    public AdvancedCollectionView Posts { get; } = new (
        (List<FeedViewPost>) [], true
    );

    [RelayCommand]
    public async Task RefreshTimelineAsync() {
        if (Connection is null) return;

        var timeline = await Connection.GetTimelineAsync(string.Empty);

        if (!FilterAndAddPostsCommand.IsRunning) {
            await FilterAndAddPostsCommand.ExecuteAsync(timeline.Feed);
        }
    }

    [RelayCommand]
    public async Task InitTimelineAsync() {
        if (Connection is null) return;

        var timeline = await Connection.GetTimelineAsync(string.Empty);

        Cursor = timeline.Cursor ?? string.Empty;

        if (!FilterAndAddPostsCommand.IsRunning) {
            await FilterAndAddPostsCommand.ExecuteAsync(timeline.Feed);
        }
    }

    [RelayCommand]
    public void DeletePost(ATUri uri) {
        var toDelete = from post in Posts
            where post is FeedViewPost p && p.Post.Uri == uri
            select post;

        foreach (var post in toDelete) Posts.Remove(post);
    }

    [RelayCommand]
    public async Task LoadMoreItemsAsync() {
        if (Connection is null) return;

        var timeline = await Connection.GetTimelineAsync(Cursor);

        await App.EnqueueAsync(() => Cursor = timeline.Cursor ?? string.Empty);

        if (!FilterAndAddPostsCommand.IsRunning) {
            await FilterAndAddPostsCommand.ExecuteAsync(timeline.Feed);
        }
    }

    public override async Task UpdateWithRequestAsync(PageRequest request) {
        await base.UpdateWithRequestAsync(request);

        if (request is not PageRequest.TimelinePage sRequest) return;

        if (Posts.Count != 0) {
            await RefreshTimelineCommand.ExecuteAsync(null);
        }
        else {
            Cursor = sRequest.Cursor;

            Posts.Clear();

            Posts.SortDescriptions.Add(new (SortDirection.Descending, new PostDateComparer()));

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
            .Concat(Posts.Select(p => (p as FeedViewPost)!.Post?.Cid));

        filteredIncoming = filteredIncoming
            .Where(p => !filterCids.Contains(p.Post?.Cid) || p.Reason is not null)
            .Where(FollowingOnly)
            .DistinctBy(p => p.Post?.Cid)
        ;

        var cidsToRemove = GetParentCids(filteredIncoming)
            .Concat(GetRootCids(filteredIncoming))
            .Concat(filteredIncoming.Select(p => p.Post?.Cid))
        ;

        List<object> postsToRemove = [..
            from post in Posts
            where cidsToRemove.Contains((post as FeedViewPost)!.Post?.Cid)
            select post
        ];

        filteredIncoming = (List<FeedViewPost>) [.. filteredIncoming];

        await App.EnqueueAsync(() => {
            using (Posts.DeferRefresh()) {
                foreach (var post in postsToRemove) Posts.Remove(post);
                foreach (var post in filteredIncoming) Posts.Add(post);
            }
        });
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