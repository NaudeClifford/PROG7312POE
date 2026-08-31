using SmartX.WPF.Navigation;
using SmartX.WPF.Services.Connectivity;
using SmartX.WPF.Services.Session;
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

        public ICommand SignInCommand { get; }
        public ICommand SignUpCommand { get; }
        public ICommand GuestCommand { get; }

        private string title = "Smart X";
        private string subtitle = "Slogon";
        private string description = "Description";

        public string Title { get => title; set => SetProperty(ref title, value); }

        public string Subtitle { get => subtitle; set => SetProperty(ref subtitle, value); }

        public string Description { get => description; set => SetProperty(ref description, value); }

        public HomeViewModel(
            INavigationService navigationService,
            SmartXSession session,
            IConnectivityService connectivityService) : base(connectivityService, session)
        {
            _navigationService = navigationService;

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
            Session.StartGuestSession("Guest");

            _navigationService.NavigateTo<GatewaySetupPage>();
        }





    }
}