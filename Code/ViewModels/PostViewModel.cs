using FishyFlip.Models;
using FishyFlip.Tools;
using GreenHill.Helpers;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Humanizer;
using Windows.Media.Playback;
using Windows.Media.Core;
using Ipfs;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace GreenHill.ViewModels;

public record EmbeddedPicture {
    public ImageView? View { get; set; }
    public ImageSource? Fullsize { get; set; }
    public ImageSource? Thumb { get; set; }
    public int RowSpan { get; set; }
    public int ColumnSpan { get; set; }
    public int Row { get; set; }
    public int Column { get; set; }
}

public record EmbeddedExternal {
    public ImageSource? Thumb { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public Uri? Uri { get; set; }
}

public record EmbeddedVideo {
    public IMediaPlaybackSource? Source { get; set; }
    public AspectRatio? AspectRatio { get; set; }
    public ImageSource? Thumb { get; set; }
}

public partial class PostViewModel : BaseViewModel {
    [ObservableProperty] public partial SkyConnection? Connection { get; set; }

    [ObservableProperty] public partial GridLength GridRowHeight { get; set; } = new (.5, GridUnitType.Star);
    [ObservableProperty] public partial double GridMaxHeight { get; set; } = double.PositiveInfinity;
    [ObservableProperty] public partial double GridRowMaxHeight { get; set; } = double.PositiveInfinity;

    [NotifyPropertyChangedFor(nameof(Handle))]
    [NotifyPropertyChangedFor(nameof(Avatar))]
    [NotifyPropertyChangedFor(nameof(IsRepost))]
    [NotifyPropertyChangedFor(nameof(IsReply))]
    [NotifyPropertyChangedFor(nameof(OriginalPost))]
    [NotifyPropertyChangedFor(nameof(ParentReply))]
    [NotifyPropertyChangedFor(nameof(ShowThreadExtensions))]
    [NotifyPropertyChangedFor(nameof(ShowParentReply))]
    [NotifyPropertyChangedFor(nameof(ContainsText))]
    [NotifyPropertyChangedFor(nameof(TimeAgo))]
    [NotifyPropertyChangedFor(nameof(PostTime))]
    [NotifyPropertyChangedFor(nameof(RepostTimeAgo))]
    [NotifyPropertyChangedFor(nameof(RepostTime))]
    [NotifyPropertyChangedFor(nameof(HasEmbeddedPictures))]
    [NotifyPropertyChangedFor(nameof(EmbeddedPictures))]
    [NotifyPropertyChangedFor(nameof(HasEmbeddedQuote))]
    [NotifyPropertyChangedFor(nameof(EmbeddedQuote))]
    [NotifyPropertyChangedFor(nameof(HasExternalEmbed))]
    [NotifyPropertyChangedFor(nameof(ExternalEmbed))]
    [NotifyPropertyChangedFor(nameof(HasEmbeddedVideo))]
    [NotifyPropertyChangedFor(nameof(EmbeddedVideo))]
    [NotifyPropertyChangedFor(nameof(HasUserLabels))]
    [NotifyPropertyChangedFor(nameof(UserLabels))]
    [NotifyPropertyChangedFor(nameof(HasPostLabels))]
    [NotifyPropertyChangedFor(nameof(PostLabels))]
    [NotifyPropertyChangedRecipients]
    [ObservableProperty] public partial FeedViewPost? Post { get; set; }

    public bool IsRepost => Post?.Reason is not null && !IsQuote;

    public string Handle => (Post?.Post?.Author?.Handle is null) ?
        string.Empty :
        $"@{Post.Post.Author.Handle}";

    public string TimeAgo => (Post?.Post?.IndexedAt is not null) ?
        Post.Post.IndexedAt.Humanize() :
        string.Empty;
    
    public string PostTime => (Post?.Post?.IndexedAt is not null) ?
        Post.Post.IndexedAt.ToLocalTime().ToShortDateString() + " " + Post.Post.IndexedAt.ToLocalTime().ToLongTimeString() :
        string.Empty;

    public string RepostTimeAgo => (Post?.Reason?.IndexedAt is not null) ?
        Post.Reason.IndexedAt.Humanize() :
        string.Empty;

    public string RepostTime => (Post?.Reason?.IndexedAt is not null) ?
        Post.Reason.IndexedAt.Value.ToLocalTime().ToShortDateString() + " " + Post.Reason.IndexedAt.Value.ToLocalTime().ToLongTimeString() :
        string.Empty;

