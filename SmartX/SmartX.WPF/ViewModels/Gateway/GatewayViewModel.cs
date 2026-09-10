using FluentValidation;
using SmartX.Application.Requests.Gateway;
using SmartX.Domain.Enums;
using SmartX.Shared.DTOs;
using SmartX.WPF.Navigation;
using SmartX.WPF.Services.Api;
using SmartX.WPF.Services.Connectivity;
using SmartX.WPF.Services.Demo;
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
    private readonly IGuestDemoService _guestDemoService;
    private readonly IValidator<CreateGatewayRequest> _createGatewayValidator;

    public enum GatewayMode
    {
        List,
        Create,
        Edit
    }

    private GatewayMode _mode = GatewayMode.List;

    private bool _isOnboarding;

    private GatewayDto? _gateway;
    private GatewayDto? _selectedGateway;
    private Guid? _editingGatewayId;

    private string _name = string.Empty;
    private string _description = string.Empty;
    private string? _serialNumber;
    private string? _ipAddress;
    private bool _isActive = true;
    private string? _statusMessage;

    public GatewayViewModel(
        ISmartXApiClient apiClient,
        INavigationService navigationService,
        ICacheSyncService cacheSyncService,
        IConnectivityService connectivityService,
        IValidator<CreateGatewayRequest> createGatewayValidator,
        IGuestDemoService guestDemoService,
        SmartXSession session)
        : base(connectivityService, session)
    {
        _apiClient = apiClient;
        _navigationService = navigationService;
        _cacheSyncService = cacheSyncService;
        _createGatewayValidator = createGatewayValidator;
        _guestDemoService = guestDemoService;

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

    public bool IsOnboarding
    {
        get => _isOnboarding;
        private set => SetProperty(
            ref _isOnboarding,
            value);
    }

    public bool IsListMode =>
        Mode == GatewayMode.List;

    public bool IsCreateMode =>
        Mode == GatewayMode.Create;

    public bool IsEditMode =>
        Mode == GatewayMode.Edit;

    public ObservableCollection<GatewayDto> Gateways { get; } = [];

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
        Session.GatewayId.HasValue &&
        Session.GatewayId.Value != Guid.Empty;

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
        set
        {
            if (!SetProperty(ref _description, value))
                return;

            RaiseCommandStates();
        }
    }

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

    public string? StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(
            ref _statusMessage,
            value);
    }

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

    public bool IsAdministrator =>
        Session.Role == UserRole.Administrator;

    public bool IsTechnician =>
        Session.Role == UserRole.Technician;

    public bool IsSuperAdmin =>
        Session.Role == UserRole.SuperAdmin;

    public bool CanAddGateway =>
        !IsBusy &&
        IsOnline &&
        Session.IsAuthenticated &&
        Session.CompanyId != Guid.Empty &&
        IsAdministrator &&
        IsListMode &&
        (!Session.IsGuest || Gateways.Count < 1);

    public bool CanEditGateway =>
        !IsBusy &&
        IsOnline &&
        Session.IsAuthenticated &&
        Gateway is not null &&
        IsAdministrator;

    public bool CanDeleteGateway =>
        !IsBusy &&
        IsOnline &&
        Session.IsAuthenticated &&
        Gateway is not null &&
        IsAdministrator;

    public bool CanOpenGatewayArea =>
        !IsBusy &&
        IsOnline &&
        Session.IsAuthenticated &&
        Gateway is not null &&
        Session.Role is
            UserRole.Administrator or
            UserRole.Technician;

    public bool CanSaveGateway
    {
        get
        {
            if (IsBusy ||
                !IsOnline ||
                !Session.IsAuthenticated ||
                Session.CompanyId == Guid.Empty)
            {
                return false;
            }

            if (IsCreateMode)
            {
                if (Session.IsGuest &&
                    Gateways.Count >= 1)
                {
                    return false;
                }

                var request = BuildCreateRequest();

                return _createGatewayValidator
                    .Validate(request)
                    .IsValid;
            }

            if (IsEditMode &&
                EditingGatewayId.HasValue)
            {
                return IsAdministrator;
            }

            return false;
        }
    }

    public AsyncRelayCommand AddGatewayCommand { get; }

    public AsyncRelayCommand EditGatewayCommand { get; }

    public AsyncRelayCommand DeleteGatewayCommand { get; }

    public AsyncRelayCommand SaveGatewayCommand { get; }

    public AsyncRelayCommand CancelGatewayCommand { get; }

    public AsyncRelayCommand ViewSensorsCommand { get; }

    public AsyncRelayCommand ViewCommandHistoryCommand { get; }

    public AsyncRelayCommand ViewNetworkCommand { get; }

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

            if (!Session.IsAuthenticated)
            {
                ErrorMessage =
                    "No authenticated user is associated with this session.";

                ClearGatewaySelection();

                return;
            }

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

            await LoadGatewaysAsync(
                cancellationToken);
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

    private async Task AddGatewayAsync()
    {
        if (!CanAddGateway)
            return;

        BeginCreate();

        _navigationService.NavigateTo<GatewaySetupPage>();

        await Task.CompletedTask;
    }

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

            if (Session.IsGuest)
            {
                EditingGatewayId = Gateway.Id;

                Name = Gateway.Name;
                Description = Gateway.Description;
                SerialNumber = Gateway.SerialNumber;
                IpAddress = Gateway.IpAddress;
                IsActive = Gateway.IsActive;

                Mode = GatewayMode.Edit;

                _navigationService
                    .NavigateTo<GatewayEditPage>();

                return;
            }

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

    private async Task SaveGatewayAsync()
    {
        if (!CanSaveGateway)
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

                return;
            }

            if (IsCreateMode)
            {
                await CreateGatewayAsync();
                return;
            }

            if (IsEditMode)
            {
                await UpdateGatewayAsync();
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

    private async Task CreateGatewayAsync()
    {
        if (Session.IsGuest)
        {
            if (Gateways.Count >= 1)
            {
                ErrorMessage =
                    "Guest mode is limited to one gateway.";

                return;
            }

            var demoGateway =
                _guestDemoService
                    .GetGateways()
                    .FirstOrDefault();

            if (demoGateway is null)
            {
                ErrorMessage =
                    "The demo gateway could not be loaded.";

                return;
            }

            var gateway =
                new GatewayDto
                {
                    Id = demoGateway.Id,
                    CompanyId = Session.CompanyId,
                    Name = string.IsNullOrWhiteSpace(Name)
                        ? demoGateway.Name
                        : Name.Trim(),
                    Description =
                        string.IsNullOrWhiteSpace(Description)
                            ? demoGateway.Description
                            : Description.Trim(),
                    SerialNumber =
                        string.IsNullOrWhiteSpace(SerialNumber)
                            ? demoGateway.SerialNumber
                            : SerialNumber.Trim(),
                    IpAddress =
                        string.IsNullOrWhiteSpace(IpAddress)
                            ? demoGateway.IpAddress
                            : IpAddress.Trim(),
                    IsActive = IsActive
                };

            Gateways.Clear();
            Gateways.Add(gateway);

            Gateway = gateway;
            SelectedGateway = gateway;

            Session.SelectGateway(
                gateway.Id,
                gateway.Name);

            StatusMessage =
                "Demo gateway created successfully.";

            Mode = GatewayMode.List;

            ClearEditor();

            _navigationService
                .NavigateTo<GatewayPage>();

            return;
        }

        var request =
            BuildCreateRequest();

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

        if (IsOnboarding)
        {
            var onboardingCompleted =
                await _apiClient
                    .CompleteCompanyOnboardingAsync(
                        Session.CompanyId);

            if (!onboardingCompleted)
            {
                ErrorMessage =
                    "Gateway was created, but onboarding could not be completed.";

                return;
            }

            Session.CompleteOnboarding();
        }

        await _cacheSyncService
            .SyncGatewaysAsync(
                Session.CompanyId);

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

        StatusMessage =
            IsOnboarding
                ? "Gateway created successfully. Company setup is complete."
                : "Gateway created successfully.";

        Mode = GatewayMode.List;

        ClearEditor();

        await LoadAsync();

        _navigationService
            .NavigateTo<GatewayPage>();
    }

    private async Task UpdateGatewayAsync()
    {
        if (!EditingGatewayId.HasValue)
        {
            ErrorMessage =
                "No gateway is selected for editing.";

            return;
        }

        if (Session.IsGuest)
        {
            var existing =
                Gateways.FirstOrDefault(
                    x => x.Id == EditingGatewayId.Value);

            if (existing is null)
            {
                ErrorMessage =
                    "The demo gateway could not be found.";

                return;
            }

            var updated =
                new GatewayDto
                {
                    Id = existing.Id,
                    CompanyId = existing.CompanyId,
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

            var index =
                Gateways.IndexOf(existing);

            if (index >= 0)
                Gateways[index] = updated;

            Gateway = updated;
            SelectedGateway = updated;

            Session.SelectGateway(
                updated.Id,
                updated.Name);

            StatusMessage =
                "Demo gateway updated successfully.";

            Mode = GatewayMode.List;

            ClearEditor();

            _navigationService
                .NavigateTo<GatewayPage>();

            return;
        }

        var request =
            new UpdateGatewayRequest
            {
                Id = EditingGatewayId.Value,
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
            .SyncGatewaysAsync(
                Session.CompanyId);

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

    private async Task DeleteGatewayAsync()
    {
        if (!CanDeleteGateway ||
            Gateway is null)
        {
            return;
        }

        var gatewayId =
            Gateway.Id;

        var gatewayName =
            Gateway.Name;

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

            if (Session.IsGuest)
            {
                var demoGateway =
                    Gateways.FirstOrDefault(
                        x => x.Id == gatewayId);

                if (demoGateway is not null)
                    Gateways.Remove(demoGateway);

                Gateway = null;
                SelectedGateway = null;

                Session.ClearGateway();

                StatusMessage =
                    "Demo gateway deleted successfully.";

                RaiseCommandStates();

                return;
            }

            var deleted =
                await _apiClient.DeleteGatewayAsync(
                    gatewayId);

            if (!deleted)
            {
                ErrorMessage =
                    "The gateway could not be deleted.";

                return;
            }

            var deletedGateway =
                Gateways.FirstOrDefault(
                    x => x.Id == gatewayId);

            if (deletedGateway is not null)
                Gateways.Remove(deletedGateway);

            if (Gateways.Count > 0)
            {
                var nextGateway =
                    Gateways[0];

                SelectedGateway = nextGateway;

                Session.SelectGateway(
                    nextGateway.Id,
                    nextGateway.Name);

                Gateway = nextGateway;
            }
            else
            {
                SelectedGateway = null;
                Gateway = null;

                Session.ClearGateway();
            }

            await _cacheSyncService
                .SyncGatewaysAsync(
                    Session.CompanyId);

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

    private async Task OpenSensorsAsync()
    {
        if (!CanOpenGatewayArea)
            return;

        _navigationService
            .NavigateTo<SensorsPage>();

        await Task.CompletedTask;
    }

    private async Task OpenCommandHistoryAsync()
    {
        if (!CanOpenGatewayArea)
            return;

        _navigationService
            .NavigateTo<HistoryPage>();

        await Task.CompletedTask;
    }

    private async Task OpenNetworkAsync()
    {
        if (!CanOpenGatewayArea)
            return;

        _navigationService
            .NavigateTo<NetworkPage>();

        await Task.CompletedTask;
    }

    private async Task LoadGatewaysAsync(
        CancellationToken cancellationToken = default)
    {
        if (Session.IsGuest)
        {
            Gateways.Clear();

            foreach (var gateway in _guestDemoService.GetGateways())
            {
                Gateways.Add(
                    new GatewayDto
                    {
                        Id = gateway.Id,
                        CompanyId = Session.CompanyId,
                        Name = gateway.Name,
                        Description = gateway.Description,
                        SerialNumber = gateway.SerialNumber,
                        IpAddress = gateway.IpAddress,
                        IsActive = gateway.IsActive
                    });
            }

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

            return;
        }

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

    private void ClearGatewaySelection()
    {
        _selectedGateway = null;

        OnPropertyChanged(
            nameof(SelectedGateway));

        Gateway = null;

        Session.ClearGateway();

        OnPropertyChanged(
            nameof(SelectedGatewayName));

        OnPropertyChanged(
            nameof(HasSelectedGateway));

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
        OnPropertyChanged(nameof(HasGateway));
        OnPropertyChanged(nameof(HasNoGateway));
        OnPropertyChanged(nameof(HasSelectedGateway));
    }

    public void ResetEditor()
    {
        ClearEditor();

        Mode = GatewayMode.List;

        RaiseCommandStates();
    }

    protected override void RaiseConnectivityState()
    {
        RaiseCommandStates();
    }

    protected override void OnSessionPropertyChanged(
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SmartXSession.Role) ||
            e.PropertyName == nameof(SmartXSession.CompanyId) ||
            e.PropertyName == nameof(SmartXSession.IsGuest) ||
            e.PropertyName == nameof(SmartXSession.IsAuthenticated) ||
            e.PropertyName == nameof(SmartXSession.SelectedCompanyId) ||
            e.PropertyName == nameof(SmartXSession.GatewayId) ||
            e.PropertyName == nameof(SmartXSession.GatewayName))
        {
            RaiseCommandStates();
        }
    }
}
