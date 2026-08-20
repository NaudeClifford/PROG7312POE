using Microsoft.Extensions.DependencyInjection;
using SmartX.Domain.Enums;
using SmartX.WPF.Services;
using SmartX.WPF.Views.Pages.Gateway;
using SmartX.WPF.Views.Pages.History;
using SmartX.WPF.Views.Pages.Home;
using SmartX.WPF.Views.Pages.Sensor;
using SmartX.WPF.Views.Pages.Telemetry;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SmartX.WPF;

public partial class MainWindow : Window
{
    private readonly HomePage _homePage;
    private readonly SmartXSession _session;

    private NavigationStage _navigationStage =
        NavigationStage.Home;


    // =========================================================
    // NAVIGATION STAGE
    // =========================================================

    private enum NavigationStage
    {
        Home,

        Gateway,

        Sensors,

        Telemetry,

        History,

        NetworkMesh,

        CommandHistory
    }


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public MainWindow(
        HomePage homePage,
        SmartXSession session)
    {
        InitializeComponent();

        _homePage = homePage;
        _session = session;

        MainFrame.Navigate(_homePage);

        UpdateNavigation();
    }


    // =========================================================
    // PUBLIC NAVIGATION REFRESH
    // =========================================================

    public void RefreshNavigation()
    {
        UpdateNavigation();
    }


    // =========================================================
    // NAVIGATION VISIBILITY
    // =========================================================

    private void UpdateNavigation()
    {
        if (!_session.IsAuthenticated)
        {
            HideNavigation();
            return;
        }

        NavigationBorder.Visibility =
            Visibility.Visible;

        NavigationRow.Height =
            new GridLength(70);

        ApplyNavigationPermissions();
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

        GatewayButton.Visibility =
            Visibility.Collapsed;

        SensorsButton.Visibility =
            Visibility.Collapsed;

        TelemetryButton.Visibility =
            Visibility.Collapsed;

        HistoryButton.Visibility =
            Visibility.Collapsed;

        BackButton.Visibility =
            Visibility.Collapsed;

        LogOutButton.Visibility =
            Visibility.Collapsed;
    }


    // =========================================================
    // ROLE NAVIGATION
    // =========================================================

    private void ApplyNavigationPermissions()
    {
        HideAllNavigationButtons();

        // Logout is available to every authenticated user.
        LogOutButton.Visibility =
            Visibility.Visible;

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


    // =========================================================
    // ADMINISTRATOR
    // =========================================================

    private void ApplyAdministratorNavigation()
    {
        switch (_navigationStage)
        {
            case NavigationStage.Home:

                HomeButton.Visibility =
                    Visibility.Visible;

                GatewayButton.Visibility =
                    Visibility.Visible;

                break;


            case NavigationStage.Gateway:

                HomeButton.Visibility =
                    Visibility.Visible;

                GatewayButton.Visibility =
                    Visibility.Visible;

                break;


            case NavigationStage.Sensors:

            case NavigationStage.Telemetry:

            case NavigationStage.History:

            case NavigationStage.NetworkMesh:

            case NavigationStage.CommandHistory:

                SensorsButton.Visibility =
                    Visibility.Visible;

                TelemetryButton.Visibility =
                    Visibility.Visible;

                HistoryButton.Visibility =
                    Visibility.Visible;

                BackButton.Visibility =
                    Visibility.Visible;

                break;
        }
    }


    // =========================================================
    // TECHNICIAN
    // =========================================================

    private void ApplyTechnicianNavigation()
    {
        switch (_navigationStage)
        {
            case NavigationStage.Home:

                HomeButton.Visibility =
                    Visibility.Visible;

                break;


            case NavigationStage.Sensors:

            case NavigationStage.Telemetry:

            case NavigationStage.History:

            case NavigationStage.NetworkMesh:

            case NavigationStage.CommandHistory:

                SensorsButton.Visibility =
                    Visibility.Visible;

                TelemetryButton.Visibility =
                    Visibility.Visible;

                HistoryButton.Visibility =
                    Visibility.Visible;

                BackButton.Visibility =
                    Visibility.Visible;

                break;

            case NavigationStage.Gateway:

                // Technician cannot access Gateway.

                HomeButton.Visibility =
                    Visibility.Visible;

                break;
        }
    }


    // =========================================================
    // SUPER ADMINISTRATOR
    // =========================================================

    private void ApplySuperAdminNavigation()
    {
        /*
         * SuperAdmin does NOT access:
         *
         * Gateway
         * Sensors
         * Telemetry
         * History
         *
         * SuperAdmin navigation will eventually contain:
         *
         * Users
         * Companies
         * Admin accounts
         * API monitoring
         * Analytics
         * System management
         *
         * Those buttons can be added here.
         */

        HomeButton.Visibility =
            Visibility.Visible;
    }


    // =========================================================
    // VIEWER
    // =========================================================

    private void ApplyViewerNavigation()
    {
        HomeButton.Visibility =
            Visibility.Visible;
    }


    // =========================================================
    // HOME
    // =========================================================

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

        UpdateNavigation();
    }


    // =========================================================
    // GATEWAY
    // =========================================================

    private void Gateway_Click(
        object sender,
        RoutedEventArgs e)
    {
        // Gateway is Administrator only.
        if (_session.Role != UserRole.Administrator)
            return;

        var page =
            App.ServiceProvider
                .GetRequiredService<GatewayPage>();

        _navigationStage =
            NavigationStage.Gateway;

        MainFrame.Navigate(page);

        UpdateNavigation();
    }


    // =========================================================
    // SENSORS
    // =========================================================

    private void Sensors_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!CanAccessGatewayArea())
            return;

        NavigateTo<SensorsPage>(
            NavigationStage.Sensors);
    }


    // =========================================================
    // TELEMETRY
    // =========================================================

    private void Telemetry_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!CanAccessGatewayArea())
            return;

        NavigateTo<TelemetryPage>(
            NavigationStage.Telemetry);
    }


    // =========================================================
    // HISTORY
    // =========================================================

    private void History_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!CanAccessGatewayArea())
            return;

        NavigateTo<HistoryPage>(
            NavigationStage.History);
    }


    // =========================================================
    // BACK
    // =========================================================

    private void Back_Click(
        object sender,
        RoutedEventArgs e)
    {
        NavigateHome();
    }


    // =========================================================
    // GATEWAY AREA ACCESS
    // =========================================================

    private bool CanAccessGatewayArea()
    {
        return _session.Role is
            UserRole.Technician or
            UserRole.Administrator;
    }


    // =========================================================
    // GENERIC PAGE NAVIGATION
    // =========================================================

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

        UpdateNavigation();
    }


    // =========================================================
    // LOGOUT
    // =========================================================

    private void LogOut_Click(
        object sender,
        RoutedEventArgs e)
    {
        _session.SignOut();

        _navigationStage =
            NavigationStage.Home;

        HideNavigation();

        MainFrame.Navigate(_homePage);
    }


    // =========================================================
    // WINDOW CONTROLS
    // =========================================================

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
}