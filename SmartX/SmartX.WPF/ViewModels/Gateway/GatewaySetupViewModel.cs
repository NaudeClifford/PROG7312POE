using SmartX.Application.Commands.Gateway;
using SmartX.WPF.Navigation;
using SmartX.WPF.Services;
using SmartX.WPF.Services.Api;
using SmartX.WPF.Services.Sync;
using SmartX.WPF.ViewModels.Base;
using SmartX.WPF.Views.Pages.Gateway;
using System.Net.Http;

namespace SmartX.WPF.ViewModels.Gateway;

public class GatewaySetupViewModel : ViewModelBase
{
    private readonly ISmartXApiClient _apiClient;
    private readonly SmartXSession _session;
    private readonly INavigationService _navigationService;
    private readonly ICacheSyncService _cacheSyncService;

    private string _name = string.Empty;
    private string _description = string.Empty;
    private string? _serialNumber;
    private string? _ipAddress;

    private bool _isBusy;
    private bool _isOnline;
    private bool _isCreated;

    private Guid? _gatewayId;

    private string _errorMessage = string.Empty;

    public GatewaySetupViewModel(
        ISmartXApiClient apiClient,
        SmartXSession session,
        INavigationService navigationService,
        ICacheSyncService cacheSyncService)
    {
        _apiClient = apiClient;
        _session = session;
        _navigationService = navigationService;
        _cacheSyncService = cacheSyncService;

        CreateGatewayCommand =
            new AsyncRelayCommand(
                CreateGatewayAsync,
                CanCreateGateway);
    }


    // =========================================================
    // PROPERTIES
    // =========================================================

    public string Name
    {
        get => _name;

        set
        {
            if (!SetProperty(ref _name, value))
                return;

            RaiseCommandStates();
        }
    }


    public string Description
    {
        get => _description;

        set => SetProperty(
            ref _description,
            value);
    }


    public string? SerialNumber
    {
        get => _serialNumber;

        set => SetProperty(
            ref _serialNumber,
            value);
    }


    public string? IpAddress
    {
        get => _ipAddress;

        set => SetProperty(
            ref _ipAddress,
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
            {
                return;
            }

            RaiseCommandStates();
        }
    }


    public bool IsOnline
    {
        get => _isOnline;

        private set
        {
            if (!SetProperty(
                    ref _isOnline,
                    value))
            {
                return;
            }

            RaiseCommandStates();
        }
    }


    public bool IsCreated
    {
        get => _isCreated;

        private set
        {
            if (!SetProperty(
                    ref _isCreated,
                    value))
            {
                return;
            }

            RaiseCommandStates();
        }
    }


    public Guid? GatewayId
    {
        get => _gatewayId;

        private set => SetProperty(
            ref _gatewayId,
            value);
    }


    public string ErrorMessage
    {
        get => _errorMessage;

        private set => SetProperty(
            ref _errorMessage,
            value);
    }


    // =========================================================
    // COMMAND
    // =========================================================

    public AsyncRelayCommand CreateGatewayCommand { get; }


    // =========================================================
    // LOAD
    // =========================================================

    public async Task LoadAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            IsOnline =
                await _apiClient.IsAvailableAsync(
                    cancellationToken);

            if (!IsOnline)
            {
                ErrorMessage =
                    "The SmartX API is currently unavailable.";

                return;
            }

            // Make sure the command is evaluated after
            // the API availability check.
            RaiseCommandStates();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            IsOnline = false;

            ErrorMessage =
                $"Unable to connect to the SmartX API: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            RaiseCommandStates();
        }
    }


    // =========================================================
    // CAN CREATE
    // =========================================================

    private bool CanCreateGateway()
    {
        return
            IsOnline &&
            !IsBusy &&
            !IsCreated &&
            !string.IsNullOrWhiteSpace(Name) &&
            _session.CompanyId != Guid.Empty;
    }


    // =========================================================
    // CREATE
    // =========================================================

    private async Task CreateGatewayAsync()
    {
        if (!CanCreateGateway())
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            var companyId = _session.CompanyId;

            if (companyId == Guid.Empty)
            {
                ErrorMessage =
                    "No company is associated with the current user.";

                return;
            }

            var command =
                new CreateGatewayCommand
                {
                    CompanyId = companyId,

                    Name = Name.Trim(),

                    Description =
                        Description.Trim(),

                    SerialNumber =
                        string.IsNullOrWhiteSpace(
                            SerialNumber)
                            ? null
                            : SerialNumber.Trim(),

                    IpAddress =
                        string.IsNullOrWhiteSpace(
                            IpAddress)
                            ? null
                            : IpAddress.Trim(),

                    IsActive = true
                };


            // -------------------------------------------------
            // CREATE ON API
            // -------------------------------------------------

            var gatewayId =
                await _apiClient.CreateGatewayAsync(
                    command);


            if (gatewayId == Guid.Empty)
            {
                ErrorMessage =
                    "The gateway could not be created.";

                return;
            }


            GatewayId = gatewayId;


            // -------------------------------------------------
            // REFRESH LOCAL CACHE
            // -------------------------------------------------

            await _cacheSyncService.SyncGatewaysAsync();


            // -------------------------------------------------
            // MARK CREATED
            // -------------------------------------------------

            IsCreated = true;


            // -------------------------------------------------
            // NAVIGATE
            // -------------------------------------------------
            _session.SelectGateway(
            gatewayId,
            Name.Trim());

            _navigationService.NavigateTo<GatewayPage>();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            IsOnline = false;

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


    // =========================================================
    // COMMAND STATE
    // =========================================================

    private void RaiseCommandStates()
    {
        CreateGatewayCommand
            .RaiseCanExecuteChanged();
    }
}