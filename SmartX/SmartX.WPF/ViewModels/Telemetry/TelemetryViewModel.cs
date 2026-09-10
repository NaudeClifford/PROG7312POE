using SmartX.Application.Requests.Telemetry;
using SmartX.Shared.Mapping;
using SmartX.WPF.Navigation;
using SmartX.WPF.Repositories.Local;
using SmartX.WPF.Services.Api;
using SmartX.WPF.Services.Connectivity;
using SmartX.WPF.Services.Session;
using SmartX.WPF.ViewModels.Base;
using SmartX.WPF.Views.Pages.Gateway;
using SmartX.WPF.Views.Pages.Sensor;
using System.Collections.ObjectModel;
using System.ComponentModel;

using DomainSensor = SmartX.Domain.Entities.Sensor;
using DomainTelemetry = SmartX.Domain.Entities.Telemetry;

namespace SmartX.WPF.ViewModels.Telemetry;

public class TelemetryViewModel : ViewModelBase
{
    private readonly ILocalTelemetryCache _telemetryCache;
    private readonly ILocalSensorCache _sensorCache;
    private readonly INavigationService _navigationService;
    private readonly ISmartXApiClient _apiClient;
    private readonly IMapper _mapper;


    private Guid? _selectedSensorId;

    private string _selectedSensorFilter = "All";

    private DateTime? _fromDate;
    private DateTime? _toDate;

    // COLLECTIONS
    private readonly TelemetryCollection _telemetryCollection = new();

    private DomainTelemetry[] _telemetryArray = [];

    public ObservableCollection<TelemetryDisplayModel> Telemetry { get; } = [];

    public ObservableCollection<TelemetryDisplayModel> FilteredTelemetry { get; } = [];

    public ObservableCollection<DomainSensor> Sensors { get; } = [];

    public ObservableCollection<string> SensorFilters { get; } =
    [
        "All"
    ];

    // SELECTED SENSOR
    public string GetSensorName(Guid sensorId)
    {
        return Sensors.FirstOrDefault(
                   x => x.Id == sensorId)
               ?.Name
               ?? "Unknown Sensor";
    }

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
            OnPropertyChanged(nameof(SelectedSensorName));

            if (value.HasValue)
            {
                var sensor =
                    Sensors.FirstOrDefault(
                        x => x.Id == value.Value);

                _selectedSensorFilter =
                    sensor?.Name ?? "All";

                OnPropertyChanged(
                    nameof(SelectedSensorFilter));

                RaiseCommandStates();

            }
            else
            {
                _selectedSensorFilter = "All";

                OnPropertyChanged(
                    nameof(SelectedSensorFilter));

                RaiseCommandStates();


            }

