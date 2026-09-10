using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SmartX.WPF.Navigation;
using SmartX.WPF.ViewModels.Pages.Sensor;

namespace SmartX.WPF.Views.Pages.Sensor;

public partial class SensorSetupPage : Page, INavigationAware
{
    private readonly SensorViewModel _viewModel;

    public SensorSetupPage(
        SensorViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;

        DataContext = _viewModel;
    }

    public void OnNavigatedTo(object parameter)
    {
        _viewModel.OnNavigatedTo(parameter);
    }

    private void Page_PreviewKeyDown(
        object sender,
        KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            NavigationService?.GoBack();

            e.Handled = true;
        }
    }
}
