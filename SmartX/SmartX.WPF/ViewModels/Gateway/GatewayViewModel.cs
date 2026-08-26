using Microsoft.Win32;
using SmartX.Application.Commands.Gateway;
using SmartX.Domain.Enums;
using SmartX.Shared.DTOs;
using SmartX.WPF.Navigation;
using SmartX.WPF.Services;
using SmartX.WPF.Services.Api;
using SmartX.WPF.Services.Sync;
using SmartX.WPF.ViewModels.Base;
using SmartX.WPF.Views.Pages.Gateway;
using SmartX.WPF.Views.Pages.History;
using SmartX.WPF.Views.Pages.Network;
using SmartX.WPF.Views.Pages.Sensor;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Windows;

namespace SmartX.WPF.ViewModels.Gateway;

public class GatewayViewModel : ViewModelBase
{
    // =========================================================
    // DEPENDENCIES
    // =========================================================

    private readonly ISmartXApiClient _apiClient;
    private readonly SmartXSession _session;
    private readonly INavigationService _navigationService;
    private readonly ICacheSyncService _cacheSyncService;

    // =========================================================
    // MODE
    // =========================================================

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

    // =========================================================
    // GATEWAYS
    // =========================================================

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

    // =========================================================
    // SELECTED GATEWAY
    // =========================================================

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

