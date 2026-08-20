using SmartX.Application.Authentication;
using SmartX.Application.Commands.Company;
using SmartX.Application.Commands.Users;
using SmartX.Domain.Enums;
using SmartX.WPF.Navigation;
using SmartX.WPF.Services;
using SmartX.WPF.Services.Api;
using SmartX.WPF.ViewModels.Base;
using SmartX.WPF.Views.Pages.Gateway;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SmartX.WPF.ViewModels.SignUp;

public class SignUpViewModel : INotifyPropertyChanged
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ISmartXApiClient _apiClient;
    private readonly SmartXSession _session;
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
    private bool _isBusy;

    public event PropertyChangedEventHandler? PropertyChanged;
    private readonly INavigationService _navigationService;
    public SignUpViewModel(
        IAuthenticationService authenticationService,
        ISmartXApiClient apiClient,
        SmartXSession session,
        INavigationService navigationService)
    {
        _authenticationService = authenticationService;
        _apiClient = apiClient;
        _session = session;
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

    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(
            ref _errorMessage,
            value);
    }

    public bool HasError
    {
        get => _hasError;
        private set => SetProperty(
            ref _hasError,
            value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetProperty(
                    ref _isBusy,
                    value))
                return;

            ((AsyncRelayCommand)ContinueCommand)
                .RaiseCanExecuteChanged();

            ((AsyncRelayCommand)CancelCommand)
                .RaiseCanExecuteChanged();
        }
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

            var command = new CreateCompanyCommand
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

            // Create SmartX user
            var userCommand = new CreateUserCommand
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

            var user =
                await _apiClient.GetUserByFirebaseUidAsync(
                    firebaseResult.UserId);

            if (user is null)
            {
                ShowError(
                    "The administrator was created, but the SmartX user could not be loaded.");

                return;
            }

            // Sign into SmartX session

            _session.SignIn(
                user,
                firebaseResult.IdToken,
                firebaseResult.RefreshToken);

            // Continue directly to Gateway Setup
            _navigationService.NavigateTo<GatewaySetupPage>();
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


    // PROPERTY HELPERS
    protected void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }

    private bool SetProperty<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(
                field,
                value))
        {
            return false;
        }

        field = value;

        OnPropertyChanged(propertyName);

        return true;
    }
}