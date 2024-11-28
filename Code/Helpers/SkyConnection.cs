using FishyFlip;
using FishyFlip.Models;
using FishyFlip.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using Windows.Security.Credentials;

namespace GreenHill.Helpers;

public class SkyConnection {
    public ATProtocol Protocol { get; }
    public Session? Session { get; private set; }
    public ILogger DebugLog { get; }

    public static Dictionary<string, SkyConnection> Connections { get; } = new();

    private SkyConnection() {
        DebugLog = new DebugLoggerProvider().CreateLogger("FishyFlip");

        Protocol = new ATProtocolBuilder()
            .EnableAutoRenewSession(true)
            .WithLogger(DebugLog)
            .Build();
    }

    public static async Task<SkyConnection> GetOrCreateAsync(PasswordCredential credential, CancellationToken? ct = default) {
        SkyConnection connection;

        if (Connections.TryGetValue(credential.UserName, out var conn)) {
            connection = conn;
        }
        else {
            connection = new();

            await connection.LoginAsync(credential, ct);

            Connections.Add(credential.UserName, connection);
        }

        return connection;
    }

    public async Task LoginAsync(PasswordCredential credential, CancellationToken? ct = null) {
        credential.RetrievePassword();

        var session = await Protocol.AuthenticateWithPasswordAsync(
            credential.UserName,
            credential.Password,
            ct ?? CancellationToken.None
        );

        Session = session;
    }
    
    public async Task<FeedProfile> GetProfileAsync(ATDid did) {
        var (profile, error) = await Protocol.Actor.GetProfileAsync(did);

        if (profile is null) throw new ArgumentException(error?.Detail?.Message);

        return profile;
    }
    
    public async Task<ATDid?> GetDidAsync(string handle) {
        if (handle.StartsWith('@')) handle = handle.TrimStart('@');
        if (!handle.Contains('.')) handle = $"{handle}.bsky.social";

        var (actor, _) = await Protocol.Identity.ResolveHandleAsync(new (handle));

        return actor?.Did;
    }

    public async Task<FeedProfile> GetProfileAsync(string handle) {
        var did = await GetDidAsync(handle) ?? throw new ArgumentException($"Can't resolve handle @{handle}.");

        return await GetProfileAsync(did);
    }

    public async Task<IEnumerable<FeedProfile>> GetAllFollowersAsync(ATDid subject) {
        List<FeedProfile> followers = [];
        string? cursor = string.Empty;

        while (cursor is not null) {
            var (fs, err_) = await Protocol.Graph.GetFollowersAsync(subject, cursor: cursor);

            if (fs is null) {
                throw new InvalidDataException(err_?.Detail?.Message ?? string.Empty);
            }

            followers.AddRange(fs.Followers ?? []);

            cursor = fs.Cursor;
        }

        return followers;
    }



    public async Task<IEnumerable<FeedProfile>> GetAllFollowsAsync(ATDid subject) {
        List<FeedProfile> follows = [];
        string? cursor = string.Empty;

        while (cursor is not null) {
            var (fs, err_) = await Protocol.Graph.GetFollowsAsync(subject, cursor: cursor);

            if (fs is null) {
                throw new InvalidDataException(err_?.Detail?.Message ?? string.Empty);
            }

            follows.AddRange(fs.Follows ?? []);

            cursor = fs.Cursor;
        }

        return follows;
    }

    public async Task<IEnumerable<PostView>> GetPostsAsync(IEnumerable<ATUri> uriList) {
        var (posts, err) = await Protocol.Feed.GetPostsAsync(uriList);

        if (posts is null) {
            throw new InvalidDataException(err?.Detail?.Message ?? string.Empty);
        }

        return posts.Posts;
    }

    public async Task<ThreadPostViewFeed> GetPostThreadAsync(ATUri uri, int depth) {
        var (thread, err) = await Protocol.Feed.GetPostThreadAsync(uri, depth);

        if (thread is null) {
            throw new InvalidDataException(err?.Detail?.Message ?? string.Empty);
        }

        return thread;
    }

    public async Task<Timeline> GetTimelineAsync(string? cursor = null) {
        var (timeline, err) = await Protocol.Feed.GetTimelineAsync(cursor: cursor);

        if (timeline is null) {
            throw new InvalidDataException(err?.Detail?.Message ?? string.Empty);
        }

        return timeline;
    }
}