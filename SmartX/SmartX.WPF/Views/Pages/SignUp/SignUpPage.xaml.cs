using SmartX.WPF.ViewModels.SignUp;
using System.Windows;
using System.Windows.Controls;

namespace SmartX.WPF.Views.Pages.SignUp;

public partial class SignUpPage : Page
{
    private readonly SignUpViewModel _viewModel;

    public SignUpPage(
        SignUpViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;

        DataContext = _viewModel;
    }

    private void PasswordBox_PasswordChanged(
        object sender,
        RoutedEventArgs e)
    {
        _viewModel.Password =
            PasswordBox.Password;
    }

    private void ConfirmPasswordBox_PasswordChanged(
        object sender,
        RoutedEventArgs e)
    {
        _viewModel.ConfirmPassword =
            ConfirmPasswordBox.Password;
    }
}