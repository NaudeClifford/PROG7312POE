using SmartX.WPF.Navigation;
using SmartX.WPF.ViewModels.Telemetry;
using System.Windows;
using System.Windows.Controls;

namespace SmartX.WPF.Views.Pages.Telemetry;

public partial class TelemetryPage : Page, INavigationAware
{
    private readonly TelemetryViewModel _viewModel;

    private readonly Guid _sensorId;

    public TelemetryPage(
        TelemetryViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;

        DataContext = _viewModel;

        Loaded += TelemetryPage_Loaded;
    }

    private async void TelemetryPage_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        await _viewModel.LoadAsync(_sensorId);
    }

    public async void OnNavigatedTo(object parameter)
    {
        if (parameter is not Guid sensorId)
            throw new ArgumentException(
                "TelemetryPage requires a sensor ID.");

        if (DataContext is TelemetryViewModel viewModel)
        {
            await viewModel.LoadSensorAsync(sensorId);
        }
    }
}