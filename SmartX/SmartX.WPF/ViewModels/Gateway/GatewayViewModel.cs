using SmartX.Domain.Enums;
using SmartX.Shared.DTOs;
using SmartX.WPF.Navigation;
using SmartX.WPF.Services;
using SmartX.WPF.Services.Api;
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

public class GatewayViewModel : INotifyPropertyChanged
{

    private readonly ISmartXApiClient _apiClient;
    private readonly SmartXSession _session;
    private readonly INavigationService _navigationService;

    private GatewayDto? _gateway;
    private GatewayDto? _selectedGateway;

    private bool _isBusy;
    private string? _errorMessage;
    private bool _isOnline;

    public GatewayViewModel(
        ISmartXApiClient apiClient,
        SmartXSession session,
        INavigationService navigationService)
    {
        _apiClient = apiClient;
        _session = session;
        _navigationService = navigationService;
        _session.PropertyChanged += OnSessionChanged;
        AddGatewayCommand = new AsyncRelayCommand(
            AddGatewayAsync,
            () => CanAddGateway);

        EditGatewayCommand = new AsyncRelayCommand(
            EditGatewayAsync,
            () => CanEditGateway);

        DeleteGatewayCommand = new AsyncRelayCommand(
            DeleteGatewayAsync,
            () => CanDeleteGateway);

        ViewSensorsCommand = new AsyncRelayCommand(
            OpenSensorsAsync,
            () => CanOpenGatewayArea);

        ViewCommandHistoryCommand = new AsyncRelayCommand(
            OpenCommandHistoryAsync,
            () => CanOpenGatewayArea);

        ViewNetworkCommand = new AsyncRelayCommand(
            OpenNetworkAsync,
            () => CanOpenGatewayArea);
    }

    public Visibility GatewayCrudVisibility => IsAdministrator
    ? Visibility.Visible
    : Visibility.Collapsed;

    public Visibility GatewayFeaturesVisibility => HasGateway
            ? Visibility.Visible
            : Visibility.Collapsed;

    public Visibility NoGatewayVisibility => HasNoGateway
            ? Visibility.Visible
            : Visibility.Collapsed;

    // GATEWAYS
    public ObservableCollection<GatewayDto> Gateways { get; } = new();

    // CURRENT GATEWAY
    public GatewayDto? Gateway
    {
        get => _gateway;

        private set
        {
            if (_gateway == value) return;

            _gateway = value;

            OnPropertyChanged();

            OnPropertyChanged(nameof(HasGateway));
            OnPropertyChanged(nameof(HasNoGateway));
            OnPropertyChanged(nameof(GatewayFeaturesVisibility));
            OnPropertyChanged(nameof(NoGatewayVisibility));

            RaiseCommandStates();
        }
    }

    public bool HasGateway => Gateway != null;

    public bool HasNoGateway => Gateway == null;

    // SELECTED GATEWAY
    public GatewayDto? SelectedGateway
    {
        get => _selectedGateway;

        set
        {
            if (_selectedGateway == value)
                return;

            _selectedGateway = value;

            OnPropertyChanged();

            if (_selectedGateway != null)
            {
                Gateway = _selectedGateway;

                _session.SelectGateway(
                    _selectedGateway.Id,
                    _selectedGateway.Name);

                GatewayMessage =
                    $"Gateway changed to \"{_selectedGateway.Name}\".";
            }
            else
            {
                Gateway = null;

                _session.ClearGateway();

                GatewayMessage = string.Empty;
            }

            RaiseCommandStates();
        }
    }

    private string _gatewayMessage = string.Empty;

    public string GatewayMessage
    {
        get => _gatewayMessage;
        private set => SetProperty(
            ref _gatewayMessage,
            value);
    }

    public string? SelectedGatewayName =>
        _session.GatewayName;

    public bool HasSelectedGateway =>
        _session.GatewayId.HasValue;

    // BUSY
    public bool IsBusy
    {
        get => _isBusy;

        private set
        {
            if (_isBusy == value) return;

            _isBusy = value;

            OnPropertyChanged();

            RaiseCommandStates();
        }
    }

    public bool IsOnline
    {
        get => _isOnline;

        private set
        {
            if (_isOnline == value) return;

            _isOnline = value;

            OnPropertyChanged();

            RaiseCommandStates();
        }
    }

    // ERROR

    public string? ErrorMessage
    {
        get => _errorMessage;

        private set
        {
            if (_errorMessage == value) return;

            _errorMessage = value;

            OnPropertyChanged();
        }
    }

    // PERMISSIONS
    public bool IsAdministrator =>
        _session.Role == UserRole.Administrator;

    public bool IsTechnician =>
        _session.Role == UserRole.Technician;

    public bool IsSuperAdmin =>
        _session.Role == UserRole.SuperAdmin;

    public bool CanAddGateway =>
        IsOnline &&
        !IsBusy &&
        _session.CompanyId != Guid.Empty && IsAdministrator;

    public bool CanEditGateway =>
        IsOnline &&
        !IsBusy &&
        Gateway != null && IsAdministrator;

    public bool CanDeleteGateway =>
        IsOnline &&
        !IsBusy &&
        Gateway != null && IsAdministrator;

    public bool CanOpenGatewayArea =>
        IsOnline &&
        !IsBusy &&
        Gateway != null &&
        (IsAdministrator || IsTechnician);


    // COMMANDS
    public AsyncRelayCommand AddGatewayCommand { get; }

    public AsyncRelayCommand EditGatewayCommand { get; }

    public AsyncRelayCommand DeleteGatewayCommand { get; }

    public AsyncRelayCommand ViewSensorsCommand { get; }

    public AsyncRelayCommand ViewCommandHistoryCommand { get; }