    public ImageSource? Avatar => (Post?.Post?.Author?.Avatar is not null) ?
        new BitmapImage() { UriSource = new (Post.Post.Author.Avatar) } :
        null;

    public bool ContainsText => (Post?.Post?.Record?.Text?.Trim() ?? string.Empty) != string.Empty;

    [NotifyPropertyChangedFor(nameof(HasReplies), nameof(ReplyCountText))]
    [ObservableProperty] public partial int ReplyCount { get; set; }

    [NotifyPropertyChangedFor(nameof(HasReposts), nameof(RepostCountText))]
    [ObservableProperty] public partial int RepostCount { get; set; }

    [NotifyPropertyChangedFor(nameof(HasLikes), nameof(LikeCountText))]
    [ObservableProperty] public partial int LikeCount { get; set; }

    public bool HasReplies => ReplyCount > 0;
    public bool HasReposts => RepostCount > 0;
    public bool HasLikes => LikeCount > 0;

    public string LikeCountText => LikeCount.ToMetric(decimals: 1);
    public string ReplyCountText => ReplyCount.ToMetric(decimals: 1);
    public string RepostCountText => RepostCount.ToMetric(decimals: 1);

    [NotifyPropertyChangedFor(nameof(LikedByViewer))]
    [NotifyPropertyChangedFor(nameof(LikeGlyph))]
    [NotifyPropertyChangedFor(nameof(LikeColor))]
    [ObservableProperty] public partial ATUri? ViewerLike { get; set; }

    public bool LikedByViewer => ViewerLike is not null;

    public string LikeGlyph => LikedByViewer ? "\uEB52" : "\uEB51";

    public SolidColorBrush? LikeColor => LikedByViewer ?
        App.Current.Resources["AccentTextFillColorPrimaryBrush"] as SolidColorBrush :
        App.Current.Resources["TextFillColorPrimaryBrush"] as SolidColorBrush ;

    [NotifyPropertyChangedFor(nameof(RepostedByViewer))]
    [NotifyPropertyChangedFor(nameof(RepostColor))]
    [NotifyPropertyChangedFor(nameof(RepostMenuText))]
    [ObservableProperty] public partial ATUri? ViewerRepost { get; set; }

    public bool RepostedByViewer => ViewerRepost is not null;

    public string RepostMenuText => RepostedByViewer ? "Undo repost" : "Repost";

    public SolidColorBrush? RepostColor => RepostedByViewer ?
        App.Current.Resources["AccentTextFillColorPrimaryBrush"] as SolidColorBrush :
        App.Current.Resources["TextFillColorPrimaryBrush"] as SolidColorBrush ;

    [ObservableProperty] public partial IRelayCommand? DisplayProfileCommand { get; set; }
    [ObservableProperty] public partial IRelayCommand? DisplayPostCommand { get; set; }
    [ObservableProperty] public partial IRelayCommand? DisplayUserListCommand { get; set; }
    [ObservableProperty] public partial IRelayCommand? DeleteCommand { get; set; }
    [ObservableProperty] public partial IRelayCommand? LinksCommand { get; set; }

    [ObservableProperty] public partial bool ShowThreadLine { get; set; } = false;

    public bool HasEmbeddedPictures => Post?.Post?.Embed is ImageViewEmbed ||
        Post?.Post?.Embed is RecordWithMediaViewEmbed embed && embed.Embed is ImageViewEmbed;

    public List<EmbeddedPicture> EmbeddedPictures => Post?.Post?.Embed is ImageViewEmbed embed ?
        [.. ConvertImages(embed) ] :
        (Post?.Post?.Embed is RecordWithMediaViewEmbed extEmbed && extEmbed.Embed is ImageViewEmbed innEmbed) ?
        [.. ConvertImages(innEmbed) ] :
        [];

    private IEnumerable<EmbeddedPicture> ConvertImages(ImageViewEmbed embed) =>
        from pair in embed.Images.WithIndex()
        let image = pair.Value
        let count = embed.Images.Length
        select new EmbeddedPicture() {
            View = image,
            Fullsize = new BitmapImage() { UriSource = new (image.Fullsize) },
            Thumb = new BitmapImage() { UriSource = new (image.Thumb) },
            RowSpan = count switch {
                1 or 2 => 2,
                3 => pair.Key == 0 ? 2 : 1,
                _ => 1
            },
            ColumnSpan = count switch {
                1 => 2,
                _ => 1,
            },
            Row = pair.Key switch {
                0 or 1 => 0,
                _ => 1
            },
            Column = pair.Key switch {
                0 => 0,
                2 => count == 3 ? 1 : 0,
                _ => 1
            }
        };

