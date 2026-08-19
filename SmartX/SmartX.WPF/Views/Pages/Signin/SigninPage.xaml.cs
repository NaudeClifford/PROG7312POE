using System.Windows.Controls;
using SmartX.WPF.ViewModels;

namespace SmartX.WPF.Views.Pages.Signin
{
    public partial class SigninPage : Page
    {
        public SigninPage(SigninViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;
        }
    }
}