    public AsyncRelayCommand ViewNetworkCommand { get; }

    // LOAD
    public async Task LoadAsync()
    {
        if (_session.CompanyId == Guid.Empty)
        {
            IsOnline = false;

            ErrorMessage = "No company is associated with this session.";

            ClearGatewaySelection();

            return;
        }

        if (IsBusy)
            return;

            if (_session.Role == UserRole.SuperAdmin)
        {



            ErrorMessage = "SuperAdmin accounts cannot access gateways.";

            ClearGatewaySelection();

            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            IsOnline =
                await _apiClient.IsAvailableAsync();

            if (!IsOnline)
            {
                ErrorMessage = "SmartX API is currently unavailable.";

                ClearGatewaySelection();

                return;
            }

            var gateways = await _apiClient.GetGatewaysByCompanyIdAsync(
                    _session.CompanyId);

            Gateways.Clear();

            foreach (var gateway in gateways) Gateways.Add(gateway);

            // NO GATEWAYS
            if (Gateways.Count == 0)
            {
                ClearGatewaySelection();

                ErrorMessage = null;
                
                RaiseCommandStates();

                return;
            }

            // RESTORE PREVIOUS GATEWAY
            if (_session.GatewayId.HasValue)
            {
                var existingGateway = Gateways.FirstOrDefault(
                        x => x.Id == _session.GatewayId.Value);

                if (existingGateway != null)
                {
                    SelectedGateway = existingGateway;

                    return;
                }
            }

            // SELECT FIRST
            SelectedGateway = Gateways[0];
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            IsOnline = false;

            ErrorMessage = "Unable to connect to the SmartX API.";

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

    // ADD
    private async Task AddGatewayAsync()
    {
        if (!CanAddGateway) return;

        _navigationService.NavigateTo<GatewaySetupPage>();

        await Task.CompletedTask;
    }

    // EDIT
    private async Task EditGatewayAsync()
    {
        if (!CanEditGateway) return;

        if (Gateway == null) return;

        _session.SelectGateway(Gateway.Id, Gateway.Name);

        _navigationService
            .NavigateTo<GatewayEditPage>();

        await Task.CompletedTask;
    }

    // DELETE
    private async Task DeleteGatewayAsync()
    {
        if (!CanDeleteGateway) return;

        if (Gateway == null) return;

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var gatewayId = Gateway.Id;

            var deleted = await _apiClient.DeleteGatewayAsync(
                    gatewayId);

            if (!deleted)
            {
                ErrorMessage = "The gateway could not be deleted.";

                return;
            }

            var deletedGateway =
                Gateways.FirstOrDefault(
                    x => x.Id == gatewayId);

            if (deletedGateway != null)
                Gateways.Remove(deletedGateway);

            if (Gateways.Count == 0)
            {
                ClearGatewaySelection();

                return;
            }

            SelectedGateway = Gateways[0];
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

    // SENSOR DATA
    private async Task OpenSensorsAsync()
    {
        if (!CanOpenGatewayArea) return;

        _navigationService.NavigateTo<SensorsPage>();

        await Task.CompletedTask;
    }

    // COMMAND HISTORY
    private async Task OpenCommandHistoryAsync()
    {
        if (!CanOpenGatewayArea) return;

        _navigationService.NavigateTo<HistoryPage>();

        await Task.CompletedTask;
    }

    // NETWORK
    private async Task OpenNetworkAsync()
    {
        if (!CanOpenGatewayArea) return;

        _navigationService.NavigateTo<NetworkPage>();

        await Task.CompletedTask;
    }

    // CLEAR SELECTION
    private void ClearGatewaySelection()
    {
        _selectedGateway = null;

        OnPropertyChanged(nameof(SelectedGateway));

        Gateway = null;

        _session.ClearGateway();

        RaiseCommandStates();
    }

    // COMMAND STATE
    private void RaiseCommandStates()
    {
        AddGatewayCommand?.RaiseCanExecuteChanged();

        EditGatewayCommand?.RaiseCanExecuteChanged();

        DeleteGatewayCommand?.RaiseCanExecuteChanged();

        ViewSensorsCommand?.RaiseCanExecuteChanged();

        ViewCommandHistoryCommand?.RaiseCanExecuteChanged();

        ViewNetworkCommand?.RaiseCanExecuteChanged();

        OnPropertyChanged(nameof(CanAddGateway));
        OnPropertyChanged(nameof(CanEditGateway));
        OnPropertyChanged(nameof(CanDeleteGateway));
        OnPropertyChanged(nameof(CanOpenGatewayArea));

        OnPropertyChanged(nameof(IsAdministrator));
        OnPropertyChanged(nameof(IsTechnician));
        OnPropertyChanged(nameof(IsSuperAdmin));

        OnPropertyChanged(nameof(GatewayCrudVisibility));
        OnPropertyChanged(nameof(GatewayFeaturesVisibility));
        OnPropertyChanged(nameof(NoGatewayVisibility));
    }

    // PROPERTY CHANGED
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this, new PropertyChangedEventArgs(propertyName));
    }

    private void OnSessionChanged(
    object? sender,
    PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SmartXSession.Role) ||
            e.PropertyName == nameof(SmartXSession.CompanyId) ||
            e.PropertyName == nameof(SmartXSession.GatewayId))
        {
            OnPropertyChanged(nameof(IsAdministrator));
            OnPropertyChanged(nameof(IsTechnician));
            OnPropertyChanged(nameof(IsSuperAdmin));

            OnPropertyChanged(nameof(GatewayCrudVisibility));
            OnPropertyChanged(nameof(GatewayFeaturesVisibility));
            OnPropertyChanged(nameof(NoGatewayVisibility));

            RaiseCommandStates();
        }
    }
}