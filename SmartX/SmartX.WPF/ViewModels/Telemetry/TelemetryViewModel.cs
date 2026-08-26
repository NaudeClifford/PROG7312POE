using SmartX.Domain.Entities;
using SmartX.Domain.Enums;
using SmartX.WPF.Navigation;
using SmartX.WPF.Repositories.Local;
using SmartX.WPF.Services;
using SmartX.WPF.ViewModels.Base;
using SmartX.WPF.Views.Pages.Gateway;
using SmartX.WPF.Views.Pages.Sensor;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

using DomainSensor = SmartX.Domain.Entities.Sensor;
using DomainTelemetry = SmartX.Domain.Entities.Telemetry;

namespace SmartX.WPF.ViewModels.Telemetry;

public class TelemetryViewModel : ViewModelBase
{
    private readonly ILocalTelemetryCache _telemetryCache;
    private readonly ILocalSensorCache _sensorCache;
    private readonly SmartXSession _session;
    private readonly INavigationService _navigationService;

    private bool _isBusy;
    private bool _isOnline;

    private string _errorMessage = string.Empty;

    private Guid? _selectedSensorId;

    private DateTime? _fromDate;
    private DateTime? _toDate;

    // =========================================================
    // COLLECTIONS
    // =========================================================

    public ObservableCollection<DomainTelemetry> Telemetry { get; } = [];

    public ObservableCollection<DomainSensor> Sensors { get; } = [];

    // =========================================================
    // SELECTED SENSOR
    // =========================================================

    public Guid? SelectedSensorId
    {
        get => _selectedSensorId;

        set
        {
            if (!SetProperty(
                    ref _selectedSensorId,
                    value))
            {
                return;
            }

            OnPropertyChanged(nameof(SelectedSensor));

            RaiseFilterState();

            _ = ReloadTelemetryAsync();
        }
    }

    public DomainSensor? SelectedSensor =>
        _selectedSensorId.HasValue
            ? Sensors.FirstOrDefault(
                x => x.Id == _selectedSensorId.Value)
            : null;

    public string SelectedSensorName =>
        SelectedSensor?.Name ?? "All Sensors";

    // =========================================================
    // GATEWAY
    // =========================================================

    public Guid? SelectedGatewayId =>
        _session.GatewayId;

    public string CurrentGatewayName =>
        _session.GatewayName ?? "No Gateway Selected";

    public bool HasGateway =>
        _session.GatewayId.HasValue;

    // =========================================================
    // DATE FILTERS
    // =========================================================

    public DateTime? FromDate
    {
        get => _fromDate;

        set
        {
            if (!SetProperty(
                    ref _fromDate,
                    value))
            {
                return;
            }

            _ = ReloadTelemetryAsync();
        }
    }

    public DateTime? ToDate
    {
        get => _toDate;

        set
        {
            if (!SetProperty(
                    ref _toDate,
                    value))
            {
                return;
            }

            _ = ReloadTelemetryAsync();
        }
    }

    // =========================================================
    // STATE
    // =========================================================

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

    public string ErrorMessage
    {
        get => _errorMessage;

        private set => SetProperty(
            ref _errorMessage,
            value);
    }

    // =========================================================
    // TELEMETRY VALUES
    // =========================================================

    public DomainTelemetry? LatestTelemetry =>
        Telemetry
            .OrderByDescending(x => x.Timestamp)
            .FirstOrDefault();

    public double? Temperature =>
        LatestTelemetry?.Temperature;

    public double? Voltage =>
        LatestTelemetry?.Voltage;

    public double? Current =>
        LatestTelemetry?.Current;

    public double? Power =>
        LatestTelemetry?.Power;

    // =========================================================
    // COMMANDS
    // =========================================================

    public AsyncRelayCommand BackToSensorsCommand { get; }

    public AsyncRelayCommand BackToGatewaysCommand { get; }

