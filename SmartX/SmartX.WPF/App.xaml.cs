using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartX.Application.Authentication;
using SmartX.WPF.Authentication;
using SmartX.WPF.Data;
using SmartX.WPF.Navigation;
using SmartX.WPF.Repositories.Local;
using SmartX.WPF.Services;
using SmartX.WPF.Services.Api;
using SmartX.WPF.Services.Sync;
using SmartX.WPF.ViewModels;
using SmartX.WPF.ViewModels.History;
using SmartX.WPF.ViewModels.Pages.Sensor;
using SmartX.WPF.ViewModels.Telemetry;
using SmartX.WPF.Views.Pages.History;
using SmartX.WPF.Views.Pages.Home;
using SmartX.WPF.Views.Pages.Sensor;
using SmartX.WPF.Views.Pages.Signin;
using SmartX.WPF.Views.Pages.Telemetry;
using System.Windows;

namespace SmartX.WPF;

public partial class App
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
                client.BaseAddress = new Uri(apiOptions.BaseUrl);
            });

        //Session
        services.AddSingleton<SmartXSession>();

        // SQLite Cache
        services.AddSingleton<SmartXCacheDatabase>();

        services.AddSingleton<ILocalSensorCache, SQLiteSensorCache>();
        services.AddSingleton<ILocalTelemetryCache, SQLiteTelemetryCache>();
        services.AddSingleton<ILocalUserCache, SQLiteUserCache>();
        services.AddSingleton<ILocalCompanyCache, SQLiteCompanyCache>();
        services.AddSingleton<ILocalGatewayCache, SQLiteGatewayCache>();

        // Synchronization
        services.AddSingleton<ICacheSyncService, CacheSyncService>();

        // ViewModels
        services.AddTransient<HomeViewModel>();
        services.AddTransient<SigninViewModel>();
        services.AddTransient<HistoryViewModel>();
        services.AddTransient<SensorViewModel>();
        services.AddTransient<TelemetryViewModel>();

        // Pages
        services.AddTransient<HomePage>();
        services.AddTransient<SigninPage>();
        services.AddTransient<SensorsPage>();
        services.AddTransient<TelemetryPage>();
        services.AddTransient<HistoryPage>();

        //Main window
        services.AddSingleton<MainWindow>();

        // Navigation
        services.AddSingleton<INavigationService, NavigationService>();

        //Authentication
        services.AddHttpClient<IAuthenticationService,FirebaseAuthService>();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var database =
                ServiceProvider.GetRequiredService<SmartXCacheDatabase>();

            await database.InitializeAsync();

            var mainWindow =
                ServiceProvider.GetRequiredService<MainWindow>();

            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.ToString(),
                "SmartX Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown();
        }
    }
}