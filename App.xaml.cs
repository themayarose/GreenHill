using MSHost = Microsoft.Extensions.Hosting.Host;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

using GreenHill.Views;
using GreenHill.Services;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;

namespace GreenHill;

public partial class App : Application {
    public static MainWindow MainWindow { get; } = new ();
    public IHost Host { get; }

    public App() {
        InitializeComponent();

        Host = MSHost
            .CreateDefaultBuilder()
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureServices(Configure)
            .Build();
    }

    private void Configure(HostBuilderContext ctx, IServiceCollection services) {
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<BlueskyViewModel>();
        services.AddTransient<PostViewModel>();
        services.AddTransient<ProfileViewModel>();
        services.AddTransient<ProfileFeedViewModel>();
        services.AddTransient<TimelineViewModel>();

        services.AddSingleton<IConnectionService, ConnectionService>();
        services.AddSingleton<ICredentialService, CredentialService>();
        services.AddSingleton<IMessenger, WeakReferenceMessenger>();

    }

    public static T GetService<T>() where T : class {
        var app = Current as App;
        var maybeService = app?.Host.Services.GetService<T>();

        if (maybeService is T service) {
            return service;
        }
        else {
            throw new ArgumentException($"Unregistered service ({typeof(T)}).");
        }
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args) => MainWindow.Activate();

    public static async Task EnqueueAsync(Action action, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal) {
        await MainWindow.DispatcherQueue.EnqueueAsync(action, priority);
    }

    public static async Task EnqueueAsync(Func<Task> task, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal) {
        await MainWindow.DispatcherQueue.EnqueueAsync(task, priority);
    }

    public static async Task<T> EnqueueAsync<T>(Func<T> func, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal) {
        return await MainWindow.DispatcherQueue.EnqueueAsync(func, priority);
    }

    public static async Task<T> EnqueueAsync<T>(Func<Task<T>> task, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal) {
        return await MainWindow.DispatcherQueue.EnqueueAsync(task, priority);
    }

}