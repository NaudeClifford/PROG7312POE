using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using SmartX.Application.Services.Sensors;
using SmartX.Domain.Enums;
using SmartX.Shared.DTOs;
using SmartX.WPF.Repositories.Local;
using SmartX.WPF.Services.Connectivity;
using SmartX.WPF.Services.Session;
using SmartX.WPF.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Windows.Input;
using DomainSensor = SmartX.Domain.Entities.Sensor;
using DomainTelemetry = SmartX.Domain.Entities.Telemetry;

namespace SmartX.WPF.ViewModels.History;

public sealed class HistoryViewModel : ViewModelBase
{
    private readonly ILocalTelemetryCache _telemetryCache;
    private readonly ILocalSensorCache _sensorCache;
    private readonly ILocalUserBehaviorCache _userBehaviorCache;
    private readonly SmartXSession _session;
    private readonly ISensorCommandService _sensorService;

    private List<DomainTelemetry> _loadedTelemetry = [];

    private Guid? _selectedSensorId;

    private DateTime? _fromDate;

    private DateTime? _toDate;
    private readonly AsyncRelayCommand _loadCommand;
    private readonly AsyncRelayCommand _clearFiltersCommand;
    private readonly AsyncRelayCommand _sendManualCommandCommand;
    private readonly AsyncRelayCommand _undoLastCommandCommand;

    public ICommand LoadCommand =>
    _loadCommand;

    public ICommand ClearFiltersCommand =>
        _clearFiltersCommand;

    public ICommand SendManualCommandCommand =>
        _sendManualCommandCommand;

    public ICommand UndoLastCommandCommand => _undoLastCommandCommand;

    private TelemetryGraphMetric _selectedMetric = TelemetryGraphMetric.Power;

    public TelemetryGraphMetric SelectedMetric
    {
        get => _selectedMetric;

        set
        {
            if (!SetProperty(
                    ref _selectedMetric,
                    value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsPowerSelected));
            OnPropertyChanged(nameof(IsTemperatureSelected));
            OnPropertyChanged(nameof(IsVoltageSelected));
            OnPropertyChanged(nameof(IsCurrentSelected));

            ApplyFilters();
        }
    }

    public ISeries[] GraphSeries { get; private set; } = [];

    public Axis[] GraphXAxes { get; private set; } =
    [
        new Axis
    {
        Name = "Time"
    }
    ];

    public Axis[] GraphYAxes { get; private set; } =
    [
        new Axis
    {
        Name = "Power (W)"
    }
    ];

    public string GraphMetricName =>
        SelectedMetric switch
        {
            TelemetryGraphMetric.Power => "Power",
            TelemetryGraphMetric.Temperature => "Temperature",
            TelemetryGraphMetric.Voltage => "Voltage",
            TelemetryGraphMetric.Current => "Current",
            _ => "Power"
        };

    public string GraphUnit =>
        SelectedMetric switch
        {
            TelemetryGraphMetric.Power => "W",
            TelemetryGraphMetric.Temperature => "°C",
            TelemetryGraphMetric.Voltage => "V",
            TelemetryGraphMetric.Current => "A",
            _ => "W"
        };

    public bool IsPowerSelected
    {
        get => SelectedMetric == TelemetryGraphMetric.Power;
        set
        {
            if (value)
                SelectedMetric = TelemetryGraphMetric.Power;
        }
    }

    public bool IsTemperatureSelected
    {
        get => SelectedMetric == TelemetryGraphMetric.Temperature;
        set
        {
            if (value)
                SelectedMetric = TelemetryGraphMetric.Temperature;
        }
    }

    public bool IsVoltageSelected
    {
        get => SelectedMetric == TelemetryGraphMetric.Voltage;
        set
        {
            if (value)
                SelectedMetric = TelemetryGraphMetric.Voltage;
        }
    }

    public bool IsCurrentSelected
    {
        get => SelectedMetric == TelemetryGraphMetric.Current;
        set
        {
            if (value)
                SelectedMetric = TelemetryGraphMetric.Current;
        }
    }


