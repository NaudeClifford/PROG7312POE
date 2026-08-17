using Microsoft.Extensions.DependencyInjection;
using SmartX.Domain.Enums;
using SmartX.WPF.Services;
using SmartX.WPF.Views.Pages;
using SmartX.WPF.Views.Pages.History;
using SmartX.WPF.Views.Pages.Home;
using SmartX.WPF.Views.Pages.Sensor;
using SmartX.WPF.Views.Pages.Telemetry;
using System.Windows;
using System.Windows.Input;

namespace SmartX.WPF;

public partial class MainWindow : Window
{
    private readonly HomePage _homePage;
    private readonly SmartXSession _session;

    public MainWindow(
        HomePage homePage,
        SmartXSession session)
    {
        InitializeComponent();

        _homePage = homePage;
        _session = session;

        ApplyRolePermissions();

        MainFrame.Navigate(_homePage);
    }

    private void ApplyRolePermissions()
    {
        // Everyone can access Home.
        HomeButton.Visibility = Visibility.Visible;

        // Viewer
        if (_session.Role == UserRole.Viewer)
        {
            SensorsButton.Visibility = Visibility.Collapsed;
            TelemetryButton.Visibility = Visibility.Collapsed;
            HistoryButton.Visibility = Visibility.Collapsed;
            AdministrationButton.Visibility = Visibility.Collapsed;

            return;
        }

        // Technician
        if (_session.Role == UserRole.Technician)
        {
            SensorsButton.Visibility = Visibility.Visible;
            TelemetryButton.Visibility = Visibility.Visible;
            HistoryButton.Visibility = Visibility.Visible;
            AdministrationButton.Visibility = Visibility.Collapsed;

            return;
        }

        // Administrator
        if (_session.Role == UserRole.Administrator)
        {
            SensorsButton.Visibility = Visibility.Visible;
            TelemetryButton.Visibility = Visibility.Visible;
            HistoryButton.Visibility = Visibility.Visible;
            AdministrationButton.Visibility = Visibility.Visible;

            return;
        }

        // Guest / unauthenticated
        SensorsButton.Visibility = Visibility.Collapsed;
        TelemetryButton.Visibility = Visibility.Collapsed;
        HistoryButton.Visibility = Visibility.Collapsed;
        AdministrationButton.Visibility = Visibility.Collapsed;
    }

    private void Home_Click(
        object sender,
        RoutedEventArgs e)
    {
        MainFrame.Navigate(_homePage);
    }

    private void Sensors_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_session.Role is not UserRole.Technician
            and not UserRole.Administrator)
            return;

        var page = App.ServiceProvider
            .GetRequiredService<SensorsPage>();

        MainFrame.Navigate(page);
    }

    private void Telemetry_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_session.Role is not UserRole.Technician
            and not UserRole.Administrator)
            return;

        var page = App.ServiceProvider
            .GetRequiredService<TelemetryPage>();

        MainFrame.Navigate(page);
    }

    private void History_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_session.Role is not UserRole.Technician
            and not UserRole.Administrator)
            return;

        var page = App.ServiceProvider
            .GetRequiredService<HistoryPage>();

        MainFrame.Navigate(page);
    }

    private void Administration_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_session.Role != UserRole.Administrator)
            return;

        // We'll add the administration page later.
    }

    private void TitleBar_MouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    private void Minimize_Click(
        object sender,
        RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
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