                _session.SelectGateway(
                    _selectedGateway.Id,
                    _selectedGateway.Name);
            }
            else
            {
                Gateway = null;

                _session.ClearGateway();
            }

            OnPropertyChanged(nameof(SelectedGatewayName));
            OnPropertyChanged(nameof(HasSelectedGateway));

            RaiseCommandStates();
        }
    }

    public string? SelectedGatewayName =>
        _session.GatewayName;

    public bool HasSelectedGateway =>
        _session.GatewayId.HasValue;

    // =========================================================
    // EDIT / CREATE FIELDS
    // =========================================================

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

    // =========================================================
    // STATE
    // =========================================================

    private bool _isBusy;
    private bool _isOnline;

    private string _errorMessage = string.Empty;
    private string? _statusMessage;

    public bool IsBusy
    {
        get => _isBusy;

        private set
        {
            if (!SetProperty(ref _isBusy, value))
                return;

            RaiseCommandStates();
        }
    }

    public bool IsOnline
    {
        get => _isOnline;

        private set
        {
            if (!SetProperty(ref _isOnline, value))
                return;

            RaiseCommandStates();
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;

        private set => SetProperty(
            ref _errorMessage,
            value);
    }

    public string? StatusMessage
    {
        get => _statusMessage;

        private set => SetProperty(
            ref _statusMessage,
            value);
    }

    // =========================================================
    // VISIBILITY
    // =========================================================

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

    // =========================================================
    // PERMISSIONS
    // =========================================================

    public bool IsAdministrator =>
        _session.Role == UserRole.Administrator;

    public bool IsTechnician =>
        _session.Role == UserRole.Technician;

    public bool IsSuperAdmin =>
        _session.Role == UserRole.SuperAdmin;

    public bool CanAddGateway =>
        IsOnline &&
        !IsBusy &&
        _session.CompanyId != Guid.Empty &&
        IsAdministrator &&
        IsListMode;

    public bool CanEditGateway =>
        IsOnline &&
        !IsBusy &&
        Gateway is not null &&
        IsAdministrator;

    public bool CanDeleteGateway =>
        IsOnline &&
        !IsBusy &&
        Gateway is not null &&
        IsAdministrator;

    public bool CanOpenGatewayArea =>
        IsOnline &&
        !IsBusy &&
        Gateway is not null &&
        (IsAdministrator || IsTechnician);

    public bool CanSaveGateway =>
        IsOnline &&
        !IsBusy &&
        !string.IsNullOrWhiteSpace(Name) &&
        _session.CompanyId != Guid.Empty &&
        (IsCreateMode || EditingGatewayId.HasValue);

    // =========================================================
    // COMMANDS
    // =========================================================

    public AsyncRelayCommand AddGatewayCommand { get; }

    public AsyncRelayCommand EditGatewayCommand { get; }

    public AsyncRelayCommand DeleteGatewayCommand { get; }

    public AsyncRelayCommand SaveGatewayCommand { get; }

    public AsyncRelayCommand CancelGatewayCommand { get; }

    public AsyncRelayCommand ViewSensorsCommand { get; }

    public AsyncRelayCommand ViewCommandHistoryCommand { get; }

    public AsyncRelayCommand ViewNetworkCommand { get; }

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public GatewayViewModel(
        ISmartXApiClient apiClient,
        SmartXSession session,
        INavigationService navigationService,
        ICacheSyncService cacheSyncService)
    {
        _apiClient = apiClient;
        _session = session;
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

        _session.PropertyChanged +=
            OnSessionChanged;
    }

    // =========================================================
    // LOAD LIST
    // =========================================================

    public async Task LoadAsync(
        CancellationToken cancellationToken = default)
    {
        if (IsBusy)
            return;

        if (_session.CompanyId == Guid.Empty)
        {
            IsOnline = false;

            ErrorMessage =
                "No company is associated with this session.";

            ClearGatewaySelection();

            return;
        }

        if (_session.Role == UserRole.SuperAdmin)
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

            IsOnline =
                await _apiClient.IsAvailableAsync(
                    cancellationToken);

            if (!IsOnline)
            {
                ErrorMessage =
                    "SmartX API is currently unavailable.";

                ClearGatewaySelection();

                return;
            }

            var gateways =
                await _apiClient.GetGatewaysByCompanyIdAsync(
                    _session.CompanyId,
                    cancellationToken);

            Gateways.Clear();

            foreach (var gateway in gateways)
                Gateways.Add(gateway);

            if (Gateways.Count == 0)
            {
                ClearGatewaySelection();
                return;
            }

            if (_session.GatewayId.HasValue)
            {
                var existing =
                    Gateways.FirstOrDefault(
                        x => x.Id == _session.GatewayId.Value);

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
            IsOnline = false;

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

    // =========================================================
    // CREATE MODE
    // =========================================================

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

    // =========================================================
    // EDIT MODE
    // =========================================================

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
        }
    }

    // =========================================================
    // SAVE CREATE / UPDATE
    // =========================================================

    private async Task SaveGatewayAsync()
    {
        if (!CanSaveGateway)
            return;

        try
        {
            IsBusy = true;

            ErrorMessage = string.Empty;
            StatusMessage = null;

            var companyId = _session.CompanyId;

            if (companyId == Guid.Empty)
            {
                ErrorMessage =
                    "No company is associated with this session.";

                return;
            }

            // =================================================
            // CREATE
            // =================================================

            if (IsCreateMode)
            {
                var command =
                    new CreateGatewayCommand
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

                        IsActive = true
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

                _session.SelectGateway(
                    gatewayId,
                    Name.Trim());

                StatusMessage =
                    "Gateway created successfully.";
            }

            // =================================================
            // UPDATE
            // =================================================

            else if (IsEditMode)
            {
                if (!EditingGatewayId.HasValue)
                {
                    ErrorMessage =
                        "No gateway is selected for editing.";

                    return;
                }

                var command =
                    new UpdateGatewayCommand
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

                _session.SelectGateway(
                    EditingGatewayId.Value,
                    Name.Trim());

                StatusMessage =
                    "Gateway updated successfully.";
            }

            // =================================================
            // RETURN TO LIST
            // =================================================

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
    // DELETE
    // =========================================================

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
                    _session.CompanyId);

            if (_session.GatewayId == gatewayId)
                _session.ClearGateway();

            // IMPORTANT:
            // Reload the complete gateway list.
            await LoadAsync();

            StatusMessage =
                "Gateway deleted successfully.";
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
    // CANCEL
    // =========================================================

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

    // =========================================================
    // CLEAR EDITOR
    // =========================================================

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

    // =========================================================
    // SENSOR
    // =========================================================

    private async Task OpenSensorsAsync()
    {
        if (!CanOpenGatewayArea)
            return;

        _navigationService
            .NavigateTo<SensorsPage>();

        await Task.CompletedTask;
    }

    // =========================================================
    // HISTORY
    // =========================================================

    private async Task OpenCommandHistoryAsync()
    {
        if (!CanOpenGatewayArea)
            return;

        _navigationService
            .NavigateTo<HistoryPage>();

        await Task.CompletedTask;
    }

    // =========================================================
    // NETWORK
    // =========================================================

    private async Task OpenNetworkAsync()
    {
        if (!CanOpenGatewayArea)
            return;

        _navigationService
            .NavigateTo<NetworkPage>();

        await Task.CompletedTask;
    }

    // =========================================================
    // CLEAR SELECTION
    // =========================================================

    private void ClearGatewaySelection()
    {
        _selectedGateway = null;

        OnPropertyChanged(nameof(SelectedGateway));

        Gateway = null;

        _session.ClearGateway();

        OnPropertyChanged(nameof(SelectedGatewayName));
        OnPropertyChanged(nameof(HasSelectedGateway));

        RaiseCommandStates();
    }

    // =========================================================
    // SESSION
    // =========================================================

    private void OnSessionChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SmartXSession.Role) ||
            e.PropertyName == nameof(SmartXSession.CompanyId) ||
            e.PropertyName == nameof(SmartXSession.GatewayId) ||
            e.PropertyName == nameof(SmartXSession.GatewayName))
        {
            OnPropertyChanged(nameof(IsAdministrator));
            OnPropertyChanged(nameof(IsTechnician));
            OnPropertyChanged(nameof(IsSuperAdmin));

            OnPropertyChanged(nameof(SelectedGatewayName));
            OnPropertyChanged(nameof(HasSelectedGateway));

            OnPropertyChanged(nameof(GatewayCrudVisibility));
            OnPropertyChanged(nameof(GatewayFeaturesVisibility));
            OnPropertyChanged(nameof(NoGatewayVisibility));

            RaiseCommandStates();
        }
    }

    // =========================================================
    // COMMAND STATES
    // =========================================================

    private void RaiseCommandStates()
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
}