    private string _sensorSearchText = string.Empty;

    public ObservableCollection<DomainSensor> FilteredSensors { get; } = [];

    private readonly ManualStack<SensorCommand> _commandStack = new();

    private string _manualCommand = string.Empty;

    public string ManualCommand
    {
        get => _manualCommand;
        set => SetProperty(ref _manualCommand, value);
    }

    public ObservableCollection<SensorCommand> CommandHistory { get; } = [];

    public bool CanUndoCommand => _commandStack.Count > 0 &&
        !string.IsNullOrWhiteSpace(GetInverseCommand(_commandStack.Peek()!.Command));

    public ObservableCollection<DomainSensor> Sensors
    { get; } = [];

    public ObservableCollection<TelemetryHistoryItem> History
    { get; } = [];

    public Guid? SelectedSensorId
    {
        get => _selectedSensorId;

        set
        {
            if (!SetProperty(ref _selectedSensorId, value)) return;


            ApplyFilters();

            _ = LoadSelectedSensorCommandHistoryAsync();
        }
    }

    private async Task LoadSelectedSensorCommandHistoryAsync()
    {
        if (!SelectedSensorId.HasValue)
        {
            CommandHistory.Clear();
            _commandStack.Clear();

            OnPropertyChanged(nameof(CanUndoCommand));
            _undoLastCommandCommand.RaiseCanExecuteChanged();
            return;
        }

        try
        {
            await LoadCommandHistoryAsync(SelectedSensorId.Value);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

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

    public double AverageValue { get; private set; }

    public double MaximumValue { get; private set; }

    public double MinimumValue { get; private set; }

    public string StatisticsUnit =>
        SelectedMetric switch
        {
            TelemetryGraphMetric.Power => "W",
            TelemetryGraphMetric.Temperature => "°C",
            TelemetryGraphMetric.Voltage => "V",
            TelemetryGraphMetric.Current => "A",
            _ => string.Empty
        };

    public string AverageDisplay =>
    $"{AverageValue:F2} {StatisticsUnit}";

    public string MaximumDisplay =>
        $"{MaximumValue:F2} {StatisticsUnit}";

    public string MinimumDisplay =>
        $"{MinimumValue:F2} {StatisticsUnit}";

    public HistoryViewModel(
    ILocalTelemetryCache telemetryCache,
    ILocalSensorCache sensorCache,
    IConnectivityService connectivityService,
    ISensorCommandService sensorService,
    ILocalUserBehaviorCache localUserBehaviorCache,
    SmartXSession session)
    : base(connectivityService,
        session)
    {
        _telemetryCache = telemetryCache;
        _sensorService = sensorService;
        _sensorCache = sensorCache;
        _sensorCache = sensorCache;
        _userBehaviorCache = localUserBehaviorCache;
        _session = session;

        _loadCommand =
            new AsyncRelayCommand(
                () => LoadAsync());

        _clearFiltersCommand =
            new AsyncRelayCommand(
                () =>
                {
                    ClearFilters();
                    return Task.CompletedTask;
                });

        _sendManualCommandCommand =
            new AsyncRelayCommand(
                () => SendManualCommandAsync());

        _undoLastCommandCommand =
            new AsyncRelayCommand(
                () => UndoLastCommandAsync(),
                () => CanUndoCommand);
    }

    public async Task LoadAsync(
        CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        if (!Session.GatewayId.HasValue)
        {
            _loadedTelemetry.Clear();
            History.Clear();
            UpdateGraph();
            CalculateStatistics(_loadedTelemetry);
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            var gatewayId =
                Session.GatewayId.Value;

            var sensors =
                await _sensorCache.GetByGatewayIdAsync(
                    gatewayId,
                    cancellationToken);

            Sensors.Clear();


            foreach (var sensor in sensors)
            {
                Sensors.Add(sensor);
            }

            ApplySensorFilter();

            if (SelectedSensorId.HasValue &&
                Sensors.Any(x => x.Id == SelectedSensorId.Value))
            {
                await LoadCommandHistoryAsync(
                    SelectedSensorId.Value,
                    cancellationToken);
            }

            var telemetry =
                await _telemetryCache.GetByGatewayIdAsync(
                    gatewayId,
                    cancellationToken);

            _loadedTelemetry = telemetry.ToList();

            ApplyFilters();
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

    private void ApplyFilters()
    {
        IEnumerable<DomainTelemetry> query = _loadedTelemetry;


        if (_selectedSensorId.HasValue)
        {
            query = query.Where(x => x.SensorId ==
                         _selectedSensorId.Value);
        }

        if (_fromDate.HasValue)
        {
            query =
                query.Where(
                    x => x.Timestamp >=
                         _fromDate.Value);
        }

        if (_toDate.HasValue)
        {
            var end =
                _toDate.Value.Date.AddDays(1);

            query =
                query.Where(
                    x => x.Timestamp < end);
        }

        var telemetry = query.OrderByDescending(x => x.Timestamp)
            .ToArray();

        History.Clear();

        foreach (var item in telemetry)
        {
            var sensor =
                Sensors.FirstOrDefault(
                    x => x.Id == item.SensorId);

            History.Add(
                new TelemetryHistoryItem
                {
                    Id = item.Id,
                    SensorId = item.SensorId,

                    SensorName =
                        sensor?.Name ??
                        "Unknown Sensor",

                    Timestamp = item.Timestamp,

                    Voltage = item.Voltage,
                    Current = item.Current,
                    Power = item.Power,
                    Temperature = item.Temperature
                });
        }

        CalculateStatistics(telemetry);

        UpdateGraph();

    }

    public async Task SendManualCommandAsync(
     CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ManualCommand))
            return;

        if (!SelectedSensorId.HasValue)
        {
            ErrorMessage = "Please select a sensor first.";

            return;
        }


        try
        {
            var sensorId = SelectedSensorId.Value;

            ErrorMessage = string.Empty;

            var commandText = ManualCommand.Trim();

            var result =
                await _sensorService.SendCommandAsync(
                    sensorId,
                    commandText,
                    cancellationToken);

            await RecordUserBehaviorAsync(
                sensorId, commandText, result.Success,
                result.Success ? result.Response
                : result.ErrorMessage ?? "Command failed.", cancellationToken);

            if (!result.Success)
            {
                ErrorMessage =
                    result.ErrorMessage ??
                    "The command could not be sent.";

                await LoadCommandHistoryAsync(sensorId, cancellationToken);

                return;
            }

            var command = new SensorCommand
            {
                SensorId = SelectedSensorId.Value,
                Command = commandText,
                InverseCommand = result.InverseCommand ?? string.Empty,
                Response = result.Response
            };

            _commandStack.Push(command);

            CommandHistory.Insert(0, command);

            ManualCommand = string.Empty;

            OnPropertyChanged(nameof(CanUndoCommand));

            _undoLastCommandCommand.RaiseCanExecuteChanged();
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

    public async Task UndoLastCommandAsync(
    CancellationToken cancellationToken = default)
    {
        if (_commandStack.Count == 0) return;

        var command = _commandStack.Peek();

        var inverseCommand = GetInverseCommand(command.Command);

        if (string.IsNullOrWhiteSpace(inverseCommand))
        {
            ErrorMessage = "This command cannot be undone.";
            OnPropertyChanged(nameof(ErrorMessage));

            return;
        }

        if (string.IsNullOrWhiteSpace(command.InverseCommand))
        {
            ErrorMessage = "This command cannot be undone.";
            OnPropertyChanged(nameof(ErrorMessage));

            return;
        }

        try
        {
            ErrorMessage = string.Empty;
            OnPropertyChanged(nameof(ErrorMessage));

            var result = await _sensorService.SendCommandAsync(
                    command.SensorId,
                    inverseCommand,
                    cancellationToken);

            if (!result.Success)
            {
                ErrorMessage =
                    result.ErrorMessage ??
                    "The undo command could not be sent.";

                return;
            }

            await RecordUserBehaviorAsync(
                command.SensorId, command.InverseCommand, true, $"Undo executed for: {command.Command}", cancellationToken);

            _commandStack.Pop();
            CommandHistory.Remove(command);

            OnPropertyChanged(nameof(CanUndoCommand));
            _undoLastCommandCommand.RaiseCanExecuteChanged();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Undo failed: {ex.Message}";
            OnPropertyChanged(nameof(ErrorMessage));

        }
    }

    public string SensorSearchText
    {
        get => _sensorSearchText;
        set
        {
            if (!SetProperty(ref _sensorSearchText, value))
                return;

            ApplySensorFilter();
        }
    }

    private void ApplySensorFilter()
    {
        FilteredSensors.Clear();

        var search =
            SensorSearchText?.Trim();

        IEnumerable<DomainSensor> sensors =
            Sensors;

        if (!string.IsNullOrWhiteSpace(search))
        {
            sensors =
                sensors.Where(
                    x =>
                        x.Name.Contains(
                            search,
                            StringComparison.OrdinalIgnoreCase));
        }

        foreach (var sensor in sensors)
        {
            FilteredSensors.Add(sensor);
        }
    }

    private async Task LoadCommandHistoryAsync(
        Guid sensorId, CancellationToken cancellationToken = default)
    {
        var behaviors = await _userBehaviorCache.GetBySensorIdAsync(
            sensorId, cancellationToken);

        var ordered = behaviors.OrderByDescending(x => x.Timestamp).ToArray();

        CommandHistory.Clear();

        foreach (var behavior in ordered)
        {
            CommandHistory.Add(new SensorCommand
            {
                SensorId = behavior.SensorId,
                Command = behavior.Command,
                InverseCommand = string.Empty,
                Response = behavior.Outcome
            });
        }
    }

    private bool HasGraphValue(
    TelemetryHistoryItem item)
    {
        return GetGraphValue(item).HasValue;
    }
    private double? GetGraphValue(
    TelemetryHistoryItem item)
    {
        return SelectedMetric switch
        {
            TelemetryGraphMetric.Power =>
                item.Power,

            TelemetryGraphMetric.Temperature =>
                item.Temperature,

            TelemetryGraphMetric.Voltage =>
                item.Voltage,

            TelemetryGraphMetric.Current =>
                item.Current,

            _ => null
        };
    }

    private void UpdateGraph()
    {
        var points =
            History
                .Where(HasGraphValue)
                .OrderBy(x => x.Timestamp)
                .ToArray();

        var values =
            points
                .Select(
                    x =>
                        new DateTimePoint(
                            x.Timestamp.ToLocalTime(),
                            GetGraphValue(x)!.Value))
                .ToArray();

        GraphSeries =
        [
            new LineSeries<DateTimePoint>
        {
            Name = GraphMetricName,

            Values = values,

            Fill = null,

            LineSmoothness = 0.15,

            GeometrySize = 6,

            Stroke =
                new SolidColorPaint(
                    new SKColor(
                        37,
                        99,
                        235))
                {
                    StrokeThickness = 3
                },

            GeometryStroke =
                new SolidColorPaint(
                    new SKColor(
                        37,
                        99,
                        235))
                {
                    StrokeThickness = 2
                },

            GeometryFill =
                new SolidColorPaint(
                    SKColors.White),

            YToolTipLabelFormatter =
                point =>
                    $"{point.Coordinate.PrimaryValue:F2} {GraphUnit}"
        }
        ];

        GraphXAxes =
[
    new Axis
    {
        Name = "Time",

        Labeler =
            value =>
            {
                if (double.IsNaN(value) ||
                    double.IsInfinity(value))
                {
                    return string.Empty;
                }

                if (value < DateTime.MinValue.Ticks ||
                    value > DateTime.MaxValue.Ticks)
                {
                    return string.Empty;
                }

                var date =
                    new DateTime((long)value);

                return date.TimeOfDay ==
                       TimeSpan.Zero
                    ? date.ToString("dd MMM")
                    : date.ToString("HH:mm");
            },

        UnitWidth =
            TimeSpan.FromHours(1).Ticks,

        MinStep =
            TimeSpan.FromHours(1).Ticks,

        LabelsRotation = 0,

        SeparatorsPaint =
            new SolidColorPaint(
                new SKColor(
                    225,
                    229,
                    233))
            {
                StrokeThickness = 1
            }
    }
];

        GraphYAxes =
        [
            new Axis
        {
            Name =
                $"{GraphMetricName} ({GraphUnit})",

            SeparatorsPaint =
                new SolidColorPaint(
                    new SKColor(
                        225,
                        229,
                        233))
                {
                    StrokeThickness = 1
                }
        }
        ];

        OnPropertyChanged(nameof(GraphSeries));
        OnPropertyChanged(nameof(GraphXAxes));
        OnPropertyChanged(nameof(GraphYAxes));
        OnPropertyChanged(nameof(GraphMetricName));
        OnPropertyChanged(nameof(GraphUnit));
    }

    private async Task RecordUserBehaviorAsync(
    Guid sensorId,
    string command,
    bool succeeded,
    string? outcome,
    CancellationToken cancellationToken)
    {
        if (_session.UserId == Guid.Empty)
            return;

        var now = DateTime.UtcNow;

        var behavior = new UserBehaviorDto
        {
            UserId = _session.UserId,
            SensorId = sensorId,
            Command = command,
            Timestamp = now,
            Succeeded = succeeded,
            Outcome = outcome,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _userBehaviorCache.AddAsync(
            behavior,
            cancellationToken);
    }

    private void CalculateStatistics(
     IEnumerable<DomainTelemetry> telemetry)
    {
        var values =
            telemetry
                .Select(GetTelemetryValue)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .ToArray();

        if (values.Length == 0)
        {
            AverageValue = 0;
            MaximumValue = 0;
            MinimumValue = 0;
        }
        else
        {
            AverageValue = values.Average();
            MaximumValue = values.Max();
            MinimumValue = values.Min();
        }

        OnPropertyChanged(
            nameof(AverageValue));

        OnPropertyChanged(
            nameof(MaximumValue));

        OnPropertyChanged(
            nameof(MinimumValue));

        OnPropertyChanged(
            nameof(StatisticsUnit));

        OnPropertyChanged(
            nameof(AverageDisplay));

        OnPropertyChanged(
            nameof(MaximumDisplay));

        OnPropertyChanged(
            nameof(MinimumDisplay));
    }

    private static string? GetInverseCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return null;

        return command.Trim().ToUpperInvariant() switch
        {
            "ON" => "OFF",
            "OFF" => "ON",
            "START" => "STOP",
            "STOP" => "START",
            _ => null
        };
    }

    private double? GetTelemetryValue(
    DomainTelemetry telemetry)
    {
        return SelectedMetric switch
        {
            TelemetryGraphMetric.Power =>
                telemetry.Power,

            TelemetryGraphMetric.Temperature =>
                telemetry.Temperature,

            TelemetryGraphMetric.Voltage =>
                telemetry.Voltage,

            TelemetryGraphMetric.Current =>
                telemetry.Current,

            _ => null
        };
    }

    private void ClearFilters()
    {
        SensorSearchText = string.Empty;
        SelectedSensorId = null;
        FromDate = null;
        ToDate = null;
    }

    protected override void RaiseCommandStates()
    {
        _loadCommand.RaiseCanExecuteChanged();
        _clearFiltersCommand.RaiseCanExecuteChanged();
        _sendManualCommandCommand.RaiseCanExecuteChanged();
        _undoLastCommandCommand.RaiseCanExecuteChanged();
    }

}