            ApplyFilters();
            RaiseFilterState();
            RaiseCommandStates();
        }
    }

    public DomainSensor? SelectedSensor =>
        _selectedSensorId.HasValue
            ? Sensors.FirstOrDefault(
                x => x.Id == _selectedSensorId.Value)
            : null;

    public string SelectedSensorName =>
        SelectedSensor?.Name ?? "All Sensors";

    // SENSOR FILTER

    public string SelectedSensorFilter
    {
        get => _selectedSensorFilter;

        set
        {
            if (!SetProperty(
                    ref _selectedSensorFilter,
                    value))
            {
                return;
            }

            if (string.Equals(
                    value,
                    "All",
                    StringComparison.OrdinalIgnoreCase))
            {
                _selectedSensorId = null;

                OnPropertyChanged(
                    nameof(SelectedSensorId));

                OnPropertyChanged(
                    nameof(SelectedSensor));

                OnPropertyChanged(
                    nameof(SelectedSensorName));
            }
            else
            {
                var sensor =
                    Sensors.FirstOrDefault(
                        x => x.Name.Equals(
                            value,
                            StringComparison.OrdinalIgnoreCase));

                if (sensor is not null)
                {
                    _selectedSensorId = sensor.Id;

                    OnPropertyChanged(
                        nameof(SelectedSensorId));

                    OnPropertyChanged(
                        nameof(SelectedSensor));

                    OnPropertyChanged(
                        nameof(SelectedSensorName));
                }
            }

            ApplyFilters();
            RaiseFilterState();
        }
    }

    // GATEWAY

    public Guid? SelectedGatewayId =>
        Session.GatewayId;

    public bool HasGateway =>
        Session.GatewayId.HasValue;

    // DATE FILTERS

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

            ApplyFilters();
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

            ApplyFilters();
        }
    }

    // TELEMETRY VALUES

    public TelemetryDisplayModel? LatestTelemetry =>
        FilteredTelemetry
            .OrderByDescending(
                x => x.Timestamp)
            .FirstOrDefault();


    public double? Temperature =>
        LatestTelemetry?.Temperature;

    public double? Voltage =>
        LatestTelemetry?.Voltage;

    public double? Current =>
        LatestTelemetry?.Current;

    public double? Power =>
        LatestTelemetry?.Power;

    public double TotalPower { get; private set; }
    // COMMANDS

    public AsyncRelayCommand BackToSensorsCommand { get; }

    public AsyncRelayCommand BackToGatewaysCommand { get; }

    public AsyncRelayCommand ClearFiltersCommand { get; }

    public AsyncRelayCommand AddTelemetryCommand { get; }

    public AsyncRelayCommand ReloadTelemetryCommand { get; }

    // CONSTRUCTOR

    public TelemetryViewModel(
        ILocalTelemetryCache telemetryCache,
        ILocalSensorCache sensorCache,
        INavigationService navigationService,
        IConnectivityService connectivityService,
        SmartXSession session,
        IMapper mapper,
        ISmartXApiClient apiClient) : base(
            connectivityService,
            session)
    {
        _telemetryCache = telemetryCache;
        _sensorCache = sensorCache;
        _apiClient = apiClient;
        _navigationService = navigationService;
        _mapper = mapper;

        BackToSensorsCommand =
            new AsyncRelayCommand(
                BackToSensorsAsync);

        BackToGatewaysCommand =
            new AsyncRelayCommand(
                BackToGatewaysAsync);

        ClearFiltersCommand =
            new AsyncRelayCommand(
                ClearFiltersAsync);

        AddTelemetryCommand =
            new AsyncRelayCommand(
                AddTelemetryAsync,
                CanAddTelemetry);

        ReloadTelemetryCommand =
            new AsyncRelayCommand(
        ReloadTelemetryAsync,
        CanReloadTelemetry);

    }

    // LOAD PAGE

    public async Task LoadAsync(
    Guid? sensorId = null,
    CancellationToken cancellationToken = default)
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            if (Session.GatewayId is not Guid gatewayId ||
                gatewayId == Guid.Empty)
            {
                _selectedSensorId = null;
                _selectedSensorFilter = "All";

                OnPropertyChanged(nameof(SelectedSensorId));
                OnPropertyChanged(nameof(SelectedSensor));
                OnPropertyChanged(nameof(SelectedSensorName));
                OnPropertyChanged(nameof(SelectedSensorFilter));

                Telemetry.Clear();
                FilteredTelemetry.Clear();

                ErrorMessage = "No gateway selected.";

                RaiseTelemetryProperties();

                return;
            }

            await LoadSensorsAsync(
                gatewayId,
                cancellationToken);

            if (sensorId.HasValue &&
                Sensors.Any(x => x.Id == sensorId.Value))
            {
                _selectedSensorId = sensorId.Value;

                var selectedSensor =
                    Sensors.First(
                        x => x.Id == sensorId.Value);

                _selectedSensorFilter =
                    selectedSensor.Name;
            }
            else
            {
                _selectedSensorId = null;
                _selectedSensorFilter = "All";
            }

            OnPropertyChanged(nameof(SelectedSensorId));
            OnPropertyChanged(nameof(SelectedSensor));
            OnPropertyChanged(nameof(SelectedSensorName));
            OnPropertyChanged(nameof(SelectedSensorFilter));

            await LoadTelemetryAsync(
                cancellationToken);

            RaiseFilterState();
            RaiseCommandStates();
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

    private async Task LoadSensorsAsync(
    Guid gatewayId,
    CancellationToken cancellationToken = default)
    {
        Sensors.Clear();
        SensorFilters.Clear();
        SensorFilters.Add("All");

        var sensors =
            await _sensorCache.GetByGatewayIdAsync(
                gatewayId,
                cancellationToken);

        if (sensors is null || sensors.Count == 0)
        {
            var sensorDtos =
                await _apiClient.GetSensorsByGatewayIdAsync(
                    gatewayId,
                    cancellationToken);

            foreach (var sensorDto in sensorDtos)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var sensor =
                    _mapper.Map<DomainSensor>(sensorDto);

                await _sensorCache.UpdateAsync(
                    sensor,
                    cancellationToken);

                Sensors.Add(sensor);
                SensorFilters.Add(sensor.Name);
            }

            return;
        }

        foreach (var sensor in sensors)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Sensors.Add(sensor);
            SensorFilters.Add(sensor.Name);
        }
    }

    // LOAD TELEMETRY

    private async Task LoadTelemetryAsync(
    CancellationToken cancellationToken = default)
    {
        if (!Session.GatewayId.HasValue)
        {
            Telemetry.Clear();
            FilteredTelemetry.Clear();

            _telemetryCollection.Clear();
            _telemetryArray = [];

            TotalPower = 0;

            RaiseTelemetryProperties();

            return;
        }

        IEnumerable<DomainTelemetry> telemetry;

        if (_selectedSensorId.HasValue)
        {
            telemetry =
                await _telemetryCache.GetBySensorIdAsync(
                    _selectedSensorId.Value,
                    cancellationToken);
        }
        else
        {
            telemetry =
                await _telemetryCache.GetByGatewayIdAsync(
                    Session.GatewayId.Value,
                    cancellationToken);
        }

        _telemetryCollection.Clear();

        foreach (var item in telemetry)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _telemetryCollection.Add(item);
        }

        _telemetryArray =
            _telemetryCollection.ToArray();

        TotalPower =
            CalculateTotalPower(
                _telemetryArray,
                0);

        OnPropertyChanged(
            nameof(TotalPower));

        Telemetry.Clear();

        foreach (var item in _telemetryCollection.Items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Telemetry.Add(
                CreateDisplayModel(item));
        }

        ApplyFilters();
    }

    // FILTERING
    private void ApplyFilters()
    {
        FilteredTelemetry.Clear();

        IEnumerable<TelemetryDisplayModel> filtered =
            Telemetry;

        if (_selectedSensorId.HasValue)
        {
            var sensorId = _selectedSensorId.Value;

            filtered =
                filtered.Where(
                    x => x.SensorId == sensorId);
        }

        if (FromDate.HasValue)
        {
            filtered =
                filtered.Where(
                    x => x.Timestamp >= FromDate.Value);
        }

        if (ToDate.HasValue)
        {
            var endDate =
                ToDate.Value.Date.AddDays(1);

            filtered =
                filtered.Where(
                    x => x.Timestamp < endDate);
        }

        filtered =
            filtered.OrderByDescending(
                x => x.Timestamp);

        foreach (var telemetry in filtered)
        {
            FilteredTelemetry.Add(telemetry);
        }

        RaiseTelemetryProperties();
    }



    // ADD TELEMETRY
    // DEVELOPMENT ONLY
    private bool CanAddTelemetry()
    {
        return !IsBusy &&
               IsOnline &&
               SelectedSensorId.HasValue &&
               HasGateway;
    }

    private async Task AddTelemetryAsync()
    {
        if (IsBusy)
            return;

        if (!SelectedSensorId.HasValue)
        {
            ErrorMessage =
                "Select a sensor before adding telemetry.";

            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            var random = new Random();

            var request = new CreateTelemetryRequest
            {
                SensorId = SelectedSensorId.Value,
                Timestamp = DateTime.UtcNow,

                Voltage =
                 Math.Round(
                     220 + random.NextDouble() * 20,
                     2),

                Current =
                 Math.Round(
                     1 + random.NextDouble() * 5,
                     2),

                Power =
                 Math.Round(
                     500 + random.NextDouble() * 500,
                     2),

                Temperature =
                 Math.Round(
                     20 + random.NextDouble() * 15,
                     2)
            };
            // SEND TO API
            var telemetryId =
                await _apiClient.CreateTelemetryAsync(
                    request,
                    CancellationToken.None);

            var savedTelemetry =
            await _apiClient.GetTelemetryByIdAsync(
                telemetryId,
                CancellationToken.None);

            if (savedTelemetry is null)
            {
                throw new InvalidOperationException(
                    "Telemetry was created, but the API " +
                    "did not return the saved telemetry.");
            }

            var telemetry =
                _mapper.Map<DomainTelemetry>(
                    savedTelemetry);

            // UPDATE LOCAL SQLITE CACHE
            await _telemetryCache.UpdateAsync(
                telemetry,
                CancellationToken.None);

            // ADD TO WRAPPER
            _telemetryCollection.Add(
                telemetry);

            // REBUILD ARRAY
            _telemetryArray =
                _telemetryCollection.ToArray();

            // RUN RECURSION AGAIN
            TotalPower =
                CalculateTotalPower(
                    _telemetryArray,
                    0);

            OnPropertyChanged(
                nameof(TotalPower));

            // ADD TO UI COLLECTION
            Telemetry.Add(
                CreateDisplayModel(telemetry));

            // REAPPLY FILTERS
            ApplyFilters();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }

        await Task.CompletedTask;
    }


    private TelemetryDisplayModel CreateDisplayModel(
    DomainTelemetry telemetry)
    {
        var sensor =
            Sensors.FirstOrDefault(
                x => x.Id == telemetry.SensorId);

        return new TelemetryDisplayModel
        {
            Id = telemetry.Id,
            SensorId = telemetry.SensorId,
            SensorName = sensor?.Name ?? "Unknown Sensor",
            Timestamp = telemetry.Timestamp,
            Voltage = telemetry.Voltage,
            Current = telemetry.Current,
            Power = telemetry.Power,
            Temperature = telemetry.Temperature
        };
    }

    // RELOAD
    private bool CanReloadTelemetry()
    {
        return !IsBusy &&
               IsOnline &&
               HasGateway;
    }

    private async Task ReloadTelemetryAsync()
    {
        if (IsBusy)
            return;

        try
        {
            await LoadAsync(
                _selectedSensorId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }


    // BACK TO SENSORS

    private async Task BackToSensorsAsync()
    {
        _navigationService
            .NavigateTo<SensorsPage>();

        await Task.CompletedTask;
    }

    // BACK TO GATEWAYS

    private async Task BackToGatewaysAsync()
    {
        _navigationService
            .NavigateTo<GatewayPage>();

        await Task.CompletedTask;
    }

    // CLEAR FILTERS

    private async Task ClearFiltersAsync()
    {
        try
        {
            IsBusy = true;

            _fromDate = null;
            _toDate = null;
            _selectedSensorId = null;
            _selectedSensorFilter = "All";

            OnPropertyChanged(
                nameof(FromDate));

            OnPropertyChanged(
                nameof(ToDate));

            OnPropertyChanged(
                nameof(SelectedSensorId));

            OnPropertyChanged(
                nameof(SelectedSensor));

            OnPropertyChanged(
                nameof(SelectedSensorName));

            OnPropertyChanged(
                nameof(SelectedSensorFilter));

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

    private double CalculateTotalPower(
    DomainTelemetry[] telemetry,
    int index)
    {
        // BASE CASE
        if (index >= telemetry.Length)
        {
            return 0;
        }

        // CURRENT VALUE
        double currentPower =
            telemetry[index].Power ?? 0;

        // RECURSIVE CALL
        return currentPower +
               CalculateTotalPower(
                   telemetry,
                   index + 1);
    }

    // FILTER STATE

    private void RaiseFilterState()
    {
        OnPropertyChanged(
            nameof(SelectedSensor));

        OnPropertyChanged(
            nameof(SelectedSensorName));
    }

    // TELEMETRY PROPERTIES

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

        OnPropertyChanged(
            nameof(TotalPower));
    }


    protected override void RaiseCommandStates()
    {
        AddTelemetryCommand?.RaiseCanExecuteChanged();
        BackToSensorsCommand?.RaiseCanExecuteChanged();
        BackToGatewaysCommand?.RaiseCanExecuteChanged();
        ClearFiltersCommand?.RaiseCanExecuteChanged();
    }

    // SESSION

    protected override async void OnSessionPropertyChanged(
        PropertyChangedEventArgs e)
    {
        base.OnSessionPropertyChanged(e);

        if (e.PropertyName !=
            nameof(SmartXSession.GatewayId))
        {
            return;
        }

        _selectedSensorId = null;
        _selectedSensorFilter = "All";

        OnPropertyChanged(
            nameof(SelectedSensorId));

        OnPropertyChanged(
            nameof(SelectedSensor));

        OnPropertyChanged(
            nameof(SelectedSensorName));

        OnPropertyChanged(
            nameof(SelectedSensorFilter));

        OnPropertyChanged(
            nameof(SelectedGatewayId));

        OnPropertyChanged(
            nameof(HasGateway));

        await LoadAsync();
    }

}