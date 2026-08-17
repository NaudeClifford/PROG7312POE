using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using SmartX.WPF.Authentication;

namespace SmartX.WPF.Views.Pages.Home;

public partial class HomePage : Page
{
    public HomePage()
    {
        InitializeComponent();
    }

    private void SignUpButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Sign up will be added next.",
            "SmartX");
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        var signinPage =
            App.ServiceProvider.GetRequiredService<Signin.SigninPage>();

        NavigationService?.Navigate(signinPage);
    }
}