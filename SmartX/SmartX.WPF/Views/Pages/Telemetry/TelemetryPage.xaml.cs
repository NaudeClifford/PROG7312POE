using SmartX.WPF.Navigation;
using SmartX.WPF.ViewModels.Telemetry;
using System;
using System.Windows;
using System.Windows.Controls;

namespace SmartX.WPF.Views.Pages.Telemetry;

public partial class TelemetryPage : Page, INavigationAware
{
    private readonly TelemetryViewModel _viewModel;

    private bool _navigationWasHandled;

    public TelemetryPage(
        TelemetryViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = _viewModel;

        Loaded += TelemetryPage_Loaded;
    }

    public void OnNavigatedTo(object parameter)
    {
        _navigationWasHandled = true;

        if (parameter is Guid sensorId &&
            sensorId != Guid.Empty)
        {
            _ = _viewModel.LoadAsync(sensorId);
            return;
        }

        _ = _viewModel.LoadAsync();
    }

    private async void TelemetryPage_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        if (_navigationWasHandled)
        {
            _navigationWasHandled = false;
            return;
        }

        await _viewModel.LoadAsync();
    }
}
