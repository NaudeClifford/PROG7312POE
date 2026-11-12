
using SmartX.WPF.ViewModels.Mesh;
using System.Windows;
using System.Windows.Controls;

namespace SmartX.WPF.Views.Pages.Mesh;

public partial class MeshDashboardPage : Page
{
    private readonly MeshDashboardViewModel _viewModel;

    public MeshDashboardPage(
        MeshDashboardViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;

        DataContext = _viewModel;

        Loaded += MeshDashboardPage_Loaded;
    }

    private async void MeshDashboardPage_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        Loaded -= MeshDashboardPage_Loaded;

        await _viewModel.LoadDashboardAsync();
    }
}
