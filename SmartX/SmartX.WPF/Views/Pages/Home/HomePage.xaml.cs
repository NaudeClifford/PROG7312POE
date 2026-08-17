using Microsoft.Extensions.DependencyInjection;
using SmartX.WPF.Services;
using SmartX.WPF.ViewModels.Home;
using SmartX.WPF.Views.Pages.Signin;
using System.Windows;
using System.Windows.Controls;

namespace SmartX.WPF.Views.Pages.Home;

public partial class HomePage : Page
{
    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
    }

    private void SignUpButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        MessageBox.Show(
            "Sign up will be added next.",
            "SmartX");
    }

    private void LoginButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var signinPage =
            App.ServiceProvider
                .GetRequiredService<SigninPage>();

        NavigationService?.Navigate(signinPage);
    }
}