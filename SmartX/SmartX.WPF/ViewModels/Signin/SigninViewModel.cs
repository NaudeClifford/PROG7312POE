using SmartX.Application.Authentication;
using SmartX.WPF.Services;
using SmartX.WPF.Services.Api;
using SmartX.WPF.ViewModels.Base;
using System.Windows.Input;

namespace SmartX.WPF.ViewModels;

public class SigninViewModel : ViewModelBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ISmartXApiClient _apiClient;
    private readonly SmartXSession _session;

    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isBusy;

    public string Email
    {
        get => _email;
        set
        {
            if (SetProperty(ref _email, value))
            {
                (SignInCommand as AsyncRelayCommand)
                    ?.RaiseCanExecuteChanged();
            }
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            if (SetProperty(ref _password, value))
            {
                (SignInCommand as AsyncRelayCommand)
                    ?.RaiseCanExecuteChanged();
            }
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                (SignInCommand as AsyncRelayCommand)
                    ?.RaiseCanExecuteChanged();
            }
        }
    }

    public ICommand SignInCommand { get; }

    public SigninViewModel(
        IAuthenticationService authenticationService,
        ISmartXApiClient apiClient,
        SmartXSession session)
    {
        _authenticationService = authenticationService;
        _apiClient = apiClient;
        _session = session;

        SignInCommand = new AsyncRelayCommand(
            SignInAsync,
            CanSignIn);
    }

    private bool CanSignIn()
    {
        return !IsBusy &&
               !string.IsNullOrWhiteSpace(Email) &&
               !string.IsNullOrWhiteSpace(Password);
    }

    private async Task SignInAsync()
    {
        ErrorMessage = string.Empty;
        IsBusy = true;

        try
        {
            // 1. Authenticate with Firebase
            var authenticationResult =
                await _authenticationService.SignInAsync(
                    Email,
                    Password);

            if (!authenticationResult.Success)
            {
                ErrorMessage =
                    authenticationResult.ErrorMessage ??
                    "Sign in failed.";

                return;
            }

            if (string.IsNullOrWhiteSpace(
                    authenticationResult.UserId))
            {
                ErrorMessage =
                    "Authentication succeeded, but no user ID was returned.";

                return;
            }

            // 2. Find the SmartX user using Firebase UID
            var user =
                await _apiClient.GetUserByFirebaseUidAsync(
                authenticationResult.UserId);

            if (user is null)
            {
                ErrorMessage =
                    "Your account is authenticated, but no SmartX user account was found.";

                return;
            }

            _session.SignIn(user);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}