using Microsoft.Extensions.DependencyInjection;
using SmartX.Domain.Enums;
using SmartX.WPF.Navigation;
using SmartX.WPF.Services.Api;
using SmartX.WPF.Services.Session;
using SmartX.WPF.Views.Pages.Company;
using SmartX.WPF.Views.Pages.Gateway;
using SmartX.WPF.Views.Pages.History;
using SmartX.WPF.Views.Pages.Home;
using SmartX.WPF.Views.Pages.Sensor;
using SmartX.WPF.Views.Pages.Signin;
using SmartX.WPF.Views.Pages.Telemetry;
using SmartX.WPF.Views.Pages.Users;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace SmartX.WPF;

public partial class MainWindow : Window
{
    private readonly HomePage _homePage;
    private readonly SmartXSession _session;
    private readonly SmartXAuthenticationService _authenticationService;
    private readonly INavigationService _navigationService;
    private readonly ISmartXApiClient _apiClient;
    private readonly DispatcherTimer _connectivityTimer;

    private NavigationStage _navigationStage =
        NavigationStage.Home;

    // NAVIGATION STAGE

    private enum NavigationStage
    {
        Home,

        // Administrator / SuperAdmin
        Users,
        CurrentCompany,
        Companies,

        // Gateway area
        Gateway,
        Sensors,
        Telemetry,
        History,

        NetworkMesh,
        CommandHistory
    }


    // CONSTRUCTOR

    public MainWindow(
        INavigationService navigationService,
        HomePage homePage,
        SmartXSession session,
        SmartXAuthenticationService authenticationService,

        ISmartXApiClient apiClient)
    {
        InitializeComponent();

        _navigationService = navigationService;
        _homePage = homePage;
        _session = session;
        _authenticationService = authenticationService;
        _apiClient = apiClient;

        UpdateLoggedInUser();

        _session.PropertyChanged += Session_PropertyChanged;

        _navigationService.SetFrame(MainFrame);


        MainFrame.Navigated += MainFrame_Navigated;

        Loaded += MainWindow_Loaded;

        _connectivityTimer =
            new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(60)
            };

