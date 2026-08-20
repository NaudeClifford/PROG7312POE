using SmartX.WPF.ViewModels.Gateway;
using System.Windows.Controls;

namespace SmartX.WPF.Views.Pages.Gateway;

public partial class GatewaySetupPage : Page
{
    private readonly GatewaySetupViewModel _viewModel;

    public GatewaySetupPage(
        GatewaySetupViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = _viewModel;
    }
}