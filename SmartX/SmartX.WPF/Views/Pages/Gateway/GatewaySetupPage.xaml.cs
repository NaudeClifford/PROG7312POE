using SmartX.WPF.Navigation;
using SmartX.WPF.ViewModels.Gateway;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SmartX.WPF.Views.Pages.Gateway;

public partial class GatewaySetupPage : Page, INavigationAware
{
    private GatewayViewModel ViewModel => (GatewayViewModel)DataContext;

    public GatewaySetupPage(GatewayViewModel viewModel)
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

        ViewModel.BeginCreate();

        await ViewModel.LoadAsync();
    }


    private void Page_PreviewKeyDown(
        object sender,
        System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != System.Windows.Input.Key.Enter)
            return;

        if (DataContext is not GatewayViewModel viewModel)
            return;

        if (!viewModel.SaveGatewayCommand.CanExecute(null))
            return;

        viewModel.SaveGatewayCommand.Execute(null);

        e.Handled = true;
    }

    public void OnNavigatedTo(object parameter)
    {
        if (parameter is string value &&
            value == "OnBoarding")
        {
            ViewModel.SetOnboardingMode(true);
        }
    }



}
