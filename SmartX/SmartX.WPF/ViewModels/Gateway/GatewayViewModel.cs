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
using System.Net.Http;
using System.Windows;

namespace SmartX.WPF.ViewModels.Gateway;

public class GatewayViewModel : ViewModelBase
{

    private readonly ISmartXApiClient _apiClient;
    private readonly INavigationService _navigationService;
    private readonly ICacheSyncService _cacheSyncService;

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
        Session.GatewayId.HasValue == true;

    // EDIT / CREATE FIELDS
    private Guid? _editingGatewayId;

    public Guid? EditingGatewayId
    {
        get => _editingGatewayId;
        private set => SetProperty(
            ref _editingGatewayId,
            value);
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
        set => SetProperty(
            ref _description,
            value);
    }

    private string? _serialNumber;

    public string? SerialNumber
    {
        get => _serialNumber;
        set => SetProperty(
            ref _serialNumber,
            value);
    }

    private string? _ipAddress;

    public string? IpAddress
    {
        get => _ipAddress;
        set => SetProperty(
            ref _ipAddress,
            value);
    }

    private bool _isActive = true;

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(
            ref _isActive,
            value);
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

    public bool CanAddGateway
    {
        get
        {

            return
                   !IsBusy &&
                   Session.CompanyId != Guid.Empty &&
                   Session.Role == UserRole.Administrator &&
                   IsListMode;
        }
    }


    public bool CanEditGateway =>
        IsOnline &&
        !IsBusy &&
        Gateway is not null &&
        Session.Role == UserRole.Administrator;

    public bool CanDeleteGateway =>
        IsOnline &&
        !IsBusy &&
        Gateway is not null &&
        Session.Role == UserRole.Administrator;

    public bool CanOpenGatewayArea =>
        IsOnline &&
        !IsBusy &&
        Gateway is not null &&
        Session.Role is UserRole.Administrator or UserRole.Technician;

    public bool CanSaveGateway =>
        IsOnline &&
        !IsBusy &&
        !string.IsNullOrWhiteSpace(Name) &&
        Session.CompanyId != Guid.Empty &&
        (IsCreateMode || EditingGatewayId.HasValue);

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
        SmartXSession session): base(connectivityService, session)
    {
        _apiClient = apiClient;
        _navigationService = navigationService;
        _cacheSyncService = cacheSyncService;

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

    // LOAD LIST

    public async Task LoadAsync(
        CancellationToken cancellationToken = default)
    {
        if (IsBusy)
            return;

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

        try
        {
            IsBusy = true;

            ErrorMessage = string.Empty;
            StatusMessage = null;

            if (!await CheckOnlineAsync(cancellationToken))
            {
                ErrorMessage =
                    "SmartX API is currently unavailable.";

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

    // CREATE MODE
    private async Task AddGatewayAsync()
    {
        if (!CanAddGateway)
            return;

        ClearEditor();

        Mode = GatewayMode.Create;

        _navigationService
            .NavigateTo<GatewaySetupPage>();

        await Task.CompletedTask;
    }

    // EDIT MODE
    private async Task EditGatewayAsync()
    {
        if (!CanEditGateway ||
            Gateway is null)
            return;

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
        }
    }

    // SAVE CREATE / UPDATE
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
                var command =
                    new CreateGatewayRequest
                    {
                        CompanyId = companyId,

                        Name = Name.Trim(),

                        Description =
                            Description.Trim(),

                        SerialNumber =
                            string.IsNullOrWhiteSpace(SerialNumber)
                                ? null
                                : SerialNumber.Trim(),

                        IpAddress =
                            string.IsNullOrWhiteSpace(IpAddress)
                                ? null
                                : IpAddress.Trim(),

                    };

                var gatewayId =
                    await _apiClient.CreateGatewayAsync(
                        command);

                if (gatewayId == Guid.Empty)
                {
                    ErrorMessage =
                        "The gateway could not be created.";

                    return;
                }

                await _cacheSyncService
                    .SyncGatewaysAsync(companyId);

                Session.SelectGateway(
                    gatewayId,
                    Name.Trim());

                StatusMessage =
                    "Gateway created successfully.";
            }

            // UPDATE

            else if (IsEditMode)
            {
                if (!EditingGatewayId.HasValue)
                {
                    ErrorMessage =
                        "No gateway is selected for editing.";

                    return;
                }

                var command =
                    new UpdateGatewayRequest
                    {
                        Id = EditingGatewayId.Value,

                        CompanyId = companyId,

                        Name = Name.Trim(),

                        Description =
                            Description.Trim(),

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
                        command);

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
            }

            // RETURN TO LIST

            Mode = GatewayMode.List;

            ClearEditor();

            await LoadAsync();

            _navigationService
                .NavigateTo<GatewayPage>();
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
            return;

        var result =
            MessageBox.Show(
                $"Are you sure you want to delete '{Gateway.Name}'?\n\nThis action cannot be undone.",
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

            var gatewayId =
                Gateway.Id;

            var deleted =
                await _apiClient.DeleteGatewayAsync(
                    gatewayId);

            if (!deleted)
            {
                ErrorMessage =
                    "The gateway could not be deleted.";

                return;
            }

            await _cacheSyncService
                .SyncGatewaysAsync(
                    Session.CompanyId);

            if (Session.GatewayId == gatewayId)
                Session.ClearGateway();

            await LoadAsync();

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
    }

    // SENSOR
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


    // CONNECTIVITY
    protected override void RaiseConnectivityState()
    {
        RaiseCommandStates();
    }

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