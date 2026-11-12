
using SmartX.WPF.ViewModels.Mesh;
using System.Windows;
using System.Windows.Controls;

namespace SmartX.WPF.Views.Pages.Mesh;

public partial class NetworkTopologyPage : Page
{
    private readonly NetworkTopologyViewModel _viewModel;

    public NetworkTopologyPage(
        NetworkTopologyViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = _viewModel;

        Loaded += NetworkTopologyPage_Loaded;
    }

    private async void NetworkTopologyPage_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        Loaded -= NetworkTopologyPage_Loaded;

        await _viewModel.LoadTopologyAsync();
    }
}
