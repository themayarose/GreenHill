using FishyFlip.Models;
using GreenHill.Helpers;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Humanizer;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Messaging.Messages;

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

public partial class PostViewModel : BaseViewModel, IRecipient<PropertyChangedMessage<FeedViewPost?>> {
    [ObservableProperty] private SkyConnection? connection;

    [NotifyPropertyChangedFor(nameof(Handle))]
    [NotifyPropertyChangedFor(nameof(Avatar))]
    [NotifyPropertyChangedFor(nameof(IsRepost))]
    [NotifyPropertyChangedFor(nameof(ContainsText))]
    [NotifyPropertyChangedFor(nameof(HasEmbed))]
    [NotifyPropertyChangedFor(nameof(HasReplies))]
    [NotifyPropertyChangedFor(nameof(HasReposts))]
    [NotifyPropertyChangedFor(nameof(HasLikes))]
    [NotifyPropertyChangedFor(nameof(TimeAgo))]
    [NotifyPropertyChangedFor(nameof(PostTime))]
    [NotifyPropertyChangedFor(nameof(RepostTimeAgo))]
    [NotifyPropertyChangedFor(nameof(RepostTime))]
    [NotifyPropertyChangedFor(nameof(ControlPadding))]
    [NotifyPropertyChangedFor(nameof(EmbeddedPictures))]
    [NotifyPropertyChangedFor(nameof(HasEmbeddedPictures))]
    [NotifyPropertyChangedRecipients]
    [ObservableProperty] private FeedViewPost? post;

    public bool IsRepost => Post?.Reason is not null;

    public bool HasEmbed => Post?.Post?.Embed is not null;

    public string Handle => (Post?.Post?.Author?.Handle is null) ?
        string.Empty :
        $"@{Post.Post.Author.Handle}";

    public string TimeAgo => (Post?.Post?.Record?.CreatedAt is not null) ?
        Post.Post.Record.CreatedAt.Humanize() :
        string.Empty;
    
    public string PostTime => (Post?.Post?.Record?.CreatedAt is not null) ?
        Post.Post.Record.CreatedAt.Value.ToLocalTime().ToShortDateString() + " " + Post.Post.Record.CreatedAt.Value.ToLocalTime().ToLongTimeString() :
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

    public bool HasEmbeddedPictures => Post?.Post?.Embed is ImageViewEmbed;

    public List<EmbeddedPicture> EmbeddedPictures => Post?.Post?.Embed is ImageViewEmbed embed ?
        [..
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
            }
        ] :
        [];


    public bool ContainsText => (Post?.Post?.Record?.Text?.Trim() ?? string.Empty) != string.Empty;

    public bool HasReplies => (Post?.Post?.ReplyCount ?? 0) > 0;
    public bool HasReposts => (Post?.Post?.RepostCount ?? 0) > 0;
    public bool HasLikes => (Post?.Post?.LikeCount ?? 0) > 0;

    public Thickness ControlPadding => new (
        (Post?.Reply is not null) ? 32 : 0,
        0,
        0,
        0
    );

    [ObservableProperty] private IRelayCommand? displayProfileCommand;
    [ObservableProperty] private IRelayCommand? displayPostCommand;
    [ObservableProperty] private IRelayCommand? displayUserListCommand;
    [ObservableProperty] private IRelayCommand? linksCommand;

    public ObservableCollection<FeedViewPost> Replies { get; } = new();

    public PostViewModel() {
        Messenger.RegisterAll(this);
    }

    public void Receive(PropertyChangedMessage<FeedViewPost?> msg) {
        if (ReferenceEquals(msg?.Sender, this) && msg.NewValue is not null) {
            Replies.Clear();
        }
    }

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
    public async Task FetchRepliesAsync() {
        if (Connection is null) return;
        if (Post?.Post?.Uri is not ATUri uri) return;

        Replies.Clear();

        var thread = await Connection.GetPostThreadAsync(uri, 1);

        if (thread.Thread.Replies is not IEnumerable<ThreadView> views) return;

        var replies = 
            from view in views
            where view.Post is not null
            select new FeedViewPost(
                view.Post!,
                new (thread.Thread.Post, thread.Thread.Parent?.Post),
                null,
                null
            );
        
        foreach (var reply in replies) Replies.Add(reply);
    }

}