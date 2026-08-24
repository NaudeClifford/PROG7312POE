using SmartX.WPF.Navigation;
using SmartX.WPF.Services;
using SmartX.WPF.ViewModels.Base;
using SmartX.WPF.Views.Pages.Gateway;
using SmartX.WPF.Views.Pages.Signin;
using SmartX.WPF.Views.Pages.SignUp;
using System.Windows.Input;

namespace SmartX.WPF.ViewModels
{
    public class HomeViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;
        private readonly SmartXSession _session;

        public ICommand SignInCommand { get; }
        public ICommand SignUpCommand { get; }
        public ICommand GuestCommand { get; }

        public HomeViewModel(
            INavigationService navigationService,
            SmartXSession session)
        {
            _navigationService = navigationService;
            _session = session;

            SignInCommand = new RelayCommand(
                NavigateToSignIn);

            SignUpCommand = new RelayCommand(
                NavigateToSignUp);

            GuestCommand = new RelayCommand(
                EnterGuestMode);
        }

        private void NavigateToSignIn(object? parameter)
        {
            _navigationService.NavigateTo<SigninPage>();
        }

        private void NavigateToSignUp(object? parameter)
        {
            _navigationService.NavigateTo<SignUpPage>();
        }

        private void EnterGuestMode(object? parameter)
        {
            _session.StartGuestSession("Guest");

            _navigationService.NavigateTo<GatewaySetupPage>();
        }
    }
}