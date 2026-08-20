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
    }
}