    public AsyncRelayCommand ClearFiltersCommand { get; }

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public TelemetryViewModel(
        ILocalTelemetryCache telemetryCache,
        ILocalSensorCache sensorCache,
        SmartXSession session,
        INavigationService navigationService)
    {
        _telemetryCache = telemetryCache;
        _sensorCache = sensorCache;
        _session = session;
        _navigationService = navigationService;

        BackToSensorsCommand =
            new AsyncRelayCommand(
                BackToSensorsAsync);

        BackToGatewaysCommand =
            new AsyncRelayCommand(
                BackToGatewaysAsync);

        ClearFiltersCommand =
            new AsyncRelayCommand(
                ClearFiltersAsync);

        _session.PropertyChanged +=
            Session_PropertyChanged;
    }

    // =========================================================
    // LOAD PAGE
    // =========================================================

    public async Task LoadAsync(
    Guid? sensorId = null,
    CancellationToken cancellationToken = default)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            Telemetry.Clear();
            Sensors.Clear();

            // -------------------------------------------------
            // GATEWAY REQUIRED
            // -------------------------------------------------

            if (!_session.GatewayId.HasValue ||
                _session.GatewayId.Value == Guid.Empty)
            {
                _selectedSensorId = null;

                OnPropertyChanged(nameof(SelectedSensorId));
                OnPropertyChanged(nameof(SelectedSensor));
                OnPropertyChanged(nameof(SelectedSensorName));

                ErrorMessage = "No gateway selected.";

                RaiseTelemetryProperties();

                return;
            }

            var gatewayId = _session.GatewayId.Value;

            // -------------------------------------------------
            // LOAD SENSORS FOR CURRENT GATEWAY
            // -------------------------------------------------

            var sensors =
                await _sensorCache.GetByGatewayIdAsync(
                    gatewayId,
                    cancellationToken);

