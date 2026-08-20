using SmartX.WPF.Navigation;
using SmartX.WPF.ViewModels.Telemetry;
using System.Windows.Controls;

namespace SmartX.WPF.Views.Pages.Telemetry;

public partial class TelemetryPage : Page, INavigationAware
{
    private readonly TelemetryViewModel _viewModel;

    public TelemetryPage(TelemetryViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    public async void OnNavigatedTo(object parameter)
    {
        if (parameter is not Guid sensorId)
            return;

        await _viewModel.LoadAsync(sensorId);
    }
}