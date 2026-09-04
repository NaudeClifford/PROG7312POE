using SmartX.WPF.ViewModels.SignUp;
using System.Windows.Controls;

namespace SmartX.WPF.Views.Pages.SignUp;

public partial class CompanyServicesPage : Page
{
    private readonly CompanyServicesViewModel _viewModel;


    public CompanyServicesPage(
        CompanyServicesViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;

        DataContext = _viewModel;

        Loaded += CompanyServicesPage_Loaded;
    }

    private async void CompanyServicesPage_Loaded(
        object sender,
        System.Windows.RoutedEventArgs e)
    {
        Loaded -= CompanyServicesPage_Loaded;

        await _viewModel.LoadAsync();
    }

    public void OnNavigatedTo(object? parameter)
    {
        _viewModel.IsOnboarding =
            parameter?.ToString() == "OnBoarding";
    }

}
