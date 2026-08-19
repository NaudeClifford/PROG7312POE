using SmartX.WPF.Navigation;
using SmartX.WPF.ViewModels.Base;
using SmartX.WPF.Views.Pages;
using SmartX.WPF.Views.Pages.Signin;
using System.Windows.Input;

namespace SmartX.WPF.ViewModels
{
    public class HomeViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;

        public ICommand SignInCommand { get; }

        public HomeViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;

            SignInCommand = new RelayCommand(
                NavigateToSignIn);
        }

        private void NavigateToSignIn(object? parameter)
        {
            _navigationService.NavigateTo<SigninPage>();
        }
    }
}