    public bool HasExternalEmbed => Post?.Post?.Embed is ExternalViewEmbed ||
        (Post?.Post?.Embed is RecordWithMediaViewEmbed embed && embed.Embed is ExternalViewEmbed)
    ;

    public EmbeddedExternal? ExternalEmbed => Post?.Post?.Embed is ExternalViewEmbed embed ?
        ConvertExternal(embed) :
        (Post?.Post?.Embed is RecordWithMediaViewEmbed extEmbed && extEmbed.Embed is ExternalViewEmbed innEmbed) ?
        ConvertExternal(innEmbed) :
        null;

    private EmbeddedExternal ConvertExternal(ExternalViewEmbed embed) =>
        new () {
            Thumb = embed.External?.Thumb is not null ?
                new BitmapImage() { UriSource = new (embed.External.Thumb) } :
                null,
            Title = embed.External?.Title,
            Description = embed.External?.Description,
            Uri = embed.External?.Uri is not null ?
                new (embed.External.Uri) :
                null
        };

    public bool HasEmbeddedVideo => Post?.Post?.Embed is VideoViewEmbed ||
        (Post?.Post?.Embed is RecordWithMediaViewEmbed embed && embed.Embed is VideoViewEmbed);

    public EmbeddedVideo? EmbeddedVideo => (Post?.Post?.Embed is VideoViewEmbed embed) ?
        ConvertVideo(embed) :
        (Post?.Post?.Embed is RecordWithMediaViewEmbed extEmbed && extEmbed.Embed is VideoViewEmbed innEmbed) ?
        ConvertVideo(innEmbed) :
        null;

    private EmbeddedVideo ConvertVideo(VideoViewEmbed embed) =>
        new () {
            Source = embed.Playlist is not null ? 
                MediaSource.CreateFromUri(new (embed.Playlist)) :
                null,
            AspectRatio = embed.AspectRatio,
            Thumb = embed.Thumbnail is not null ?
                new BitmapImage() { UriSource = new (embed.Thumbnail) } :
                null
        };

    public bool HasEmbeddedQuote => Post?.Post?.Embed is RecordViewEmbed or RecordWithMediaViewEmbed && !IsQuote;

    public FeedViewPost? EmbeddedQuote => Post?.Post?.Embed is RecordViewEmbed embed && embed.Post is not null ?
        new (embed.Post, null, null, null) :
        Post?.Post?.Embed is RecordWithMediaViewEmbed rmEmbed && rmEmbed.Record is not null ? 
        new (rmEmbed.Record.Post, null, null, null) :
        null;

    public bool HasUserLabels => false; //Post?.Post?.Author?.Labels.Any() ?? false;

    public IEnumerable<string> UserLabels => HasUserLabels ?
        from label in Post!.Post!.Author!.Labels
        select label.Val
    : [];

    public bool HasPostLabels => false; // Post?.Post?.Label?.Any() ?? false;

    public IEnumerable<string> PostLabels => HasPostLabels ?
        from label in Post!.Post!.Label
        select label.Val
    : [];

    public bool IsReply => Post?.Reply is not null && !IsQuote;

    public FeedViewPost? OriginalPost => Post?.Reply?.Root is not null ?
        new (Post.Reply.Root, null, null, null) :
        null;

    public FeedViewPost? ParentReply => Post?.Reply?.Parent is not null ?
        new (Post.Reply.Parent, null, null, null) :
        null;

    public bool ShowParentReply => Post?.Reason is null &&
        Post?.Reply?.Parent is not null &&
        Post?.Reply?.Root is not null &&
        Post.Reply.Root.Cid != Post.Reply.Parent.Cid;

    public bool ShowThreadExtensions => ShowParentReply &&
        Post?.Reply?.Parent?.Record?.Reply?.Parent?.Cid is not null &&
        Post.Reply.Parent.Record.Reply?.Parent.Cid != Post?.Reply?.Root?.Cid;

