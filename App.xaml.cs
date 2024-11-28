using MSHost = Microsoft.Extensions.Hosting.Host;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

using GreenHill.Views;
using GreenHill.Services;

namespace GreenHill;

public partial class App : Application {
    public MainWindow? MainWindow { get; private set; }
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

    protected override void OnLaunched(LaunchActivatedEventArgs args) {
        MainWindow ??= new();
        MainWindow.Activate();
    }

}