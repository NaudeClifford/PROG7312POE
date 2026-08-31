using SmartX.Application.Authentication;
using SmartX.Application.Requests.Company;
using SmartX.Application.Requests.User;
using SmartX.Domain.Enums;
using SmartX.Shared.Models;
using SmartX.WPF.Navigation;
using SmartX.WPF.Services.Api;
using SmartX.WPF.Services.Connectivity;
using SmartX.WPF.Services.Session;
using SmartX.WPF.ViewModels.Base;
using SmartX.WPF.Views.Pages.Gateway;
using System.Windows.Input;

namespace SmartX.WPF.ViewModels.SignUp;

public class SignUpViewModel : ViewModelBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ISmartXApiClient _apiClient;

    private Guid _companyId;
    private int _currentStep = 1;
    private string _companyName = string.Empty;
    private string _companyDescription = string.Empty;

    private string _displayName = string.Empty;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _confirmPassword = string.Empty;

    private string _errorMessage = string.Empty;
    private bool _hasError;

    private readonly INavigationService _navigationService;
    public SignUpViewModel(
        IAuthenticationService authenticationService,
        ISmartXApiClient apiClient,
        SmartXSession session,
        INavigationService navigationService,
        IConnectivityService connectivityService) : base(connectivityService, session)
    {
        _authenticationService = authenticationService;
        _apiClient = apiClient;
        _navigationService = navigationService;

        ContinueCommand = new AsyncRelayCommand(
            ContinueAsync,
            () => !IsBusy);

        CancelCommand = new AsyncRelayCommand(
            CancelAsync,
            () => !IsBusy);
    }

    // PROPERTIES

    public int CurrentStep
    {
        get => _currentStep;
        set
        {
            if (_currentStep == value)
                return;

            _currentStep = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(IsCompanyStep));
            OnPropertyChanged(nameof(IsAdministratorStep));
            OnPropertyChanged(nameof(StepTitle));
            OnPropertyChanged(nameof(ButtonText));
        }
    }

    public string CompanyName
    {
        get => _companyName;
        set => SetProperty(ref _companyName, value);
    }

    public string CompanyDescription
    {
        get => _companyDescription;
        set => SetProperty(
            ref _companyDescription,
            value);
    }

    public string DisplayName
    {
        get => _displayName;
        set => SetProperty(
            ref _displayName,
            value);
    }

    public string Email
    {
        get => _email;
        set => SetProperty(
            ref _email,
            value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(
            ref _password,
            value);
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set => SetProperty(
            ref _confirmPassword,
            value);
    }

    public bool HasError
    {
        get => _hasError;
        private set => SetProperty(
            ref _hasError,
            value);
    }

    public bool IsCompanyStep =>
        CurrentStep == 1;

    public bool IsAdministratorStep =>
        CurrentStep == 2;

    public string StepTitle =>
        CurrentStep == 1
            ? "Create your company"
            : "Create your administrator account";

    public string ButtonText =>
        CurrentStep == 1
            ? "Continue"
            : "Create Administrator";

    public ICommand ContinueCommand { get; }

    public ICommand CancelCommand { get; }


    // FLOW
    private async Task ContinueAsync()
    {
        ClearError();

        if (CurrentStep == 1)
        {
            await CreateCompanyAsync();
            return;
        }

        await CreateAdministratorAsync();
    }


    // CREATE COMPANY

    private async Task CreateCompanyAsync()
    {
        if (string.IsNullOrWhiteSpace(CompanyName))
        {
            ShowError(
                "Please enter your company name.");

            return;
        }

        try
        {
            IsBusy = true;

            var command = new CreateCompanyRequest
            {
                Name = CompanyName.Trim(),
                Description = CompanyDescription.Trim()
            };

            _companyId =
                await _apiClient.CreateCompanyAsync(
                    command);

            if (_companyId == Guid.Empty)
            {
                ShowError(
                    "The company was not created.");

                return;
            }

            CurrentStep = 2;
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }


    // CREATE FIREBASE + SMARTX ADMINISTRATOR
    private async Task CreateAdministratorAsync()
    {
        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            ShowError(
                "Please enter your name.");

            return;
        }

        if (string.IsNullOrWhiteSpace(Email))
        {
            ShowError(
                "Please enter your email address.");

            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            ShowError(
                "Please enter a password.");

            return;
        }

        if (Password != ConfirmPassword)
        {
            ShowError(
                "Passwords do not match.");

            return;
        }

        if (_companyId == Guid.Empty)
        {
            ShowError(
                "Company setup has expired. Please start again.");

            CurrentStep = 1;

            return;
        }

        try
        {
            IsBusy = true;

            // Create Firebase account

            var firebaseResult =
                await _authenticationService.SignUpAsync(
                    Email.Trim(),
                    Password);

            if (!firebaseResult.Success)
            {
                ShowError(
                    firebaseResult.ErrorMessage ??
                    "Unable to create the administrator account.");

                return;
            }
            if (string.IsNullOrWhiteSpace(firebaseResult.UserId))
            {
                throw new InvalidOperationException(" UserId is missing.");
            }

            // Create SmartX user
            var userCommand = new CreateUserRequest
            {
                CompanyId = _companyId,

                FirebaseUid = firebaseResult.UserId,

                Email =
                    firebaseResult.Email ??
                    Email.Trim(),

                DisplayName =
                    DisplayName.Trim(),

                Role = UserRole.Administrator,

                IsActive = true
            };

            var userId =
                await _apiClient.CreateUserAsync(
                    userCommand);

            if (userId == Guid.Empty)
            {
                ShowError(
                    "The administrator account could not be created.");

                return;
            }

            // Load created SmartX user

            if (string.IsNullOrWhiteSpace(firebaseResult.UserId))
            {
                ErrorMessage =
                    "Firebase did not return a user ID.";

                return;
            }

            if (string.IsNullOrWhiteSpace(firebaseResult.IdToken))
            {
                ErrorMessage =
                    "Firebase did not return an ID token.";

                return;
            }

            var user =
                await _apiClient.GetUserByFirebaseUidAsync(
                    firebaseResult.UserId, firebaseResult.IdToken);

            if (user is null)
            {
                ShowError(
                    "The administrator was created, but the SmartX user could not be loaded.");

                return;
            }

            // Sign into SmartX session

                if (string.IsNullOrWhiteSpace(firebaseResult.RefreshToken))
                {
                    throw new InvalidOperationException("Refresh token is missing.");
                }

                if (string.IsNullOrWhiteSpace(firebaseResult.IdToken))
                {
                    throw new InvalidOperationException("IdToken is missing.");
                }

                Session.SignIn(
                    user,
                    firebaseResult.IdToken,
                    firebaseResult.RefreshToken);

            // Continue directly to Gateway Setup
            _navigationService.NavigateTo<GatewaySetupPage>();
        }
        catch (InvalidOperationException ex)
        {
            ShowError(
                $"Unable to sign in: {ex.Message}");
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }


    // CANCEL COMPANY SETUP
    private async Task CancelAsync()
    {
        if (_companyId == Guid.Empty)
            return;

        try
        {
            IsBusy = true;

            await _apiClient.DeleteCompanyAsync(
                _companyId);

            _companyId = Guid.Empty;

            CurrentStep = 1;
        }
        catch (Exception ex)
        {
            ShowError(
                $"Unable to cancel company setup: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }


    // ERROR HANDLING

    private void ClearError()
    {
        HasError = false;
        ErrorMessage = string.Empty;
    }

    private void ShowError(
        string message)
    {
        ErrorMessage = message;
        HasError = true;
    }
}