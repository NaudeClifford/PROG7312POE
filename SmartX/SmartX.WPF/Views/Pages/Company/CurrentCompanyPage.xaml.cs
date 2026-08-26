using SmartX.WPF.ViewModels.Pages.Company;
using System.Windows.Controls;

namespace SmartX.WPF.Views.Pages.Company;

public partial class CurrentCompanyPage : Page
{
    public CurrentCompanyPage(
        CompanyViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;

        Loaded += CurrentCompanyPage_Loaded;
    }

    private async void CurrentCompanyPage_Loaded(
        object sender,
        System.Windows.RoutedEventArgs e)
    {
        if (DataContext is CompanyViewModel viewModel)
        {
            await viewModel.LoadAsync();
        }
    }
}