        _connectivityTimer.Tick +=
            ConnectivityTimer_Tick;
    }

    // STARTUP

    private async void MainWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        Loaded -= MainWindow_Loaded;
        _connectivityTimer.Start();


        try
        {        
            await UpdateConnectionStatusAsync();

            await InitializeNavigationAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.ToString(),
                "Navigation Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            NavigatePublicHome();
        }
    }


    private async Task InitializeNavigationAsync()
    {

        if (_session.IsAuthenticated)
        {

            if (_session.Role == UserRole.SuperAdmin)
            {
                _session.CompleteOnboarding();

                NavigateHome();

                return;
            }

            if (_session.CompanyId != Guid.Empty)
            {
                var hasGateway =
                    await CompanyHasGatewayAsync(
                        _session.CompanyId);

                if (hasGateway)
                {
                    _session.CompleteOnboarding();

                    NavigateTo<GatewayPage>(
                        NavigationStage.Gateway);

                    return;
                }

                _session.BeginOnboarding();

                NavigateTo<GatewaySetupPage>(
                    NavigationStage.Gateway);

                return;
            }


            // INVALID AUTHENTICATED SESSION

            NavigateHome();

            return;
        }

        // GUEST

        if (_session.IsGuest)
        {
            NavigateHome();

            return;
        }

        // NOT AUTHENTICATED

        _navigationStage = NavigationStage.Home;

        HideNavigation();

        _navigationService.NavigateTo<HomePage>();

    }


    private async Task<bool> CompanyHasGatewayAsync(
        Guid companyId)
    {
        try
        {
            var gateways =
                await _apiClient
                    .GetGatewaysByCompanyIdAsync(companyId);

            if (gateways is null || gateways.Count == 0)
            {
                return false;
            }

            var gateway =
                gateways.FirstOrDefault(
                    x => x.IsActive)
                ?? gateways.First();

            _session.SelectGateway(
                gateway.Id,
                gateway.Name);

            return true;
        }
        catch
        {
            return false;
        }
    }


    // PUBLIC NAVIGATION REFRESH

    public void RefreshNavigation()
    {
        UpdateNavigation();
    }


    // NAVIGATION VISIBILITY

    private void UpdateNavigation()
    {

        if (_session.IsOnboarding)
        {
            HideNavigation();

            return;
        }

        if (!_session.IsAuthenticated &&
            !_session.IsGuest)
        {
            HideNavigation();

            return;
        }

        ShowNavigation();

        ApplyNavigationPermissions();
    }


    private void ShowNavigation()
    {
        NavigationBorder.Visibility =
            Visibility.Visible;

        NavigationRow.Height =
            new GridLength(70);
    }


    private void HideNavigation()
    {
        NavigationBorder.Visibility =
            Visibility.Collapsed;

        NavigationRow.Height =
            new GridLength(0);

        HideAllNavigationButtons();
    }


    private void HideAllNavigationButtons()
    {
        HomeButton.Visibility =
            Visibility.Collapsed;

        UsersButton.Visibility =
            Visibility.Collapsed;

        CurrentCompanyButton.Visibility =
            Visibility.Collapsed;

        CompaniesButton.Visibility =
            Visibility.Collapsed;

        GatewayButton.Visibility =
            Visibility.Collapsed;

        SensorsButton.Visibility =
            Visibility.Collapsed;

        TelemetryButton.Visibility =
            Visibility.Collapsed;

        HistoryButton.Visibility =
            Visibility.Collapsed;

        LogOutButton.Visibility =
            Visibility.Collapsed;
    }


    // ROLE NAVIGATION

    private void ApplyNavigationPermissions()
    {
        HideAllNavigationButtons();

        LogOutButton.Visibility =
            Visibility.Visible;


        if (_session.IsGuest)
        {
            ApplyGuestNavigation();

            return;
        }


        switch (_session.Role)
        {
            case UserRole.Administrator:

                ApplyAdministratorNavigation();

                break;

            case UserRole.Technician:

                ApplyTechnicianNavigation();

                break;

            case UserRole.SuperAdmin:

                ApplySuperAdminNavigation();

                break;

            case UserRole.Viewer:

                ApplyViewerNavigation();

                break;

            default:

                HideAllNavigationButtons();

                break;
        }
    }

    // FRAME NAVIGATION

    private void MainFrame_Navigated(
        object sender,
        System.Windows.Navigation.NavigationEventArgs e)
    {
        if (e.Content is not Page page)
            return;


        // ONBOARDING

        if (page is GatewaySetupPage)
        {
            _navigationStage =
                NavigationStage.Gateway;

            HideNavigation();

            return;
        }


        // NORMAL PAGES

        switch (page)
        {
            case HomePage:

                _navigationStage =
                    NavigationStage.Home;

                break;

            case UsersPage:

                _navigationStage =
                    NavigationStage.Users;

                break;

            case CurrentCompanyPage:

                _navigationStage =
                    NavigationStage.CurrentCompany;

                break;

            case CompaniesPage:

                _navigationStage =
                    NavigationStage.Companies;

                break;

            case GatewayPage:

                _navigationStage =
                    NavigationStage.Gateway;

                break;

            case SensorsPage:
            case SensorEditPage:
            case SensorSetupPage:

                _navigationStage =
                    NavigationStage.Sensors;

                break;

            case TelemetryPage:

                _navigationStage =
                    NavigationStage.Telemetry;

                break;

            case HistoryPage:

                _navigationStage =
                    NavigationStage.History;

                break;
        }


        UpdateNavigation();
    }


    // GUEST

    private void ApplyGuestNavigation()
    {
        HomeButton.Visibility =
            Visibility.Visible;

        GatewayButton.Visibility =
            Visibility.Visible;

        SensorsButton.Visibility =
            Visibility.Visible;

        TelemetryButton.Visibility =
            Visibility.Visible;

        LogOutButton.Visibility =
            Visibility.Visible;
    }


    // ADMINISTRATOR

    private void ApplyAdministratorNavigation()
    {
        switch (_navigationStage)
        {
            case NavigationStage.Home:

                UsersButton.Visibility =
                    Visibility.Visible;

                CurrentCompanyButton.Visibility =
                    Visibility.Visible;

                GatewayButton.Visibility =
                    Visibility.Visible;

                break;

            case NavigationStage.Users:

                CurrentCompanyButton.Visibility =
                    Visibility.Visible;

                GatewayButton.Visibility =
                    Visibility.Visible;

                break;

            case NavigationStage.CurrentCompany:

                UsersButton.Visibility =
                    Visibility.Visible;

                GatewayButton.Visibility =
                    Visibility.Visible;

                break;

            case NavigationStage.Gateway:

                UsersButton.Visibility =
                    Visibility.Visible;

                CurrentCompanyButton.Visibility =
                    Visibility.Visible;

                break;

            case NavigationStage.Sensors:

                UsersButton.Visibility =
                    Visibility.Visible;

                CurrentCompanyButton.Visibility =
                    Visibility.Visible;

                GatewayButton.Visibility =
                    Visibility.Visible;

                TelemetryButton.Visibility =
                    Visibility.Visible;

                break;

            case NavigationStage.Telemetry:

                GatewayButton.Visibility =
                    Visibility.Visible;

                SensorsButton.Visibility =
                    Visibility.Visible;

                UsersButton.Visibility =
                    Visibility.Visible;

                CurrentCompanyButton.Visibility =
                    Visibility.Visible;

                break;

            case NavigationStage.History:

                GatewayButton.Visibility =
                    Visibility.Visible;

                break;

            case NavigationStage.NetworkMesh:
            case NavigationStage.CommandHistory:

                GatewayButton.Visibility =
                    Visibility.Visible;

                HistoryButton.Visibility =
                    Visibility.Visible;

                break;
        }
    }

    // TECHNICIAN

    private void ApplyTechnicianNavigation()
    {
        switch (_navigationStage)
        {
            case NavigationStage.Home:

                GatewayButton.Visibility =
                    Visibility.Visible;

                break;

            case NavigationStage.Gateway:

                break;

            case NavigationStage.Sensors:

                GatewayButton.Visibility =
                    Visibility.Visible;

                TelemetryButton.Visibility =
                    Visibility.Visible;

                break;

            case NavigationStage.Telemetry:

                GatewayButton.Visibility =
                    Visibility.Visible;

                SensorsButton.Visibility =
                    Visibility.Visible;

                break;

            case NavigationStage.History:

                GatewayButton.Visibility =
                    Visibility.Visible;

                break;

            case NavigationStage.NetworkMesh:
            case NavigationStage.CommandHistory:

                GatewayButton.Visibility =
                    Visibility.Visible;

                HistoryButton.Visibility =
                    Visibility.Visible;

                break;
        }
    }


    // SUPER ADMINISTRATOR

    private void ApplySuperAdminNavigation()
    {
        switch (_navigationStage)
        {
            case NavigationStage.Home:

                UsersButton.Visibility =
                    Visibility.Visible;

                CompaniesButton.Visibility =
                    Visibility.Visible;

                break;

            case NavigationStage.Users:

                CompaniesButton.Visibility =
                    Visibility.Visible;

                break;

            case NavigationStage.Companies:

                UsersButton.Visibility =
                    Visibility.Visible;

                break;

            default:

                break;
        }
    }

    // VIEWER
    private void ApplyViewerNavigation()
    {
        HomeButton.Visibility =
            Visibility.Visible;
    }

    // HOME

    private void Home_Click(
        object sender,
        RoutedEventArgs e)
    {
        NavigateHome();
    }


    private void SmartXHome_Click(
        object sender,
        RoutedEventArgs e)
    {
        NavigateHome();

        e.Handled = true;
    }


    private void NavigateHome()
    {
        _navigationStage =
            NavigationStage.Home;

        MainFrame.Navigate(_homePage);
    }


    private void NavigatePublicHome()
    {
        _navigationStage =
            NavigationStage.Home;

        HideNavigation();

        MainFrame.Navigate(_homePage);
    }

    // CONNECTIVITY

    private async void ConnectivityTimer_Tick(
        object? sender,
        EventArgs e)
    {
        await UpdateConnectionStatusAsync();
    }


    private async Task UpdateConnectionStatusAsync()
    {
        try
        {
            var isOnline =
                await _apiClient.IsAvailableAsync();

            if (isOnline)
            {
                ConnectionStatusIndicator.Fill =
                    new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(
                            0x28,
                            0xA7,
                            0x45));

                ConnectionStatusText.Text = "Live";

                ConnectionStatusText.Foreground =
                    new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(
                            0x21,
                            0x88,
                            0x38));
            }
            else
            {
                SetOfflineStatus();
            }
        }
        catch
        {
            SetOfflineStatus();
        }
    }

    private void SetOfflineStatus()
    {
        ConnectionStatusIndicator.Fill =
            new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(
                    0xDC,
                    0x35,
                    0x45));

        ConnectionStatusText.Text = "Offline";

        ConnectionStatusText.Foreground =
            new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(
                    0xC8,
                    0x23,
                    0x33));
    }
    // USERS

    private void Users_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_session.Role != UserRole.Administrator &&
            _session.Role != UserRole.SuperAdmin)
        {
            return;
        }

        NavigateTo<UsersPage>(
            NavigationStage.Users);
    }

    // CURRENT COMPANY

    private void CurrentCompany_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_session.Role !=
            UserRole.Administrator)
        {
            return;
        }

        NavigateTo<CurrentCompanyPage>(
            NavigationStage.CurrentCompany);
    }


    // COMPANIES

    private void Companies_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_session.Role !=
            UserRole.SuperAdmin)
        {
            return;
        }

        NavigateTo<CompaniesPage>(
            NavigationStage.Companies);
    }

    // GATEWAY

    private void Gateway_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_session.Role is not
            (UserRole.Administrator or
             UserRole.Technician))
        {
            return;
        }

        NavigateTo<GatewayPage>(
            NavigationStage.Gateway);
    }

    // SENSORS

    private void Sensors_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!CanAccessGatewayArea())
            return;

        NavigateTo<SensorsPage>(
            NavigationStage.Sensors);
    }

    // TELEMETRY
    private void Telemetry_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!CanAccessGatewayArea())
            return;

        NavigateTo<TelemetryPage>(
            NavigationStage.Telemetry);
    }


    // HISTORY
    private void History_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!CanAccessGatewayArea())
            return;

        NavigateTo<HistoryPage>(
            NavigationStage.History);
    }

    // GATEWAY AREA ACCESS

    private bool CanAccessGatewayArea()
    {
        return _session.Role is
            UserRole.Technician or
            UserRole.Administrator;
    }


    // GENERIC PAGE NAVIGATION

    private void NavigateTo<T>(
        NavigationStage stage)
        where T : Page
    {
        var page =
            App.ServiceProvider
                .GetRequiredService<T>();

        _navigationStage =
            stage;

        MainFrame.Navigate(page);
    }

    // LOGOUT

    private async void LogOut_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            await _authenticationService.LogoutAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Logout Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            return;
        }

        _navigationStage =
            NavigationStage.Home;

        NavigatePublicHome();
    }


    private async void Session_PropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SmartXSession.DisplayName))
        {
            UpdateLoggedInUser();
        }

        if (e.PropertyName == nameof(SmartXSession.IsAuthenticated))
        {
            await HandleSuccessfulLoginAsync();
        }
    }




    private void UpdateLoggedInUser()
    {
        UserDisplayText.Text =
            string.IsNullOrWhiteSpace(_session.DisplayName)
                ? "Unknown user"
                : _session.DisplayName;
    }


    protected override void OnClosed(EventArgs e)
    {
        _connectivityTimer.Stop();

        _session.PropertyChanged -= Session_PropertyChanged;

        base.OnClosed(e);
    }

    // WINDOW CONTROLS

    private void TitleBar_MouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (e.ButtonState ==
            MouseButtonState.Pressed)
        {
            DragMove();
        }
    }


    private void Minimize_Click(
        object sender,
        RoutedEventArgs e)
    {
        WindowState =
            WindowState.Minimized;
    }


    private void Maximize_Click(
        object sender,
        RoutedEventArgs e)
    {
        WindowState =
            WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
    }


    private void Close_Click(
        object sender,
        RoutedEventArgs e)
    {
        Close();
    }

    public async Task HandleSuccessfulLoginAsync()
    {
        await InitializeNavigationAsync();
    }

}
