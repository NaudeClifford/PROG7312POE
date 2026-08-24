using SmartX.WPF.ViewModels;
using SmartX.WPF.ViewModels.Gateway;
using System.Windows;
using System.Windows.Controls;

namespace SmartX.WPF.Views.Pages.Gateway;

public partial class GatewaySetupPage : Page
{
    public GatewaySetupPage(GatewaySetupViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;

        Loaded += GatewaySetupPage_Loaded;
    }

    private async void GatewaySetupPage_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        Loaded -= GatewaySetupPage_Loaded;

        if (DataContext is GatewaySetupViewModel viewModel)
        {
            await viewModel.LoadAsync();
        }
    }

    private void Page_PreviewKeyDown(
    object sender,
    System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != System.Windows.Input.Key.Enter)
            return;

        if (DataContext is not SigninViewModel viewModel)
            return;

        if (!viewModel.SignInCommand.CanExecute(null))
            return;

        viewModel.SignInCommand.Execute(null);

        e.Handled = true;
    }
}