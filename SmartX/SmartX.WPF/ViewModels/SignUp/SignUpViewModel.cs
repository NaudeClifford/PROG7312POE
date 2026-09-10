using SmartX.Application.Authentication;
using SmartX.Application.Requests.Company;
using SmartX.WPF.Navigation;
using SmartX.WPF.Services.Api;
using SmartX.WPF.Services.Connectivity;
using SmartX.WPF.Services.Session;
using SmartX.WPF.ViewModels.Base;
using SmartX.WPF.Views.Pages.Gateway;
using SmartX.WPF.Views.Pages.SignUp;
using System.Net.Http;

namespace SmartX.WPF.ViewModels.SignUp;

public class SignUpViewModel : ViewModelBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ISmartXApiClient _apiClient;
    private readonly INavigationService _navigationService;

    private int _currentStep = 1;

    private string _companyName = string.Empty;
    private string _companyDescription = string.Empty;

    private string _displayName = string.Empty;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _confirmPassword = string.Empty;

    private bool _hasError;
    private bool _isGuestSignup;

    private const int CompanyStep = 1;
    private const int AdministratorStep = 2;
    private const int ServicesStep = 3;
    private const int GatewayStep = 4;

    private static readonly Guid GuestUserId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static readonly Guid GuestCompanyId =
        Guid.Parse("22222222-2222-2222-2222-222222222222");

    public SignUpViewModel(
        IAuthenticationService authenticationService,
        ISmartXApiClient apiClient,
        SmartXSession session,
        INavigationService navigationService,
        IConnectivityService connectivityService)
        : base(connectivityService, session)
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

        BackCommand = new AsyncRelayCommand(
            BackAsync,
            () => CanGoBack);
    }

    public bool IsGuestSignup
    {
        get => _isGuestSignup;
        private set => SetProperty(
            ref _isGuestSignup,
            value);
    }

    public void SetGuestSignupMode(bool isGuestSignup)
    {
        IsGuestSignup = isGuestSignup;
    }

    public int CurrentStep
    {
        get => _currentStep;
        private set
        {
            if (_currentStep == value)
                return;

            _currentStep = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(IsCompanyStep));
            OnPropertyChanged(nameof(IsAdministratorStep));
            OnPropertyChanged(nameof(IsServicesStep));
            OnPropertyChanged(nameof(IsGatewayStep));
            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(StepTitle));
            OnPropertyChanged(nameof(ButtonText));

            BackCommand.RaiseCanExecuteChanged();
            ContinueCommand.RaiseCanExecuteChanged();
        }
    }

    public bool IsCompanyStep =>
        CurrentStep == CompanyStep;

    public bool IsAdministratorStep =>
        CurrentStep == AdministratorStep;

    public bool IsServicesStep =>
        CurrentStep == ServicesStep;

    public bool IsGatewayStep =>
        CurrentStep == GatewayStep;

    public bool CanGoBack =>
        CurrentStep > CompanyStep && !IsBusy;

    public string StepTitle =>
        CurrentStep switch
        {
            CompanyStep => "Create your company",
            AdministratorStep => "Create your administrator account",
            ServicesStep => "Configure company services",
            GatewayStep => "Set up your gateway",
            _ => string.Empty
        };

    public string ButtonText =>
        CurrentStep switch
        {
            CompanyStep => "Continue",
            AdministratorStep => "Create Administrator",
            ServicesStep => "Continue",
            GatewayStep => "Create Gateway",
            _ => "Continue"
        };

    public string CompanyName
    {
        get => _companyName;
        set => SetProperty(
            ref _companyName,
            value);
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

    public AsyncRelayCommand ContinueCommand { get; }

    public AsyncRelayCommand CancelCommand { get; }

    public AsyncRelayCommand BackCommand { get; }

    private async Task ContinueAsync()
    {
        ClearError();

        switch (CurrentStep)
        {
            case CompanyStep:
                ContinueFromCompanyStep();
                break;

            case AdministratorStep:
                if (IsGuestSignup)
                {
                    StartGuestSession();
                }
                else
                {
                    await RegisterAsync();
                }

                break;

            case ServicesStep:
                await ContinueToGatewayAsync();
                break;

            case GatewayStep:
                break;
        }
    }

    private void ContinueFromCompanyStep()
    {
        if (string.IsNullOrWhiteSpace(CompanyName))
        {
            ShowError(
                "Please enter your company name.");

            return;
        }

        CurrentStep = AdministratorStep;
    }

    private void StartGuestSession()
    {
        try
        {
            IsBusy = true;
            ClearError();

            var userName =
                string.IsNullOrWhiteSpace(DisplayName)
                    ? "Demo Administrator"
                    : DisplayName.Trim();

            var companyName =
                string.IsNullOrWhiteSpace(CompanyName)
                    ? "SmartX Demo Company"
                    : CompanyName.Trim();

            if(Session.FirebaseUid == null || Session.IdToken == null || Session.RefreshToken == null)
            {
                throw new InvalidOperationException(
                    "Session information is incomplete.");
            }

            Session.StartGuestSession(
                GuestUserId,
                GuestCompanyId,
                userName,
                companyName,
                Session.FirebaseUid,
                Session.IdToken,
                Session.RefreshToken);


            CurrentStep = ServicesStep;

            _navigationService.NavigateTo<CompanyServicesPage>(
                "OnBoarding");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                ex.ToString());

            ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RegisterAsync()
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

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

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
                throw new InvalidOperationException(
                    "Firebase did not return a user ID.");
            }

            if (string.IsNullOrWhiteSpace(firebaseResult.IdToken))
            {
                throw new InvalidOperationException(
                    "Firebase did not return an ID token.");
            }

            if (string.IsNullOrWhiteSpace(firebaseResult.RefreshToken))
            {
                throw new InvalidOperationException(
                    "Firebase did not return a refresh token.");
            }

            var registrationRequest =
                new RegisterCompanyRequest
                {
                    CompanyName =
                        CompanyName.Trim(),

                    Description =
                        CompanyDescription.Trim(),

                    DisplayName =
                        DisplayName.Trim(),

                    IdToken =
                        firebaseResult.IdToken
                };

            var registration =
                await _apiClient.RegisterCompanyAsync(
                    registrationRequest);

            if (registration.CompanyId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "Registration did not return a company ID.");
            }

            if (registration.User is null)
            {
                throw new InvalidOperationException(
                    "Registration did not return the administrator.");
            }

            Session.SignIn(
                registration.User,
                CompanyName.Trim(),
                firebaseResult.IdToken,
                firebaseResult.RefreshToken);

            CurrentStep = ServicesStep;

            _navigationService.NavigateTo<CompanyServicesPage>(
                "OnBoarding");
        }
        catch (HttpRequestException)
        {
            ShowError(
                "Unable to connect to the SmartX API.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                ex.ToString());

            ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ContinueToGatewayAsync()
    {
        if (Session.CompanyId == Guid.Empty)
        {
            ShowError(
                "No company is associated with this session.");

            return;
        }

        CurrentStep = GatewayStep;

        _navigationService.NavigateTo<GatewaySetupPage>(
            "OnBoarding");

        await Task.CompletedTask;
    }

    private async Task BackAsync()
    {
        if (!CanGoBack)
            return;

        ClearError();

        switch (CurrentStep)
        {
            case AdministratorStep:
                CurrentStep = CompanyStep;
                break;

            case ServicesStep:
                CurrentStep = AdministratorStep;
                break;

            case GatewayStep:
                CurrentStep = ServicesStep;

                _navigationService.NavigateTo<CompanyServicesPage>();
                break;
        }

        await Task.CompletedTask;
    }

    private async Task CancelAsync()
    {
        ClearError();

        CurrentStep = CompanyStep;

        await Task.CompletedTask;
    }

    private void ClearError()
    {
        HasError = false;
        ErrorMessage = string.Empty;
    }

    private void ShowError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }
}
