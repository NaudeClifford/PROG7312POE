
using SmartX.WPF.ViewModels.Mesh;
using System.Windows.Controls;

namespace SmartX.WPF.Views.Pages.Mesh;

public partial class MeshRoutingPage : Page
{
    private readonly MeshRoutingViewModel _viewModel;

    public MeshRoutingPage(MeshRoutingViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = _viewModel;
    }
}
