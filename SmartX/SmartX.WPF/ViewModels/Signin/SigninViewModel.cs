using SmartX.Application.Authentication;
using SmartX.WPF.Navigation;
using SmartX.WPF.Services;
using SmartX.WPF.Services.Api;
using SmartX.WPF.Services.Sync;
using SmartX.WPF.ViewModels.Base;
using SmartX.WPF.Views.Pages.Gateway;
using SmartX.WPF.Views.Pages.Sensor;
using System.Net.Http;
using System.Windows.Input;

namespace SmartX.WPF.ViewModels;

public class SigninViewModel : ViewModelBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ISmartXApiClient _apiClient;
    private readonly SmartXSession _session;
    private readonly ICacheSyncService _cacheSyncService;
    private readonly INavigationService _navigationService;

    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isBusy;


    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
                OnPropertyChanged(nameof(HasError));
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(SignInButtonText));
            }
        }
    }

    private void EnterGuestMode()
    {
        _session.StartGuestSession("Guest");

        _navigationService.NavigateTo<GatewayPage>();
    }

    public ICommand SignInCommand { get; }
    public ICommand GuestCommand { get; }

    public SigninViewModel(
        IAuthenticationService authenticationService,
        ISmartXApiClient apiClient,
        SmartXSession session,
        ICacheSyncService cacheSyncService,
        INavigationService navigationService)
    {
        _authenticationService = authenticationService;
        _apiClient = apiClient;
        _session = session;
        _cacheSyncService = cacheSyncService;
        _navigationService = navigationService;

        SignInCommand = new AsyncRelayCommand(
            SignInAsync,
            CanSignIn);

        GuestCommand = new RelayCommand(_ => EnterGuestMode());
    }

    private bool CanSignIn()
    {
        return !IsBusy &&
               !string.IsNullOrWhiteSpace(Email) &&
               !string.IsNullOrWhiteSpace(Password);
    }

    private async Task SignInAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            var result =
                await _authenticationService.SignInAsync(
                    Email,
                    Password);

            if (!result.Success)
            {
                ErrorMessage =
                    result.ErrorMessage ??
                    "Login failed.";

                return;
            }

            if (string.IsNullOrWhiteSpace(result.UserId))
            {
                ErrorMessage =
                    "Firebase did not return a user ID.";

                return;
            }

            var user =
                await _apiClient.GetUserByFirebaseUidAsync(
                    result.UserId);

            if (user is null)
            {
                ErrorMessage =
                    "Your Firebase account is not registered in SmartX.";

                return;
            }

            if (!user.IsActive)
            {
                ErrorMessage =
                    "Your SmartX account is inactive.";

                return;
            }

            // Store authenticated user/session
            _session.SignIn(
                user,
                result.IdToken ?? string.Empty,
                result.RefreshToken ?? string.Empty);

            // Synchronize local cache
            await _cacheSyncService.SyncUserAsync(
                user.Id);

            await _cacheSyncService.SyncCompaniesAsync();

            await _cacheSyncService.SyncGatewaysAsync();

            await _cacheSyncService.SyncSensorsAsync();

            // Continue into the application
            _navigationService.NavigateTo<GatewayPage>();
        }
        catch (HttpRequestException)
        {
            ErrorMessage =
                "Unable to connect to the SmartX API.";
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

    public bool HasError =>
    !string.IsNullOrWhiteSpace(ErrorMessage);

    public string SignInButtonText =>
        IsBusy ? "Signing in..." : "Sign In";
}