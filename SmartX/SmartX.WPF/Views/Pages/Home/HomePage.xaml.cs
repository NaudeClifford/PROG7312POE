using SmartX.WPF.ViewModels;
using System.Windows.Controls;

namespace SmartX.WPF.Views.Pages.Home;

public partial class HomePage : Page
{
    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
    }
}