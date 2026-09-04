using SmartX.Application.Requests.Telemetry;
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
    private Guid? _selectedSensorId;

    private string _selectedSensorFilter = "All";

    private DateTime? _fromDate;
    private DateTime? _toDate;

    // COLLECTIONS
    private readonly TelemetryCollection _telemetryCollection = new();

    private DomainTelemetry[] _telemetryArray = [];

    public ObservableCollection<DomainTelemetry> Telemetry { get; } = [];

    public ObservableCollection<DomainTelemetry> FilteredTelemetry { get; } = [];

    public ObservableCollection<DomainSensor> Sensors { get; } = [];

    public ObservableCollection<string> SensorFilters { get; } =
    [
        "All"
    ];

    // SELECTED SENSOR

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
            }
            else
            {
                _selectedSensorFilter = "All";

                OnPropertyChanged(
                    nameof(SelectedSensorFilter));
            }

            ApplyFilters();
            RaiseFilterState();
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

    public DomainTelemetry? LatestTelemetry =>
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

    // CONSTRUCTOR

    public TelemetryViewModel(
        ILocalTelemetryCache telemetryCache,
        ILocalSensorCache sensorCache,
        INavigationService navigationService,
        IConnectivityService connectivityService,
        SmartXSession session,
        ISmartXApiClient apiClient) : base(
            connectivityService,
            session)
    {
        _telemetryCache = telemetryCache;
        _sensorCache = sensorCache;
        _apiClient = apiClient;
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

        AddTelemetryCommand =
            new AsyncRelayCommand(
                AddTelemetryAsync);
    }

    // LOAD PAGE

    public async Task LoadAsync(
        Guid? sensorId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            await CheckOnlineAsync(
                cancellationToken);

            Telemetry.Clear();
            FilteredTelemetry.Clear();
            Sensors.Clear();
            SensorFilters.Clear();

            SensorFilters.Add("All");

            // GATEWAY REQUIRED

            if (Session.GatewayId is not Guid gatewayId ||
                gatewayId == Guid.Empty)
            {
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

                ErrorMessage =
                    "No gateway selected.";

                RaiseTelemetryProperties();

                return;
            }

            // LOAD SENSORS FOR CURRENT GATEWAY

            var sensors =
                await _sensorCache.GetByGatewayIdAsync(
                    gatewayId,
                    cancellationToken);

            foreach (var sensor in sensors)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Sensors.Add(sensor);

                SensorFilters.Add(
                    sensor.Name);
            }

            // SENSOR SELECTION

            if (sensorId.HasValue &&
                Sensors.Any(
                    x => x.Id == sensorId.Value))
            {
                _selectedSensorId =
                    sensorId.Value;

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

            OnPropertyChanged(
                nameof(SelectedSensorId));

            OnPropertyChanged(
                nameof(SelectedSensor));

            OnPropertyChanged(
                nameof(SelectedSensorName));

            OnPropertyChanged(
                nameof(SelectedSensorFilter));

            // LOAD TELEMETRY

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

    public Task LoadSensorAsync(
        Guid sensorId)
    {
        SelectedSensorId = sensorId;

        return Task.CompletedTask;
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

        var telemetry =
            await _telemetryCache.GetByGatewayIdAsync(
                Session.GatewayId.Value,
                cancellationToken);

        _telemetryCollection.Clear();

        foreach (var item in telemetry)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _telemetryCollection.Add(item);
        }

        _telemetryArray =  _telemetryCollection.ToArray();

        //Reclusion
        TotalPower = CalculateTotalPower(_telemetryArray, 0);

        OnPropertyChanged(nameof(TotalPower));

        if (_telemetryArray.Length >= 2)
        {
            TelemetryReading first = new(_telemetryArray[0]);

            TelemetryReading second = new(_telemetryArray[1]);

            if (first > second)
            {
                // First reading is newer.
            }
        }

        Telemetry.Clear();


        foreach (var item in _telemetryCollection.Items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Telemetry.Add(item);
        }

        ApplyFilters();
    }

    // FILTERING

    private void ApplyFilters()
    {
        FilteredTelemetry.Clear();

        IEnumerable<DomainTelemetry> filtered =
            Telemetry;

        if (!string.Equals(
                SelectedSensorFilter,
                "All",
                StringComparison.OrdinalIgnoreCase))
        {
            var sensor =
                Sensors.FirstOrDefault(
                    x => x.Name.Equals(
                        SelectedSensorFilter,
                        StringComparison.OrdinalIgnoreCase));

            if (sensor is not null)
            {
                filtered =
                    filtered.Where(
                        x => x.SensorId == sensor.Id);
            }
            else
            {
                filtered =
                    Enumerable.Empty<DomainTelemetry>();
            }
        }

        if (FromDate.HasValue)
        {
            filtered =
                filtered.Where(
                    x => x.Timestamp >=
                         FromDate.Value);
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

        foreach (var item in filtered)
            FilteredTelemetry.Add(item);

        RaiseTelemetryProperties();
    }

    // ADD TELEMETRY
    // DEVELOPMENT ONLY

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

            // CHECK CONNECTIVITY
            await CheckOnlineAsync(
                CancellationToken.None);

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

            var telemetry = new DomainTelemetry
            {
                Id = savedTelemetry.Id,
                SensorId = savedTelemetry.SensorId,
                Timestamp = savedTelemetry.Timestamp,
                Voltage = savedTelemetry.Voltage,
                Current = savedTelemetry.Current,
                Power = savedTelemetry.Power,
                Temperature = savedTelemetry.Temperature,
                CreatedAt = savedTelemetry.CreatedAt,
                UpdatedAt = savedTelemetry.UpdatedAt
            };


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
                telemetry);

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

    // RELOAD

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