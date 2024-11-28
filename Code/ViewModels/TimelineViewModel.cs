using System.Collections.ObjectModel;
using FishyFlip.Models;
using Ipfs;

namespace GreenHill.ViewModels;

public partial class TimelineViewModel : BasePageViewModel {
    [ObservableProperty] private string cursor = string.Empty;

    public ObservableCollection<FeedViewPost> Posts = new ();

    public override async Task UpdateWithRequestAsync(PageRequest request) {
        await base.UpdateWithRequestAsync(request);

        if (request is PageRequest.TimelinePage sRequest) Cursor = sRequest.Cursor;

        if (Connection is null) return;

        var timeline = await Connection.GetTimelineAsync(Cursor);

        Cursor = timeline.Cursor ?? string.Empty;

        Posts.Clear();

        IEnumerable<Cid> repeatedRootPosts =
            from post in timeline.Feed
            where post.Reason is null
            where post.Reply?.Root?.Cid is not null
            select post.Reply!.Root!.Cid
        ;

        IEnumerable<Cid> repeatedParentPosts =
            from post in timeline.Feed
            where post.Reason is null
            where post.Reply?.Parent?.Cid is not null
            select post.Reply!.Parent!.Cid
        ;

        IEnumerable<Cid> repeatedPosts = repeatedRootPosts.Concat(repeatedParentPosts);

        foreach (var post in timeline.Feed) {
            if (repeatedPosts.Contains(post.Post?.Cid)) continue;

            Posts.Add(post);
        }
    }
}