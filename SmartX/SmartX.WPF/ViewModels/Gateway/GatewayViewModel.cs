using FluentValidation;
using SmartX.Application.Requests.Gateway;
using SmartX.Domain.Enums;
using SmartX.Shared.DTOs;
using SmartX.WPF.Navigation;
using SmartX.WPF.Services.Api;
using SmartX.WPF.Services.Connectivity;
using SmartX.WPF.Services.Session;
using SmartX.WPF.Services.Sync;
using SmartX.WPF.ViewModels.Base;
using SmartX.WPF.Views.Pages.Gateway;
using SmartX.WPF.Views.Pages.History;
using SmartX.WPF.Views.Pages.Network;
using SmartX.WPF.Views.Pages.Sensor;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Windows;

namespace SmartX.WPF.ViewModels.Gateway;

public class GatewayViewModel : ViewModelBase
{
    private readonly ISmartXApiClient _apiClient;
    private readonly INavigationService _navigationService;
    private readonly ICacheSyncService _cacheSyncService;

    private readonly IValidator<CreateGatewayRequest> _createGatewayValidator;

    // MODE

    public enum GatewayMode
    {
        List,
        Create,
        Edit
    }

    private GatewayMode _mode = GatewayMode.List;

    public GatewayMode Mode
    {
        get => _mode;
        private set
        {
            if (_mode == value)
                return;

            _mode = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(IsListMode));
            OnPropertyChanged(nameof(IsCreateMode));
            OnPropertyChanged(nameof(IsEditMode));

            RaiseCommandStates();
        }
    }

    private bool _isOnboarding;

    public bool IsOnboarding
    {
        get => _isOnboarding;
        private set => SetProperty(
            ref _isOnboarding,
            value);
    }

    public void BeginCreate()
    {
        ClearEditor();
        
        IsOnboarding = false;
        Mode = GatewayMode.Create;

        RaiseCommandStates();
    }

    public void SetOnboardingMode(bool isOnboarding)
    {
        IsOnboarding = isOnboarding;
    }


    public bool IsListMode =>
        Mode == GatewayMode.List;

    public bool IsCreateMode =>
        Mode == GatewayMode.Create;

    public bool IsEditMode =>
        Mode == GatewayMode.Edit;

    // GATEWAYS

    public ObservableCollection<GatewayDto> Gateways { get; } = [];

    private GatewayDto? _gateway;

    public GatewayDto? Gateway
    {
        get => _gateway;

        private set
        {
            if (_gateway == value)
                return;

            _gateway = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(HasGateway));
            OnPropertyChanged(nameof(HasNoGateway));

            RaiseCommandStates();
        }
    }

    public bool HasGateway =>
        Gateway is not null;

    public bool HasNoGateway =>
        Gateway is null;

    // SELECTED GATEWAY

    private GatewayDto? _selectedGateway;

    public GatewayDto? SelectedGateway
    {
        get => _selectedGateway;

        set
        {
            if (_selectedGateway == value)
                return;

            _selectedGateway = value;

            OnPropertyChanged();

            if (_selectedGateway is not null)
            {
                Gateway = _selectedGateway;

                Session.SelectGateway(
                    _selectedGateway.Id,
                    _selectedGateway.Name);
            }
            else
            {
                Gateway = null;

                Session.ClearGateway();
            }

            OnPropertyChanged(nameof(SelectedGatewayName));
            OnPropertyChanged(nameof(HasSelectedGateway));

            RaiseCommandStates();
        }
    }

    public string? SelectedGatewayName =>
        Session.GatewayName;

    public bool HasSelectedGateway =>
        Session.GatewayId.HasValue;

    // CREATE / EDIT FIELDS

    private Guid? _editingGatewayId;

    public Guid? EditingGatewayId
    {
        get => _editingGatewayId;

        private set
        {
            if (SetProperty(
                    ref _editingGatewayId,
                    value))
            {
                RaiseCommandStates();
            }
        }
    }

    private string _name = string.Empty;

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

    private string _description = string.Empty;

    public string Description
    {
        get => _description;

        set
        {
            if (!SetProperty(ref _description, value))
                return;

            RaiseCommandStates();
        }
    }

    private string? _serialNumber;

    public string? SerialNumber
    {
        get => _serialNumber;

        set
        {
            if (!SetProperty(ref _serialNumber, value))
                return;

            RaiseCommandStates();
        }
    }

    private string? _ipAddress;

    public string? IpAddress
    {
        get => _ipAddress;

        set
        {
            if (!SetProperty(ref _ipAddress, value))
                return;

            RaiseCommandStates();
        }
    }

    private bool _isActive = true;

    public bool IsActive
    {
        get => _isActive;

        set
        {
            if (!SetProperty(ref _isActive, value))
                return;

            RaiseCommandStates();
        }
    }

    // STATE
    private string? _statusMessage;

    public string? StatusMessage
    {
        get => _statusMessage;

        private set => SetProperty(
            ref _statusMessage,
            value);
    }

    // VISIBILITY

    public Visibility GatewayCrudVisibility =>
        IsAdministrator
            ? Visibility.Visible
            : Visibility.Collapsed;

    public Visibility GatewayFeaturesVisibility =>
        HasGateway
            ? Visibility.Visible
            : Visibility.Collapsed;

    public Visibility NoGatewayVisibility =>
        HasNoGateway
            ? Visibility.Visible
            : Visibility.Collapsed;

    // PERMISSIONS

    public bool IsAdministrator =>
        Session.Role == UserRole.Administrator;

    public bool IsTechnician =>
        Session.Role == UserRole.Technician;

    public bool IsSuperAdmin =>
        Session.Role == UserRole.SuperAdmin;

    public bool CanAddGateway =>
        !IsBusy &&
        IsOnline &&
        Session.CompanyId != Guid.Empty &&
        Session.Role == UserRole.Administrator &&
        IsListMode;

    public bool CanEditGateway =>
        !IsBusy &&
        IsOnline &&
        Gateway is not null &&
        Session.Role == UserRole.Administrator;

    public bool CanDeleteGateway =>
        !IsBusy &&
        IsOnline &&
        Gateway is not null &&
        Session.Role == UserRole.Administrator;

    public bool CanOpenGatewayArea =>
        !IsBusy &&
        IsOnline &&
        Gateway is not null &&
        Session.Role is
            UserRole.Administrator or
            UserRole.Technician;

    // SAVE PERMISSION

    public bool CanSaveGateway
    {
        get
        {
            if (IsBusy)
                return false;

            if (!IsOnline)
                return false;

            if (Session.CompanyId == Guid.Empty)
                return false;

            if (IsCreateMode)
            {
                var request = BuildCreateRequest();

                return _createGatewayValidator
                    .Validate(request)
                    .IsValid;
            }

            if (IsEditMode &&
                EditingGatewayId.HasValue)
            {
                return true;
            }

            return false;
        }
    }

    // COMMANDS

    public AsyncRelayCommand AddGatewayCommand { get; }

    public AsyncRelayCommand EditGatewayCommand { get; }

    public AsyncRelayCommand DeleteGatewayCommand { get; }

    public AsyncRelayCommand SaveGatewayCommand { get; }

    public AsyncRelayCommand CancelGatewayCommand { get; }

    public AsyncRelayCommand ViewSensorsCommand { get; }

    public AsyncRelayCommand ViewCommandHistoryCommand { get; }

    public AsyncRelayCommand ViewNetworkCommand { get; }

    // CONSTRUCTOR

    public GatewayViewModel(
        ISmartXApiClient apiClient,
        INavigationService navigationService,
        ICacheSyncService cacheSyncService,
        IConnectivityService connectivityService,
        IValidator<CreateGatewayRequest> createGatewayValidator,
        SmartXSession session)
        : base(connectivityService, session)
    {
        _apiClient = apiClient;
        _navigationService = navigationService;
        _cacheSyncService = cacheSyncService;
        _createGatewayValidator = createGatewayValidator;

        AddGatewayCommand =
            new AsyncRelayCommand(
                AddGatewayAsync,
                () => CanAddGateway);

        EditGatewayCommand =
            new AsyncRelayCommand(
                EditGatewayAsync,
                () => CanEditGateway);

        DeleteGatewayCommand =
            new AsyncRelayCommand(
                DeleteGatewayAsync,
                () => CanDeleteGateway);

        SaveGatewayCommand =
            new AsyncRelayCommand(
                SaveGatewayAsync,
                () => CanSaveGateway);

        CancelGatewayCommand =
            new AsyncRelayCommand(
                CancelGatewayAsync,
                () => !IsBusy);

        ViewSensorsCommand =
            new AsyncRelayCommand(
                OpenSensorsAsync,
                () => CanOpenGatewayArea);

        ViewCommandHistoryCommand =
            new AsyncRelayCommand(
                OpenCommandHistoryAsync,
                () => CanOpenGatewayArea);

        ViewNetworkCommand =
            new AsyncRelayCommand(
                OpenNetworkAsync,
                () => CanOpenGatewayArea);

    }

    // LOAD

    public async Task LoadAsync(
        CancellationToken cancellationToken = default)
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;

            ErrorMessage = string.Empty;
            StatusMessage = null;

            if (Session.CompanyId == Guid.Empty)
            {
                ErrorMessage =
                    "No company is associated with this session.";

                ClearGatewaySelection();

                return;
            }

            if (Session.Role == UserRole.SuperAdmin)
            {
                ErrorMessage =
                    "SuperAdmin accounts cannot access gateways.";

                ClearGatewaySelection();

                return;
            }

            if (!await CheckOnlineAsync(cancellationToken))
            {
                ErrorMessage =
                    "SmartX API is currently unavailable.";

                ClearGatewaySelection();

                return;
            }

            await LoadGatewaysAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            ErrorMessage =
                "Unable to connect to the SmartX API.";

            ClearGatewaySelection();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;

            ClearGatewaySelection();
        }
        finally
        {
            IsBusy = false;

            RaiseCommandStates();
        }
    }

    // CREATE

    private async Task AddGatewayAsync()
    {
        if (!CanAddGateway)
            return;

        BeginCreate();

        _navigationService
            .NavigateTo<GatewaySetupPage>();

        await Task.CompletedTask;
    }

    // EDIT

    private async Task EditGatewayAsync()
    {
        if (!CanEditGateway ||
            Gateway is null)
        {
            return;
        }

        try
        {
            IsBusy = true;

            ErrorMessage = string.Empty;
            StatusMessage = null;

            if (!await RequireOnlineAsync())
                return;

            var gateway =
                await _apiClient.GetGatewayByIdAsync(
                    Gateway.Id);

            if (gateway is null)
            {
                ErrorMessage =
                    "The gateway could not be found.";

                return;
            }

            EditingGatewayId = gateway.Id;

            Name = gateway.Name;
            Description = gateway.Description;
            SerialNumber = gateway.SerialNumber;
            IpAddress = gateway.IpAddress;
            IsActive = gateway.IsActive;

            Mode = GatewayMode.Edit;

            _navigationService
                .NavigateTo<GatewayEditPage>();
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

    // SAVE
    private async Task SaveGatewayAsync()
    {
        if (!CanSaveGateway)
            return;

        try
        {
            IsBusy = true;

            ErrorMessage = string.Empty;
            StatusMessage = null;

            if (!await RequireOnlineAsync())
                return;

            var companyId = Session.CompanyId;

            if (companyId == Guid.Empty)
            {
                ErrorMessage =
                    "No company is associated with this session.";

                return;
            }

            // CREATE

            if (IsCreateMode)
            {
                var request =
                    new CreateGatewayRequest
                    {
                        CompanyId = companyId,

                        Name = Name.Trim(),

                        Description =
                            string.IsNullOrWhiteSpace(Description)
                                ? string.Empty
                                : Description.Trim(),

                        SerialNumber =
                            string.IsNullOrWhiteSpace(SerialNumber)
                                ? null
                                : SerialNumber.Trim(),

                        IpAddress =
                            string.IsNullOrWhiteSpace(IpAddress)
                                ? null
                                : IpAddress.Trim()
                    };

                var validation =
                    _createGatewayValidator.Validate(request);

                if (!validation.IsValid)
                {
                    ErrorMessage =
                        validation.Errors
                            .First()
                            .ErrorMessage;

                    return;
                }
                var gatewayId =
                    await _apiClient.CreateGatewayAsync(
                        request);

                if (gatewayId == Guid.Empty)
                {
                    ErrorMessage =
                        "The gateway could not be created.";

                    return;
                }

                var onboardingCompleted =
    await _apiClient.CompleteCompanyOnboardingAsync(
        Session.CompanyId);

                if (!onboardingCompleted)
                {
                    ErrorMessage =
                        "Gateway was created, but onboarding could not be completed.";

                    return;
                }

                Session.CompleteOnboarding();

                _navigationService.NavigateTo<GatewayPage>();


                await _cacheSyncService
                    .SyncGatewaysAsync(companyId);

                var createdGateway =
                    await _apiClient.GetGatewayByIdAsync(
                        gatewayId);

                if (createdGateway is null)
                {
                    ErrorMessage =
                        "Gateway was created, but could not be loaded.";

                    return;
                }

                Gateway = createdGateway;

                Session.SelectGateway(
                    createdGateway.Id,
                    createdGateway.Name);

                if (IsOnboarding)
                {
                    Session.SetOnboardingCompleted(true);
                    Session.CompleteOnboarding();

                    StatusMessage =
                        "Gateway created successfully. Company setup is complete.";
                }
                else
                {
                    StatusMessage =
                        "Gateway created successfully.";
                }

                Mode = GatewayMode.List;

                ClearEditor();

                await LoadAsync();

                _navigationService
                    .NavigateTo<GatewayPage>();

                return;

            }

            // UPDATE

            if (IsEditMode)
            {
                if (!EditingGatewayId.HasValue)
                {
                    ErrorMessage =
                        "No gateway is selected for editing.";

                    return;
                }

                var request =
                    new UpdateGatewayRequest
                    {
                        Id = EditingGatewayId.Value,

                        CompanyId = companyId,

                        Name = Name.Trim(),

                        Description =
                            string.IsNullOrWhiteSpace(Description)
                                ? string.Empty
                                : Description.Trim(),

                        SerialNumber =
                            string.IsNullOrWhiteSpace(SerialNumber)
                                ? null
                                : SerialNumber.Trim(),

                        IpAddress =
                            string.IsNullOrWhiteSpace(IpAddress)
                                ? null
                                : IpAddress.Trim(),

                        IsActive = IsActive
                    };

                var success =
                    await _apiClient.UpdateGatewayAsync(
                        request);

                if (!success)
                {
                    ErrorMessage =
                        "The gateway could not be updated.";

                    return;
                }

                await _cacheSyncService
                    .SyncGatewaysAsync(companyId);

                Session.SelectGateway(
                    EditingGatewayId.Value,
                    Name.Trim());

                StatusMessage =
                    "Gateway updated successfully.";

                Mode = GatewayMode.List;

                ClearEditor();

                await LoadAsync();

                _navigationService
                    .NavigateTo<GatewayPage>();
            }
        }
        catch (OperationCanceledException)
        {
            throw;
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

    // DELETE

    private async Task DeleteGatewayAsync()
    {
        if (!CanDeleteGateway ||
            Gateway is null)
        {
            return;
        }

        var gatewayId = Gateway.Id;
        var gatewayName = Gateway.Name;

        var result =
            MessageBox.Show(
                $"Are you sure you want to delete '{gatewayName}'?\n\nThis action cannot be undone.",
                "Delete Gateway",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return;

        try
        {
            IsBusy = true;

            ErrorMessage = string.Empty;
            StatusMessage = null;

            if (!await RequireOnlineAsync())
                return;

            var deleted =
                await _apiClient.DeleteGatewayAsync(
                    gatewayId);

            if (!deleted)
            {
                ErrorMessage =
                    "The gateway could not be deleted.";

                return;
            }

            // Remove the deleted gateway immediately
            // from the current collection.
            var deletedGateway =
                Gateways.FirstOrDefault(
                    x => x.Id == gatewayId);

            if (deletedGateway is not null)
                Gateways.Remove(deletedGateway);

            // If there are other gateways, select the next one.
            if (Gateways.Count > 0)
            {
                var nextGateway = Gateways[0];

                SelectedGateway = nextGateway;

                Session.SelectGateway(
                    nextGateway.Id,
                    nextGateway.Name);

                Gateway = nextGateway;
            }
            else
            {
                // No gateways remain.
                SelectedGateway = null;
                Gateway = null;

                Session.ClearGateway();
            }

            // Refresh the local cache.
            await _cacheSyncService
                .SyncGatewaysAsync(Session.CompanyId);

            StatusMessage =
                "Gateway deleted successfully.";
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
    private async Task CancelGatewayAsync()
    {
        if (IsBusy)
            return;

        ClearEditor();

        Mode = GatewayMode.List;

        _navigationService
            .NavigateTo<GatewayPage>();

        await LoadAsync();
    }

    // CLEAR EDITOR

    private void ClearEditor()
    {
        EditingGatewayId = null;

        Name = string.Empty;
        Description = string.Empty;
        SerialNumber = null;
        IpAddress = null;
        IsActive = true;

        ErrorMessage = string.Empty;
        StatusMessage = null;

        RaiseCommandStates();
    }

    // SENSORS

    private async Task OpenSensorsAsync()
    {
        if (!CanOpenGatewayArea)
            return;

        _navigationService
            .NavigateTo<SensorsPage>();

        await Task.CompletedTask;
    }

    // HISTORY

    private async Task OpenCommandHistoryAsync()
    {
        if (!CanOpenGatewayArea)
            return;

        _navigationService
            .NavigateTo<HistoryPage>();

        await Task.CompletedTask;
    }

    // NETWORK

    private async Task OpenNetworkAsync()
    {
        if (!CanOpenGatewayArea)
            return;

        _navigationService
            .NavigateTo<NetworkPage>();

        await Task.CompletedTask;
    }


    // LOAD GATEWAYS

    private async Task LoadGatewaysAsync(
        CancellationToken cancellationToken = default)
    {
        if (Session.CompanyId == Guid.Empty)
        {
            ErrorMessage =
                "No company is associated with this session.";

            ClearGatewaySelection();

            return;
        }

        if (Session.Role == UserRole.SuperAdmin)
        {
            ErrorMessage =
                "SuperAdmin accounts cannot access gateways.";

            ClearGatewaySelection();

            return;
        }

        var gateways =
            await _apiClient.GetGatewaysByCompanyIdAsync(
                Session.CompanyId,
                cancellationToken);

        Gateways.Clear();

        foreach (var gateway in gateways)
            Gateways.Add(gateway);

        if (Gateways.Count == 0)
        {
            ClearGatewaySelection();

            return;
        }

        if (Session.GatewayId.HasValue)
        {
            var existing =
                Gateways.FirstOrDefault(
                    x => x.Id == Session.GatewayId.Value);

            if (existing is not null)
            {
                SelectedGateway = existing;
                return;
            }
        }

        SelectedGateway = Gateways[0];
    }

    // CLEAR SELECTION

    private void ClearGatewaySelection()
    {
        _selectedGateway = null;

        OnPropertyChanged(nameof(SelectedGateway));

        Gateway = null;

        Session.ClearGateway();

        OnPropertyChanged(nameof(SelectedGatewayName));
        OnPropertyChanged(nameof(HasSelectedGateway));

        RaiseCommandStates();
    }
    private CreateGatewayRequest BuildCreateRequest()
    {
        return new CreateGatewayRequest
        {
            CompanyId = Session.CompanyId,

            Name = Name.Trim(),

            Description =
                string.IsNullOrWhiteSpace(Description)
                    ? string.Empty
                    : Description.Trim(),

            SerialNumber =
                string.IsNullOrWhiteSpace(SerialNumber)
                    ? null
                    : SerialNumber.Trim(),

            IpAddress =
                string.IsNullOrWhiteSpace(IpAddress)
                    ? null
                    : IpAddress.Trim()
        };
    }

    // COMMAND STATES

    protected override void RaiseCommandStates()
    {
        AddGatewayCommand?.RaiseCanExecuteChanged();
        EditGatewayCommand?.RaiseCanExecuteChanged();
        DeleteGatewayCommand?.RaiseCanExecuteChanged();
        SaveGatewayCommand?.RaiseCanExecuteChanged();
        CancelGatewayCommand?.RaiseCanExecuteChanged();
        ViewSensorsCommand?.RaiseCanExecuteChanged();
        ViewCommandHistoryCommand?.RaiseCanExecuteChanged();
        ViewNetworkCommand?.RaiseCanExecuteChanged();

        OnPropertyChanged(nameof(CanAddGateway));
        OnPropertyChanged(nameof(CanEditGateway));
        OnPropertyChanged(nameof(CanDeleteGateway));
        OnPropertyChanged(nameof(CanOpenGatewayArea));
        OnPropertyChanged(nameof(CanSaveGateway));

        OnPropertyChanged(nameof(IsAdministrator));
        OnPropertyChanged(nameof(IsTechnician));
        OnPropertyChanged(nameof(IsSuperAdmin));

        OnPropertyChanged(nameof(GatewayCrudVisibility));
        OnPropertyChanged(nameof(GatewayFeaturesVisibility));
        OnPropertyChanged(nameof(NoGatewayVisibility));
    }

    public void ResetEditor()
    {
        ClearEditor();
        Mode = GatewayMode.List;
        RaiseCommandStates();
    }
    // CONNECTIVITY

    protected override void RaiseConnectivityState()
    {
        RaiseCommandStates();
    }

    // SESSION

    protected override void OnSessionPropertyChanged(
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SmartXSession.Role) ||
            e.PropertyName == nameof(SmartXSession.SelectedCompanyId) ||
            e.PropertyName == nameof(SmartXSession.GatewayId) ||
            e.PropertyName == nameof(SmartXSession.GatewayName))
        {
            RaiseCommandStates();
        }
    }
}