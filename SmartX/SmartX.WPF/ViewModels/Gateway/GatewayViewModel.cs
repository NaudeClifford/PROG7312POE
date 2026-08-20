using SmartX.Domain.Enums;
using SmartX.Shared.DTOs;
using SmartX.WPF.Navigation;
using SmartX.WPF.Services;
using SmartX.WPF.Services.Api;
using SmartX.WPF.ViewModels.Base;
using SmartX.WPF.Views.Pages.Gateway;
using SmartX.WPF.Views.Pages.History;
using SmartX.WPF.Views.Pages.Sensor;
using SmartX.WPF.Views.Pages.Network;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

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


    public GatewayViewModel(
        ISmartXApiClient apiClient,
        SmartXSession session,
        INavigationService navigationService)
    {
        _apiClient = apiClient;
        _session = session;
        _navigationService = navigationService;

        AddGatewayCommand = new AsyncRelayCommand(
            AddGatewayAsync,
            () => CanAddGateway);

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


    // =========================================================
    // GATEWAYS
    // =========================================================

    public ObservableCollection<GatewayDto> Gateways { get; }
        = new();


    // =========================================================
    // CURRENT GATEWAY
    // =========================================================

    public GatewayDto? Gateway
    {
        get => _gateway;

        private set
        {
            if (_gateway == value)
                return;

            _gateway = value;

            OnPropertyChanged();
        }
    }


    // =========================================================
    // SELECTED GATEWAY
    // =========================================================

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
                    _selectedGateway.Id);
            }

            RaiseCommandStates();
        }
    }


    // =========================================================
    // BUSY
    // =========================================================

    public bool IsBusy
    {
        get => _isBusy;

        private set
        {
            if (_isBusy == value)
                return;

            _isBusy = value;

            OnPropertyChanged();

            RaiseCommandStates();
        }
    }


    // =========================================================
    // ERROR
    // =========================================================

    public string? ErrorMessage
    {
        get => _errorMessage;

        private set
        {
            if (_errorMessage == value)
                return;

            _errorMessage = value;

            OnPropertyChanged();
        }
    }


    // =========================================================
    // PERMISSIONS
    // =========================================================

    public bool CanAddGateway =>
        _session.Role == UserRole.Administrator;

    public bool CanOpenGatewayArea =>
        !IsBusy &&
        Gateway != null &&
        _session.Role is
            UserRole.Technician or
            UserRole.Administrator;


    // =========================================================
    // COMMANDS
    // =========================================================

    public AsyncRelayCommand AddGatewayCommand { get; }

    public AsyncRelayCommand ViewSensorsCommand { get; }

    public AsyncRelayCommand ViewCommandHistoryCommand { get; }

    public AsyncRelayCommand ViewNetworkCommand { get; }


    // =========================================================
    // LOAD
    // =========================================================

    public async Task LoadAsync()
    {
        if (_session.CompanyId == Guid.Empty)
        {
            ErrorMessage =
                "No company is associated with this session.";

            return;
        }

        if (_session.Role == UserRole.SuperAdmin)
        {
            ErrorMessage =
                "SuperAdmin accounts cannot access gateways.";

            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            /*
             * Use the existing company-specific API call.
             */

            var gateways =
                await _apiClient.GetGatewaysByCompanyIdAsync(
                    _session.CompanyId);

            Gateways.Clear();

            foreach (var gateway in gateways)
            {
                Gateways.Add(gateway);
            }

            if (Gateways.Count == 0)
            {
                Gateway = null;
                SelectedGateway = null;

                ErrorMessage =
                    "No gateways are available for this company.";

                return;
            }


            /*
             * Restore previously selected gateway.
             */

            if (_session.GatewayId.HasValue)
            {
                var existingGateway =
                    Gateways.FirstOrDefault(
                        x => x.Id == _session.GatewayId.Value);

                if (existingGateway != null)
                {
                    SelectedGateway =
                        existingGateway;

                    return;
                }
            }


            /*
             * Otherwise select first gateway.
             */

            SelectedGateway =
                Gateways[0];
        }
        catch (Exception ex)
        {
            ErrorMessage =
                ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }


    // =========================================================
    // ADD GATEWAY
    // =========================================================

    private async Task AddGatewayAsync()
    {
        if (!CanAddGateway)
            return;

        _navigationService
            .NavigateTo<GatewaySetupPage>();

        await Task.CompletedTask;
    }


    // =========================================================
    // SENSOR DATA & TELEMETRY
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
    // COMMAND STREAM & HISTORY
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
    // NETWORK & MESH
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
    // COMMAND STATE
    // =========================================================

    private void RaiseCommandStates()
    {
        AddGatewayCommand?.RaiseCanExecuteChanged();

        ViewSensorsCommand?.RaiseCanExecuteChanged();

        ViewCommandHistoryCommand?.RaiseCanExecuteChanged();

        ViewNetworkCommand?.RaiseCanExecuteChanged();
    }


    // =========================================================
    // PROPERTY CHANGED
    // =========================================================

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}