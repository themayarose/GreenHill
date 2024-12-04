using FishyFlip.Models;
using GreenHill.Helpers;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Humanizer;
using Windows.Media.Playback;
using Windows.Media.Core;

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
    [ObservableProperty] private SkyConnection? connection;

    [NotifyPropertyChangedFor(nameof(Handle))]
    [NotifyPropertyChangedFor(nameof(Avatar))]
    [NotifyPropertyChangedFor(nameof(IsRepost))]
    [NotifyPropertyChangedFor(nameof(IsReply))]
    [NotifyPropertyChangedFor(nameof(OriginalPost))]
    [NotifyPropertyChangedFor(nameof(ParentReply))]
    [NotifyPropertyChangedFor(nameof(ShowThreadExtensions))]
    [NotifyPropertyChangedFor(nameof(ShowParentReply))]
    [NotifyPropertyChangedFor(nameof(ContainsText))]
    [NotifyPropertyChangedFor(nameof(HasReplies))]
    [NotifyPropertyChangedFor(nameof(HasReposts))]
    [NotifyPropertyChangedFor(nameof(HasLikes))]
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
    [NotifyPropertyChangedFor(nameof(LikedByViewer))]
    [NotifyPropertyChangedFor(nameof(LikeGlyph))]
    [NotifyPropertyChangedFor(nameof(LikeColor))]
    [NotifyPropertyChangedFor(nameof(RepostedByViewer))]
    [NotifyPropertyChangedFor(nameof(RepostColor))]
    [NotifyPropertyChangedRecipients]
    [ObservableProperty] private FeedViewPost? post;

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

    public bool HasReplies => (Post?.Post?.ReplyCount ?? 0) > 0;
    public bool HasReposts => (Post?.Post?.RepostCount ?? 0) > 0;
    public bool HasLikes => (Post?.Post?.LikeCount ?? 0) > 0;

    public bool LikedByViewer => Post?.Post?.Viewer?.Like is not null;

    public string LikeGlyph => LikedByViewer ? "\uEB52" : "\uEB51";

    [ObservableProperty] private IRelayCommand? displayProfileCommand;
    [ObservableProperty] private IRelayCommand? displayPostCommand;
    [ObservableProperty] private IRelayCommand? displayUserListCommand;
    [ObservableProperty] private IRelayCommand? linksCommand;

    [ObservableProperty] private bool showThreadLine = false;

    public SolidColorBrush? LikeColor => LikedByViewer ?
        App.Current.Resources["AccentTextFillColorPrimaryBrush"] as SolidColorBrush :
        App.Current.Resources["TextFillColorPrimaryBrush"] as SolidColorBrush ;

    public bool RepostedByViewer => Post?.Post?.Viewer?.Repost is not null;

    public SolidColorBrush? RepostColor => RepostedByViewer ?
        App.Current.Resources["AccentTextFillColorPrimaryBrush"] as SolidColorBrush :
        App.Current.Resources["TextFillColorPrimaryBrush"] as SolidColorBrush ;

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
        Post?.Post?.Embed is RecordWithMediaViewEmbed rmEmbed  && rmEmbed.Record is not null ? 
        new (rmEmbed.Record.Post, null, null, null) :
        null;

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
    [ObservableProperty] private bool isQuote = false;

    [ObservableProperty] private bool showReplyTo = false;

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

        PostView? post;

        try {
            post = (await Connection.GetPostsAsync([Post.Post.Uri])).First();

            if (post is not null) {
                try {
                    Post = Post with { Post = post };
                }
                catch (NullReferenceException) {}
            }
        }
        catch (InvalidOperationException) {}
    }
}