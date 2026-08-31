using SmartX.Application.Authentication;
using SmartX.WPF.Navigation;
using SmartX.WPF.Services.Api;
using SmartX.WPF.Services.Connectivity;
using SmartX.WPF.Services.Session;
using SmartX.WPF.Services.Sync;
using SmartX.WPF.ViewModels.Base;
using SmartX.WPF.Views.Pages.Gateway;
using System.Net.Http;
using System.Windows.Input;

namespace SmartX.WPF.ViewModels;

public class SigninViewModel : ViewModelBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ISmartXApiClient _apiClient;
    private readonly ICacheSyncService _cacheSyncService;
    private readonly INavigationService _navigationService;
    private readonly SmartXCredentialStore _credentialStore;
    private string _email = string.Empty;
    private string _password = string.Empty;

    private bool _rememberMe;

    public bool RememberMe
    {
        get => _rememberMe;
        set
        {
            if (_rememberMe == value)
                return;

            _rememberMe = value;
            OnPropertyChanged();
        }
    }

    public string Email
    {
        get => _email;
        set
        {
            if (!SetProperty(ref _email, value))
                return;

            SignInCommand.RaiseCanExecuteChanged();
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            if (!SetProperty(ref _password, value))
                return;

            SignInCommand.RaiseCanExecuteChanged();
        }
    }

    private void EnterGuestMode()
    {
        Session.StartGuestSession("Guest");

        _navigationService.NavigateTo<GatewayPage>();
    }

    public AsyncRelayCommand SignInCommand { get; }
    public ICommand GuestCommand { get; }

    public SigninViewModel(
        IAuthenticationService authenticationService,
        ISmartXApiClient apiClient,
        SmartXSession session,
        ICacheSyncService cacheSyncService,
        INavigationService navigationService,
        IConnectivityService connectivityService,
        SmartXCredentialStore credentialStore) : base(connectivityService, session)
    {
        _authenticationService = authenticationService;
        _apiClient = apiClient;
        _cacheSyncService = cacheSyncService;
        _navigationService = navigationService;
        _credentialStore = credentialStore;

        SignInCommand = new AsyncRelayCommand(
            SignInAsync,
            CanSignIn);

        GuestCommand = new RelayCommand(
            _ => EnterGuestMode());
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

            if (string.IsNullOrWhiteSpace(result.IdToken))
            {
                ErrorMessage =
                    "Firebase did not return an ID token.";

                return;
            }

            var user =
                await _apiClient.GetUserByFirebaseUidAsync(
                    result.UserId, result.IdToken);


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
            Session.SignIn(
                user,
                result.IdToken ?? string.Empty,
                result.RefreshToken ?? string.Empty);

            if (RememberMe &&
                !string.IsNullOrWhiteSpace(result.RefreshToken))
            {
                await _credentialStore.SaveAsync(
                    result.RefreshToken);
            }
            else
            {
                await _credentialStore.DeleteAsync();
            }

            // Synchronize local cache
            await _cacheSyncService.SyncUserAsync(
                user.Id);

            if (user.CompanyId != Guid.Empty)
            {
                await _cacheSyncService.SyncCompanyAsync(
                    user.CompanyId);

                await _cacheSyncService.SyncGatewaysAsync(
                    user.CompanyId);
            }

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
            System.Diagnostics.Debug.WriteLine(ex.ToString());

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