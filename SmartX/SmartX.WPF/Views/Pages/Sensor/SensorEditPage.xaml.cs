using SmartX.WPF.Navigation;
using SmartX.WPF.ViewModels.Pages.Sensor;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SmartX.WPF.Views.Pages.Sensor;

public partial class SensorEditPage : Page, INavigationAware
{
    private readonly SensorViewModel _viewModel;

    public SensorEditPage(SensorViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    private void Page_PreviewKeyDown(
        object sender,
        KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        if (_viewModel.SaveSensorCommand.CanExecute(null))
        {
            _viewModel.SaveSensorCommand.Execute(null);

            e.Handled = true;
        }
    }

    public void OnNavigatedTo(object parameter)
    {
        if (parameter is Guid sensorId)
        {
            _viewModel.OnNavigatedTo(sensorId);
        }
    }
}
