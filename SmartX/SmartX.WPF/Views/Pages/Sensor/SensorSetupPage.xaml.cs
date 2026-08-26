using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SmartX.WPF.ViewModels.Pages.Sensor;

namespace SmartX.WPF.Views.Pages.Sensor;

public partial class SensorSetupPage : Page
{
    private readonly SensorViewModel _viewModel;

    public SensorSetupPage(
        SensorViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;

        DataContext = _viewModel;

        Loaded += SensorSetupPage_Loaded;
    }

    private async void SensorSetupPage_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        Loaded -= SensorSetupPage_Loaded;

        await _viewModel.LoadAsync();
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