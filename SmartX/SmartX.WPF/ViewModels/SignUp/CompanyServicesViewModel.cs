using SmartX.Application.Requests.Company;
using SmartX.WPF.Navigation;
using SmartX.WPF.Services.Api;
using SmartX.WPF.Services.Connectivity;
using SmartX.WPF.Services.Session;
using SmartX.WPF.ViewModels.Base;
using SmartX.WPF.Views.Pages.Gateway;
using System.Net.Http;

namespace SmartX.WPF.ViewModels.SignUp;

public class CompanyServicesViewModel : ViewModelBase
{
    private readonly ISmartXApiClient _apiClient;
    private readonly INavigationService _navigationService;

    // API

    private bool _useCustomApi;

    private string _apiBaseUrl = string.Empty;

    // FIREBASE

    private bool _useCustomFirebase;

    private string _firebaseProjectId = string.Empty;

    private string _firebaseApiKey = string.Empty;

    // STATE
    private bool _isOnboarding;

    private string _statusMessage = string.Empty;

    public CompanyServicesViewModel(
        ISmartXApiClient apiClient,
        INavigationService navigationService,
        IConnectivityService connectivityService,
        SmartXSession session)
        : base(
            connectivityService,
            session)
    {
        _apiClient = apiClient;
        _navigationService = navigationService;

        ContinueCommand =
            new AsyncRelayCommand(
                ContinueAsync,
                () => CanContinue);

        CancelCommand =
            new AsyncRelayCommand(
                CancelAsync,
                () => !IsBusy);
    }

    // API

    public bool UseCustomApi
    {
        get => _useCustomApi;

        set
        {
            if (!SetProperty(
                    ref _useCustomApi,
                    value))
            {
                return;
            }

            OnPropertyChanged(
                nameof(ShowCustomApiFields));

            RaiseCommandStates();
        }
    }
    public bool IsOnboarding
    {
        get => _isOnboarding;
        set => SetProperty(
            ref _isOnboarding,
            value);
    }

    public string ApiBaseUrl
    {
        get => _apiBaseUrl;

        set => SetProperty(
            ref _apiBaseUrl,
            value);
    }

    public bool ShowCustomApiFields =>
        UseCustomApi;

    // FIREBASE

    public bool UseCustomFirebase
    {
        get => _useCustomFirebase;

        set
        {
            if (!SetProperty(
                    ref _useCustomFirebase,
                    value))
            {
                return;
            }

            OnPropertyChanged(
                nameof(ShowCustomFirebaseFields));

            RaiseCommandStates();
        }
    }

    public string FirebaseProjectId
    {
        get => _firebaseProjectId;

        set => SetProperty(
            ref _firebaseProjectId,
            value);
    }

    public string FirebaseApiKey
    {
        get => _firebaseApiKey;

        set => SetProperty(
            ref _firebaseApiKey,
            value);
    }

    public bool ShowCustomFirebaseFields =>
        UseCustomFirebase;

    // STATE

    public string StatusMessage
    {
        get => _statusMessage;

        private set => SetProperty(
            ref _statusMessage,
            value);
    }

    public bool CanContinue =>
        !IsBusy &&
        Session.CompanyId != Guid.Empty;

    // COMMANDS

    public AsyncRelayCommand ContinueCommand { get; }

    public AsyncRelayCommand CancelCommand { get; }

    // LOAD

    public async Task LoadAsync(
        CancellationToken cancellationToken = default)
    {
        if (IsBusy)
            return;

        if (Session.CompanyId == Guid.Empty)
        {
            ErrorMessage =
                "No company is associated with this session.";

            return;
        }

        if (IsOnboarding)
            return;

        try
        {
            IsBusy = true;

            ErrorMessage = string.Empty;
            StatusMessage = string.Empty;

            var configuration =
                await _apiClient
                    .GetCompanyConfigurationAsync(
                        Session.CompanyId,
                        cancellationToken);

            if (configuration is null)
            {
                ResetConfiguration();

                return;
            }

            UseCustomApi =
                configuration.UseCustomApi;

            ApiBaseUrl =
                configuration.ApiBaseUrl ??
                string.Empty;

            UseCustomFirebase =
                configuration.UseCustomFirebase;

            FirebaseProjectId =
                configuration.FirebaseProjectId ??
                string.Empty;

            FirebaseApiKey =
                configuration.FirebaseApiKey ??
                string.Empty;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;

            RaiseCommandStates();
        }
    }

    // SAVE + CONTINUE
    private void ResetConfiguration()
    {
        UseCustomApi = false;
        ApiBaseUrl = string.Empty;

        UseCustomFirebase = false;
        FirebaseProjectId = string.Empty;
        FirebaseApiKey = string.Empty;
    }
    private async Task ContinueAsync()
    {
        if (!CanContinue)
            return;

        ClearMessages();

        if (!Validate())
            return;

        try
        {
            IsBusy = true;

            var request =
                new UpdateCompanyConfigurationRequest
                {
                    CompanyId =
                        Session.CompanyId,

                    UseCustomApi =
                        UseCustomApi,

                    ApiBaseUrl =
                        UseCustomApi
                            ? ApiBaseUrl.Trim()
                            : string.Empty,

                    UseCustomFirebase =
                        UseCustomFirebase,

                    FirebaseProjectId =
                        UseCustomFirebase
                            ? FirebaseProjectId.Trim()
                            : string.Empty,

                    FirebaseApiKey =
                        UseCustomFirebase
                            ? FirebaseApiKey.Trim()
                            : string.Empty
                };

            var success =
                await _apiClient
                    .UpdateCompanyConfigurationAsync(
                        request);

            if (!success)
            {
                ErrorMessage =
                    "The company service configuration could not be saved.";

                return;
            }

            StatusMessage =
                "Company services saved successfully.";

            _navigationService
                .NavigateTo<GatewaySetupPage>("OnBoarding");
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

            RaiseCommandStates();
        }
    }

    // CANCEL

    private async Task CancelAsync()
    {
        if (IsBusy)
            return;

        _navigationService
            .NavigateTo<GatewaySetupPage>();

        await Task.CompletedTask;
    }

    // VALIDATION

    private bool Validate()
    {
        if (Session.CompanyId == Guid.Empty)
        {
            ErrorMessage =
                "No company is associated with this session.";

            return false;
        }

        if (UseCustomApi &&
            string.IsNullOrWhiteSpace(ApiBaseUrl))
        {
            ErrorMessage =
                "API URL is required when using a custom API.";

            return false;
        }

        if (UseCustomFirebase &&
            string.IsNullOrWhiteSpace(FirebaseProjectId))
        {
            ErrorMessage =
                "Firebase Project ID is required when using custom Firebase.";

            return false;
        }

        return true;
    }

    private void ClearMessages()
    {
        ErrorMessage = string.Empty;
        StatusMessage = string.Empty;
    }

    // COMMAND STATES

    protected override void RaiseCommandStates()
    {
        ContinueCommand?.RaiseCanExecuteChanged();
        CancelCommand?.RaiseCanExecuteChanged();

        OnPropertyChanged(nameof(CanContinue));
        OnPropertyChanged(nameof(ShowCustomApiFields));
        OnPropertyChanged(nameof(ShowCustomFirebaseFields));
    }

    protected override void RaiseConnectivityState()
    {
        RaiseCommandStates();
    }

    protected override void OnSessionPropertyChanged(
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName ==
            nameof(SmartXSession.CompanyId))
        {
            RaiseCommandStates();
        }

        if (e.PropertyName ==
            nameof(SmartXSession.SelectedCompanyId))
        {
            RaiseCommandStates();
        }
    }
}