    [NotifyPropertyChangedFor(nameof(ShowInteractionButtons))]
    [NotifyPropertyChangedFor(nameof(ShowBigAvatar))]
    [NotifyPropertyChangedFor(nameof(IsReply))]
    [ObservableProperty] public partial bool IsQuote { get; set; } = false;

    [ObservableProperty] public partial bool ShowReplyTo { get; set; } = false;

    public bool ShowInteractionButtons => !IsQuote;

    public bool ShowBigAvatar => !IsQuote;

    [RelayCommand]
    public async Task RequestDisplayProfileAsync(ATDid did) {
        if (Connection is null) return;

        var profile = await Connection.GetProfileAsync(did);

        await RequestDisplayProfileWithProfileCommand.ExecuteAsync(profile);
    }

    [RelayCommand]
    public async Task RequestDisplayProfileWithProfileAsync(FeedProfile profile) {
        if (DisplayProfileCommand is IAsyncRelayCommand command) {
            await command.ExecuteAsync(profile);
        }
        else {
            DisplayProfileCommand?.Execute(profile);
        }
    }

    [RelayCommand]
    public async Task RequestDisplayPostAsync() {
        await Task.CompletedTask;

        if (DisplayPostCommand is IAsyncRelayCommand command) {
            await command.ExecuteAsync(null);
        }
        else {
            DisplayPostCommand?.Execute(null);
        }
    }

    [RelayCommand]
    public async Task RequestDisplayUserListAsync() {
        await Task.CompletedTask;

        if (DisplayUserListCommand is IAsyncRelayCommand command) {
            await command.ExecuteAsync(null);
        }
        else {
            DisplayUserListCommand?.Execute(null);
        }
    }

    [RelayCommand]
    public async Task UpdatePostAsync() {
        if (Connection is null) return;
        if (Post?.Post is null) return;

        OnPropertyChanged(nameof(TimeAgo));
        OnPropertyChanged(nameof(RepostTimeAgo));

        try {
            var posts = await Connection.GetPostsAsync([Post.Post.Uri]);

            if (posts is null || !posts.Any()) {
                if (DeleteCommand is IAsyncRelayCommand cmd) await cmd.ExecuteAsync(Post.Post.Uri);
                else DeleteCommand?.Execute(Post.Post.Uri);
            }
            else {
                UpdateModifiablesWithPost(posts.First());
            }
        }
        catch {}
    }

    [RelayCommand]
    public async Task ToggleLikeAsync() {
        if (Connection is null) return;

        if (!LikedByViewer) {
            if (Post?.Post.Cid is not Cid cid) return;
            if (Post.Post.Uri is not ATUri uri) return;

            var (like, err) = await Connection.Protocol.Repo.CreateLikeAsync(cid, uri);

            if (like is null) {
                throw new InvalidOperationException(err?.Detail?.Message ?? string.Empty);
            }

            ViewerLike = like.Uri;
            LikeCount++;
        }
        else {
            var (success, err) = await Connection.Protocol.Repo.DeleteLikeAsync(ViewerLike!.Rkey);

            if (success is null) {
                throw new InvalidOperationException(err?.Detail?.Message ?? string.Empty);
            }

            ViewerLike = null;
            LikeCount--;
        }
    }

    [RelayCommand]
    public async Task ToggleRepostAsync() {
        if (Connection is null) return;

        if (!RepostedByViewer) {
            if (Post?.Post.Cid is not Cid cid) return;
            if (Post.Post.Uri is not ATUri uri) return;

            var (repost, err) = await Connection.Protocol.Repo.CreateRepostAsync(cid, uri);

            if (repost is null) {
                throw new InvalidOperationException(err?.Detail?.Message ?? string.Empty);
            }

            ViewerRepost = repost.Uri;
            RepostCount++;
        }
        else {
            var (success, err) = await Connection.Protocol.Repo.DeleteRepostAsync(ViewerRepost!.Rkey);

            if (success is null) {
                throw new InvalidOperationException(err?.Detail?.Message ?? string.Empty);
            }

            ViewerRepost = null;
            RepostCount--;
        }
    }

    public void UpdateModifiablesWithPost(PostView post) {
        ViewerRepost = post.Viewer?.Repost;
        ViewerLike = post.Viewer?.Like;
        ReplyCount = post.ReplyCount;
        RepostCount = post.RepostCount;
        LikeCount = post.LikeCount;
    }
}