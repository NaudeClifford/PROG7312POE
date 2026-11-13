using SmartX.WPF.ViewModels.Alerts;
using System.Windows;
using System.Windows.Controls;

namespace SmartX.WPF.Views.Pages.Alerts;

public partial class AlertsPage : Page
{
    private readonly AlertsViewModel _viewModel;

    public AlertsPage(AlertsViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;

        DataContext = _viewModel;

        Loaded += AlertsPage_Loaded;
    }

    private async void AlertsPage_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        await _viewModel.LoadAsync();
    }
}