using SmartX.WPF.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace SmartX.WPF.Views.Pages.Signin
{
    public partial class SigninPage : Page
    {
        public SigninPage(SigninViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;
        }

        private void PasswordBox_PasswordChanged(
        object sender,
        RoutedEventArgs e)
        {
            if (DataContext is SigninViewModel viewModel &&
                sender is PasswordBox passwordBox)
            {
                viewModel.Password = passwordBox.Password;
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
}