            foreach (var sensor in sensors)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Sensors.Add(sensor);
            }

            // -------------------------------------------------
            // SENSOR SELECTION
            // -------------------------------------------------

            if (sensorId.HasValue &&
                Sensors.Any(x => x.Id == sensorId.Value))
            {
                // Came from Sensors page.
                _selectedSensorId = sensorId.Value;
            }
            else
            {
                // Came from header/navigation.
                //
                // Default = ALL SENSORS.
                _selectedSensorId = null;
            }

            OnPropertyChanged(nameof(SelectedSensorId));
            OnPropertyChanged(nameof(SelectedSensor));
            OnPropertyChanged(nameof(SelectedSensorName));

            // -------------------------------------------------
            // LOAD TELEMETRY
            // -------------------------------------------------

            await LoadTelemetryAsync(
                cancellationToken);

            RaiseFilterState();
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
        }
    }

    public async Task LoadSensorAsync(Guid sensorId)
    {
        SelectedSensorId = sensorId;

        await LoadTelemetryAsync();
    }
    // =========================================================
    // ONLINE
    // =========================================================

    private async Task<bool> CheckOnlineAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            return await Task.Run(
                () => true,
                cancellationToken);
        }
        catch
        {
            return false;
        }
    }

    // =========================================================
    // LOAD TELEMETRY
    // =========================================================

    private async Task LoadTelemetryAsync(
    CancellationToken cancellationToken = default)
    {
        if (!_session.GatewayId.HasValue)
        {
            Telemetry.Clear();

            RaiseTelemetryProperties();

            return;
        }

        IReadOnlyList<DomainTelemetry> telemetry;

        // -------------------------------------------------
        // SENSOR FILTER
        // -------------------------------------------------

        if (_selectedSensorId.HasValue)
        {
            telemetry =
                await _telemetryCache.GetBySensorIdAsync(
                    _selectedSensorId.Value,
                    cancellationToken);
        }
        else
        {
            // ALL SENSORS FOR CURRENT GATEWAY
            telemetry =
                await _telemetryCache.GetByGatewayIdAsync(
                    _session.GatewayId.Value,
                    cancellationToken);
        }

        // -------------------------------------------------
        // SORT
        // -------------------------------------------------

        IEnumerable<DomainTelemetry> filtered =
            telemetry.OrderByDescending(
                x => x.Timestamp);

        // -------------------------------------------------
        // DATE FILTER
        // -------------------------------------------------

        if (_fromDate.HasValue)
        {
            filtered = filtered.Where(
                x => x.Timestamp >= _fromDate.Value);
        }

        if (_toDate.HasValue)
        {
            var endDate =
                _toDate.Value.Date.AddDays(1);

            filtered = filtered.Where(
                x => x.Timestamp < endDate);
        }

        // Always newest first.
        filtered =
            filtered.OrderByDescending(
                x => x.Timestamp);

        // -------------------------------------------------
        // UPDATE COLLECTION
        // -------------------------------------------------

        Telemetry.Clear();

        foreach (var item in filtered)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Telemetry.Add(item);
        }

        RaiseTelemetryProperties();
    }

    // =========================================================
    // RELOAD
    // =========================================================

    private async Task ReloadTelemetryAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            await LoadTelemetryAsync();
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
    // SESSION CHANGE
    // =========================================================

    private async void Session_PropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName !=
                nameof(SmartXSession.GatewayId) &&
            e.PropertyName !=
                nameof(SmartXSession.GatewayName))
        {
            return;
        }

        OnPropertyChanged(
            nameof(SelectedGatewayId));

        OnPropertyChanged(
            nameof(CurrentGatewayName));

        OnPropertyChanged(
            nameof(HasGateway));

        // Gateway changed.
        //
        // The previously selected sensor belongs to the
        // previous gateway, so clear it.
        if (e.PropertyName ==
            nameof(SmartXSession.GatewayId))
        {
            _selectedSensorId = null;

            OnPropertyChanged(
                nameof(SelectedSensorId));

            OnPropertyChanged(
                nameof(SelectedSensor));

            OnPropertyChanged(
                nameof(SelectedSensorName));

            await LoadAsync();
        }
    }

    // =========================================================
    // BACK TO SENSORS
    // =========================================================

    private async Task BackToSensorsAsync()
    {
        _navigationService
            .NavigateTo<SensorsPage>();

        await Task.CompletedTask;
    }

    // =========================================================
    // BACK TO GATEWAYS
    // =========================================================

    private async Task BackToGatewaysAsync()
    {
        _navigationService
            .NavigateTo<GatewayPage>();

        await Task.CompletedTask;
    }

    // =========================================================
    // CLEAR FILTERS
    // =========================================================

    private async Task ClearFiltersAsync()
    {
        try
        {
            IsBusy = true;

            _fromDate = null;
            _toDate = null;

            OnPropertyChanged(
                nameof(FromDate));

            OnPropertyChanged(
                nameof(ToDate));

            await LoadTelemetryAsync();
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
    // FILTER STATE
    // =========================================================

    private void RaiseFilterState()
    {
        OnPropertyChanged(
            nameof(SelectedSensor));

        OnPropertyChanged(
            nameof(SelectedSensorName));

        OnPropertyChanged(
            nameof(SelectedGatewayId));

        OnPropertyChanged(
            nameof(CurrentGatewayName));

        OnPropertyChanged(
            nameof(HasGateway));
    }

    // =========================================================
    // TELEMETRY PROPERTIES
    // =========================================================

    private void RaiseTelemetryProperties()
    {
        OnPropertyChanged(
            nameof(LatestTelemetry));

        OnPropertyChanged(
            nameof(Temperature));

        OnPropertyChanged(
            nameof(Voltage));

        OnPropertyChanged(
            nameof(Current));

        OnPropertyChanged(
            nameof(Power));
    }

    // =========================================================
    // COMMAND STATE
    // =========================================================

    private void RaiseCommandStates()
    {
        BackToSensorsCommand?
            .RaiseCanExecuteChanged();

        BackToGatewaysCommand?
            .RaiseCanExecuteChanged();

        ClearFiltersCommand?
            .RaiseCanExecuteChanged();
    }
}