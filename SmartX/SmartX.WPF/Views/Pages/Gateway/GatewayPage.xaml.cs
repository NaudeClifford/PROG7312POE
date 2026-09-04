using Microsoft.Extensions.DependencyInjection;
using SmartX.WPF.ViewModels.Gateway;
using System.Windows;
using System.Windows.Controls;

namespace SmartX.WPF.Views.Pages.Gateway;

public partial class GatewayPage : Page
{
    private readonly GatewayViewModel _viewModel;

    public GatewayPage(
        GatewayViewModel viewModel)
    {
        
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = _viewModel;

        Loaded += GatewayPage_Loaded;
    }

    private async void GatewayPage_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        Loaded -= GatewayPage_Loaded;

        _viewModel.ResetEditor();

        await _viewModel.LoadAsync();

    }


}