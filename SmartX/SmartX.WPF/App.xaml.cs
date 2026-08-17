using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartX.WPF.Authentication;
using SmartX.WPF.Data;
using SmartX.WPF.Repositories.Local;
using SmartX.WPF.Services.Api;
using SmartX.WPF.Services.Sync;
using SmartX.WPF.Views.Pages;
using SmartX.WPF.Views.Pages.History;
using SmartX.WPF.Views.Pages.Home;
using SmartX.WPF.Views.Pages.Sensor;
using SmartX.WPF.Views.Pages.Signin;
using SmartX.WPF.Views.Pages.Telemetry;
using System.Windows;

namespace SmartX.WPF;

public partial class App : Application
{
    public static ServiceProvider ServiceProvider { get; private set; } = null!;

    public App()
    {
        ServiceCollection services = new();

        ConfigureServices(services);

        ServiceProvider = services.BuildServiceProvider();
    }

    private void ConfigureServices(ServiceCollection services)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile(
                "appsettings.json",
                optional: false,
                reloadOnChange: false)
            .Build();

        // Firebase configuration
        var firebaseOptions = configuration
            .GetSection("Firebase")
            .Get<FirebaseOptions>();

        if (firebaseOptions is null ||
            string.IsNullOrWhiteSpace(firebaseOptions.ApiKey))
        {
            throw new InvalidOperationException(
                "Firebase configuration is missing.");
        }

        services.AddSingleton(firebaseOptions);

        // API configuration
        var apiOptions = configuration
            .GetSection("Api")
            .Get<ApiOptions>();

        if (apiOptions is null ||
            string.IsNullOrWhiteSpace(apiOptions.BaseUrl))
        {
            throw new InvalidOperationException(
                "API configuration is missing.");
        }

        services.AddSingleton(apiOptions);

        // API
        services.AddHttpClient<ISmartXApiClient, SmartXApiClient>(
            client =>
            {
                client.BaseAddress =
                    new Uri(apiOptions.BaseUrl);
            });

        // Firebase
        services.AddHttpClient<FirebaseAuthService>();

        // SQLite Cache
        services.AddSingleton<SmartXCacheDatabase>();

        services.AddSingleton<ILocalSensorCache, SQLiteSensorCache>();
        services.AddSingleton<ILocalTelemetryCache, SQLiteTelemetryCache>();
        services.AddSingleton<ILocalUserCache, SQLiteUserCache>();

        // Synchronization
        services.AddSingleton<ICacheSyncService, CacheSyncService>();

        // Pages
        services.AddTransient<HomePage>();
        services.AddTransient<MainWindow>();
        services.AddTransient<SigninPage>();
        services.AddTransient<SensorsPage>();
        services.AddTransient<TelemetryPage>();
        services.AddTransient<HistoryPage>();
    }

    protected override async void OnStartup(
        StartupEventArgs e)
    {
        base.OnStartup(e);

        var database =
            ServiceProvider.GetRequiredService<
                SmartXCacheDatabase>();

        await database.InitializeAsync();

        var cacheSyncService =
                ServiceProvider.GetRequiredService<ICacheSyncService>();

        await cacheSyncService.SyncSensorsAsync();

        var mainWindow =
            ServiceProvider.GetRequiredService<MainWindow>();

        mainWindow.Show();

        //ok, i can't run because my pages are givinbg errors. this is too confusing. lets finish the api, do the management operations for the users. lets finish other things, then update the pages and test
    }
}