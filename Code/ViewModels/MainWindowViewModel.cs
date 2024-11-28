using FishyFlip.Models;
using GreenHill.Services;
using Windows.Security.Credentials;
using Microsoft.UI.Xaml.Controls;
using GreenHill.Views;
using GreenHill.Helpers;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Xaml.Controls.Primitives;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace GreenHill.ViewModels;

public partial class MainWindowViewModel(IConnectionService conn, ICredentialService cred) : BaseViewModel, IRecipient<PropertyChangedMessage<SkyConnection>> {
    public IConnectionService ConnManager { get; } = conn;
    public ICredentialService Credentials { get; } = cred;

    [ObservableProperty] private string userQuery = string.Empty;

    [ObservableProperty] private PageRequest? startOfTimelineRequest;

    [NotifyPropertyChangedFor(nameof(InputFacets))]
    [ObservableProperty]
    private string input = string.Empty;

    public IEnumerable<Facet> InputFacets {
        get {
            if (Connection is null) return [];

            var task = Task.Run(async () => await Input.GetFacetsAsync(Connection));

            task.Wait();

            return task.Result;
        }
    }

    [NotifyPropertyChangedFor(nameof(Connected))]
    [NotifyPropertyChangedRecipients]
    [ObservableProperty]
    private SkyConnection? connection = null;

    public bool Connected => Connection is not null;

    [NotifyPropertyChangedFor(nameof(Handle))]
    [ObservableProperty]
    private FeedProfile? currentProfile = null;

    public string Handle => (CurrentProfile?.Handle is null) ?
        string.Empty :
        $"@{CurrentProfile.Handle}";

    [ObservableProperty] private AutoSuggestBox? searchBox;

    [RelayCommand]
    public async Task InitAsync(FrameworkElement root) {
        Messenger.RegisterAll(this);

        PasswordCredential? credential = await Credentials.GetInitialCredentialAsync();

        if (credential is null) {
            LoginDialog dialog = new() {
                XamlRoot = root.XamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                Title = "Login to BlueSky",
                PrimaryButtonText = "Login",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary) {
                credential = await Credentials.CreateNewCredentialAsync(
                    dialog.ViewModel.UserName,
                    dialog.ViewModel.Password
                );
            }
            else Application.Current.Exit();
        }

        if (credential is not null) {
            try {
                Connection = await ConnManager.GetOrCreateAsync(credential);

            }
            catch (InvalidOperationException) {
                await Credentials.DeleteCredentialAsync(credential);
                Application.Current.Exit();
            }
        }
    }

    public async void Receive(PropertyChangedMessage<SkyConnection> msg) {
        if (msg.Sender is not MainWindowViewModel vm || !ReferenceEquals(vm, this)) return;

        if (Connection is null || Connection.Session is null) {
            await ClearConnection();
            return;
        }

        CurrentProfile = await Connection.GetProfileAsync(Connection.Session.Did);

        StartOfTimelineRequest = new PageRequest.TimelinePage() {
            Cursor = string.Empty
        };

        // await ShowIdolsAsync();
    }

    public async void SearchHandle(AutoSuggestBox s, AutoSuggestBoxQuerySubmittedEventArgs _) {
        if (s is not AutoSuggestBox asb) return;

        await PerformSearchCommand.ExecuteAsync(asb);
    }

    [RelayCommand]
    public async Task InputHyperlinkAsync(HyperlinkClickedArgs args) {
        if (args.Facet?.Features?.FirstOrDefault(null as FacetFeature) is not FacetFeature feat) return;

        if (feat.Type == RichTextExtensions.MentionType) {
            UserQuery = args.Text ?? string.Empty;
            await PerformSearchCommand.ExecuteAsync(SearchBox);
        }
    }


    [RelayCommand]
    public async Task PerformSearchAsync() {
        if (Connection is null) return;
        if (UserQuery == string.Empty) return;
        if (SearchBox is not AutoSuggestBox asb) return;

        if (FlyoutBase.GetAttachedFlyout(asb) is not Flyout flyout) return;

        var shouldShow = false;
 
        if (RichTextExtensions.MentionRegex().IsMatch(UserQuery)) {
            shouldShow = true;
            var profile = await Connection.GetProfileAsync(UserQuery);

            flyout.Content = new BlueskyControl() {
                Connection = Connection,
                Request = new PageRequest.ProfilePage() { Profile = profile },
                Width = 450
            };
        }

        if (shouldShow) {
            flyout.ShowAt(asb, new() {
                ShowMode = FlyoutShowMode.Standard,
                Placement = FlyoutPlacementMode.Bottom
            });
        }
    }

    public async Task ClearConnection() {
        CurrentProfile = null;

        await Task.CompletedTask;
    }


    public async Task ShowIdolsAsync() {
        if (Connection?.Session?.Did is not ATDid did) return;

        try {
            var followers = await Connection.GetAllFollowersAsync(did);
            var follows = await Connection.GetAllFollowsAsync(did);

            List<FeedProfile> mutualsFp = [.. from fw in followers
                where follows.Aggregate(false, (acc, fp) => {
                    if (acc) return true;

                    return fw.Did.Handler == fp.Did.Handler;
                })
                select fw];

            List<string> fans = [.. from fw in followers
                where mutualsFp.Aggregate(true, (acc, fp) => {
                    if (!acc) return false;

                    return fw.Did.Handler != fp.Did.Handler;
                })
                select $"{fw.Handle} - {fw.DisplayName}"];

            List<string> idols = [.. from fw in follows
                where mutualsFp.Aggregate(true, (acc, fp) => {
                    if (!acc) return false;

                    return fw.Did.Handler != fp.Did.Handler;
                })
                select $"{fw.DisplayName} - @{fw.Handle}\nhttps://bsky.app/profile/{fw.Handle}\n"];

            List<string> mutuals = [.. from fw in mutualsFp
                select $"{fw.Handle} - {fw.DisplayName}"];

            Input = $"Idols ({idols.Count}):\n\n{string.Join("\n", idols)}";

        }
        catch (Exception e) when (e is InvalidOperationException or InvalidDataException) {
        }
    }

}
