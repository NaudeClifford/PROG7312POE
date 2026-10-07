using SmartX.WPF.ViewModels.Alerts;
using SmartX.WPF.ViewModels.Pages.Company;
using System.Windows.Controls;

namespace SmartX.WPF.Views.Pages.Alerts;

public partial class AlertsPage : Page
{
    public AlertsPage(AlertsViewModel alertsViewModel)
    {
        InitializeComponent();

        DataContext = alertsViewModel;

        Loaded += AlertsPage_Loaded;
    }

    private async void AlertsPage_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is AlertsViewModel viewModel)
        {
            await viewModel.LoadAsync();
        }
    }
}