using SmartX.WPF.ViewModels.Pages.Company;
using System.Windows.Controls;

namespace SmartX.WPF.Views.Pages.Company;

public partial class CompaniesPage : Page
{
    public CompaniesPage(
        CompanyViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;

        Loaded += CompanyPage_Loaded;
    }

    private async void CompanyPage_Loaded(
        object sender,
        System.Windows.RoutedEventArgs e)
    {
        if (DataContext is CompanyViewModel viewModel)
        {
            await viewModel.LoadAsync();
        }
    }
}