using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartX.WPF.Authentication;
using SmartX.WPF.Views.Pages;
using SmartX.WPF.Views.Pages.History;
using SmartX.WPF.Views.Pages.Home;
using SmartX.WPF.Views.Pages.Sensor;
using SmartX.WPF.Views.Pages.Signin;
using SmartX.WPF.Views.Pages.Telemetry;
using System.Configuration;
using System.Data;
using System.Windows;

namespace SmartX.WPF
{
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

            // Services
            /*services.AddSingleton<ExpenseService>();
            services.AddHttpClient<ExchangeRateService>();
            services.AddSingleton<BalanceService>();
            services.AddSingleton<SettlementService>();
            services.AddSingleton<AnalyticsService>();
            services.AddSingleton<StorageService>();
            
            // ViewModels
            services.AddTransient<BalancesViewModel>();
            services.AddTransient<AnalyticsViewModel>();
            services.AddTransient<ExpensesViewModel>();
            services.AddTransient<MainViewModel>();
            services.AddTransient<SettleUpViewModel>();
            services.AddTransient<HomeViewModel>();
            services.AddTransient<AboutUsViewModel>();
            services.AddTransient<PeopleViewModel>();
            */

            // Firebase
            services.AddHttpClient<FirebaseAuthService>();

            // Pages
            services.AddTransient<HomePage>();
            services.AddTransient<MainWindow>();
            services.AddTransient<SigninPage>();
            services.AddTransient<SensorsPage>();
            services.AddTransient<TelemetryPage>();
            services.AddTransient<HistoryPage>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();

            mainWindow.Show();
        }


    }

}
