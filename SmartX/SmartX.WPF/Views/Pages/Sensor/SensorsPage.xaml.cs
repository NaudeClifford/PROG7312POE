using System.Windows;
using System.Windows.Controls;
using SmartX.WPF.ViewModels.Pages.Sensor;

namespace SmartX.WPF.Views.Pages.Sensor;

public partial class SensorsPage : Page
{
    private readonly SensorViewModel _viewModel;

    public SensorsPage(
        SensorViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;

        DataContext = _viewModel;

        Loaded += SensorsPage_Loaded;
    }

    private async void SensorsPage_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        await _viewModel.LoadAsync();
    }

    public async Task RefreshAsync()
    {
        await _viewModel.LoadAsync();
    }
}