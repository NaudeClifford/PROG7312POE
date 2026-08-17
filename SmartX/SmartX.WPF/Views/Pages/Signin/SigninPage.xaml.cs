using SmartX.WPF.ViewModels.Signin;
using System.Windows;
using System.Windows.Controls;

namespace SmartX.WPF.Views.Pages.Signin;

public partial class SigninPage : Page
{
    private readonly SigninViewModel _viewModel;

    public SigninPage(SigninViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;

        DataContext = _viewModel;

        _viewModel.SignInSucceeded +=
            ViewModel_SignInSucceeded;
    }

    private void PasswordBox_PasswordChanged(
        object sender,
        RoutedEventArgs e)
    {
        _viewModel.Password =
            PasswordBox.Password;
    }

    private void ViewModel_SignInSucceeded(
        object? sender,
        EventArgs e)
    {
        MessageBox.Show(
            $"Welcome {_viewModel.Email}!",
            "SmartX",
            MessageBoxButton.OK,
            MessageBoxImage.Information);

        // Navigation to MainWindow/authenticated shell
        // will be connected here once the authentication
        // flow is completed.
    }
}