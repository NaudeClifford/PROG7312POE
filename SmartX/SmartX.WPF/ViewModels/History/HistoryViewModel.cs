using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Win32;
using SkiaSharp;
using SmartX.Application.Services.Sensors;
using SmartX.Domain.Entities;
using SmartX.Domain.Enums;
using SmartX.Shared.DTOs;
using SmartX.WPF.Models.History;
using SmartX.WPF.Repositories.Local;
using SmartX.WPF.Services.Connectivity;
using SmartX.WPF.Services.PredictiveEngine;
using SmartX.WPF.Services.Session;
using SmartX.WPF.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
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
    private readonly IAnomalyRecommendationService _anomalyService;
    private readonly PredictiveSuggestionService _predictiveSuggestionService;

    private bool _isAnalyzing;
    private string _predictionMessage = "Select a sensor to analyse its telemetry.";

    public ObservableCollection<PredictiveSuggestion> PredictiveSuggestions { get; } = [];

    public bool IsAnalyzing
    {
        get => _isAnalyzing;
        private set => SetProperty(ref _isAnalyzing, value);
    }

    public string PredictionMessage
    {
        get => _predictionMessage;
        private set => SetProperty(ref _predictionMessage, value);
    }

    private List<DomainTelemetry> _loadedTelemetry = [];

    private Guid? _selectedSensorId;

    private DateTime? _fromDate;

    private DateTime? _toDate;
    private readonly AsyncRelayCommand _loadCommand;
    private readonly AsyncRelayCommand _clearFiltersCommand;
    private readonly AsyncRelayCommand _sendManualCommandCommand;
    private readonly AsyncRelayCommand _undoLastCommandCommand;
    private readonly AsyncRelayCommand _exportCommand;
    private readonly AsyncRelayCommand _analyzeAnomalyCommand;
    private readonly AsyncRelayCommand _executeRecommendationCommand;

    private AnomalyRecommendation? _recommendation;

    public AnomalyRecommendation? Recommendation
    {
        get => _recommendation;
        private set
        {
            if (SetProperty(ref _recommendation, value))
            {
                OnPropertyChanged(nameof(HasRecommendation));
                OnPropertyChanged(nameof(CanExecuteRecommendation));
                _executeRecommendationCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool HasRecommendation => Recommendation is not null;

    public bool CanExecuteRecommendation =>
        Recommendation is not null &&
        !string.IsNullOrWhiteSpace(Recommendation.SuggestedCommand) &&
        SelectedSensorId == Recommendation.SensorId;

    public ICommand AnalyzeAnomalyCommand => _analyzeAnomalyCommand;

    public ICommand ExecuteRecommendationCommand =>
        _executeRecommendationCommand;

    public ICommand ExportCommand => _exportCommand;
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
    public bool CanApplyRecommendation => SelectedSensorId.HasValue &&
        !string.IsNullOrWhiteSpace(RecommendedCommand) && !IsBusy;

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



    public bool CanUndoCommand
    {
        get
        {
            if (!SelectedSensorId.HasValue ||
                !_commandStack.TryPeek(out var command) ||
                command is null)
            {
                return false;
            }

            return command.SensorId == SelectedSensorId.Value
                && !string.IsNullOrWhiteSpace(command.InverseCommand);
        }
    }


    public ObservableCollection<DomainSensor> Sensors
    { get; } = [];

    public ObservableCollection<TelemetryHistoryItem> History
    { get; } = [];


    public Guid? SelectedSensorId
    {
        get => _selectedSensorId;
        set
        {
            if (!SetProperty(ref _selectedSensorId, value))
                return;

            ApplyFilters();

            _commandStack.Clear();
            CommandHistory.Clear();
            ManualCommand = "";
            RefreshUndoState();

            _ = LoadSelectedSensorCommandHistoryAsync();
            _ = LoadPredictiveSuggestionsAsync();

            OnPropertyChanged(nameof(CanApplyRecommendation));
            _applyRecommendationCommand.RaiseCanExecuteChanged();
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

    private string _anomalyDescription = "Select a sensor to analyse its telemetry.";

    public string AnomalyDescription
    {
        get => _anomalyDescription;
        set => SetProperty(ref _anomalyDescription, value);
    }

    private string _recommendedAction = "No recommendation available yet.";

    public string RecommendedAction
    {
        get => _recommendedAction;
        set => SetProperty(ref _recommendedAction, value);
    }

    private string _recommendedCommand = string.Empty;

    public string RecommendedCommand
    {
        get => _recommendedCommand;
        set
        {
            if (!SetProperty(ref _recommendedCommand, value))
                return;

            OnPropertyChanged(nameof(CanApplyRecommendation));
            _applyRecommendationCommand.RaiseCanExecuteChanged();
        }
    }

    private string _recommendationReason = string.Empty;

    public string RecommendationReason
    {
        get => _recommendationReason;
        set => SetProperty(ref _recommendationReason, value);
    }

    private string _recommendationStatus = string.Empty;

    public string RecommendationStatus
    {
        get => _recommendationStatus;
        set => SetProperty(ref _recommendationStatus, value);
    }

    private readonly AsyncRelayCommand _applyRecommendationCommand;
    public ICommand ClearCommandHistoryCommand { get; }


    public ICommand ApplyRecommendationCommand =>
        _applyRecommendationCommand;
    public HistoryViewModel(
    ILocalTelemetryCache telemetryCache,
    ILocalSensorCache sensorCache,
    IConnectivityService connectivityService,
    ISensorCommandService sensorService,
    ILocalUserBehaviorCache localUserBehaviorCache,
    IAnomalyRecommendationService anomalyRecommendationService,
    PredictiveSuggestionService predictiveSuggestionService,
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
        _anomalyService = anomalyRecommendationService;
        _predictiveSuggestionService = predictiveSuggestionService;

        _exportCommand = new AsyncRelayCommand(() => ExportHistoryAsync());

        ClearCommandHistoryCommand = new RelayCommand(_ => ClearCommandHistory());

        _analyzeAnomalyCommand = new AsyncRelayCommand(
            () => AnalyzeSelectedSensorAsync(),
            () => SelectedSensorId.HasValue && !IsBusy);

        _executeRecommendationCommand = new AsyncRelayCommand(
            () => ExecuteRecommendationAsync(),
            () => CanExecuteRecommendation && !IsBusy);

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

        _applyRecommendationCommand = new AsyncRelayCommand(
            () => ApplyRecommendationAsync(),
            () => CanApplyRecommendation);
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
            var inverseCommand = result.InverseCommand ?? GetInverseCommand(commandText);

            var command = new SensorCommand
            {
                SensorId = sensorId,
                Command = commandText,
                InverseCommand = result.InverseCommand
                    ?? GetInverseCommand(commandText)
                    ?? string.Empty,
                Response = result.Response,
                HistoryStatus = "IN STACK"
            };

            CommandHistory.Insert(0, command);

            if (!string.IsNullOrWhiteSpace(command.InverseCommand))
            {
                _commandStack.Push(command);
            }

            ManualCommand = string.Empty;
            ErrorMessage = string.Empty;

            RefreshUndoState();

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
    private async Task LoadPredictiveSuggestionsAsync(CancellationToken cancellationToken = default)
    {
        var sensorId = SelectedSensorId;

        PredictiveSuggestions.Clear();

        AnomalyDescription = string.Empty;
        RecommendedAction = string.Empty;
        RecommendedCommand = string.Empty;
        RecommendationReason = string.Empty;
        RecommendationStatus = string.Empty;

        if (!sensorId.HasValue)
        {
            PredictionMessage = "Select a sensor to analyse its telemetry.";
            AnomalyDescription = PredictionMessage;
            return;
        }

        try
        {
            IsAnalyzing = true;
            PredictionMessage = "Analysing sensor telemetry...";

            var suggestions = await _predictiveSuggestionService.GetWarningsAsync(cancellationToken);

            // The user may have selected a different sensor while
            // the previous analysis was still running.
            if (SelectedSensorId != sensorId)
                return;

            var filteredSuggestions = suggestions
                .Where(x => x.SensorId == sensorId.Value)
                .OrderByDescending(x => x.Score)
                .ToList();

            foreach (var suggestion in filteredSuggestions)
                PredictiveSuggestions.Add(suggestion);

            var selectedSuggestion = filteredSuggestions.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.RecommendedCommand)) ?? filteredSuggestions.FirstOrDefault();

            if (selectedSuggestion is null)
            {
                PredictionMessage =
                    "No anomalies detected for this sensor.";

                AnomalyDescription =
                    "No active or predictive anomaly was detected.";

                RecommendedAction =
                    "Continue monitoring the sensor.";

                RecommendedCommand = string.Empty;

                RecommendationReason =
                    "No command is recommended because the engine found no warning.";

                RecommendationStatus =
                    "No recommendation is currently available.";

                return;
            }


            RecommendedCommand =
                selectedSuggestion.RecommendedCommand ?? string.Empty;

            RecommendationStatus =
                string.IsNullOrWhiteSpace(RecommendedCommand)
                    ? "No executable command is available. Review the suggested action."
                    : "Command available for review.";

            // Populate the properties bound to the XAML panel.
            AnomalyDescription = selectedSuggestion.Description
                ?? "Anomaly detected.";

            RecommendedAction = selectedSuggestion.RecommendedAction
                ?? "Review the sensor before taking action.";

            RecommendationReason =
                $"Severity: {selectedSuggestion.Severity} | " +
                $"Risk score: {selectedSuggestion.Score}/100";


            PredictionMessage =
                $"{filteredSuggestions.Count} recommendation(s) available.";

            OnPropertyChanged(nameof(RecommendedCommand));
            OnPropertyChanged(nameof(CanApplyRecommendation));
            _applyRecommendationCommand.RaiseCanExecuteChanged();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            PredictionMessage = $"Analysis failed: {ex.Message}";
            RecommendationStatus = ex.Message;
        }
        finally
        {
            IsAnalyzing = false;
        }
    }

    private void ClearCommandHistory()
    {
        _commandStack.Clear();
        CommandHistory.Clear();

        ErrorMessage = string.Empty;

        RefreshUndoState();
    }

    private void RefreshUndoState()
    {
        OnPropertyChanged(nameof(CanUndoCommand));
        _undoLastCommandCommand.RaiseCanExecuteChanged();
    }

    private async Task UndoLastCommandAsync(
        CancellationToken cancellationToken = default)
    {
        if (!SelectedSensorId.HasValue ||
            !_commandStack.TryPeek(out var command) ||
            command is null ||
            command.SensorId != SelectedSensorId.Value ||
            string.IsNullOrWhiteSpace(command.InverseCommand))
        {
            ErrorMessage =
                "No reversible command is available for the selected sensor.";
            RefreshUndoState();
            return;
        }

        var sensorId = command.SensorId;
        var originalCommand = command.Command;
        var inverseCommand = command.InverseCommand.Trim();

        try
        {
            ErrorMessage = string.Empty;

            // Send the inverse through the same service as normal commands.
            var result = await _sensorService.SendCommandAsync(
                sensorId,
                inverseCommand,
                cancellationToken);

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage
                    ?? $"Undo command '{inverseCommand}' failed.";

                // Preserve the original command so the user can retry.
                return;
            }

            await RecordUserBehaviorAsync(
                sensorId,
                inverseCommand,
                true,
                $"Reversal of '{originalCommand}' executed. " +
                $"Service response: {result.Response}",
                cancellationToken);

            // Remove the original only after successful execution.
            _commandStack.Pop();
            CommandHistory.Remove(command);

            // Show the reversal explicitly in the history.
            CommandHistory.Insert(0, new SensorCommand
            {
                SensorId = sensorId,
                Command = inverseCommand,
                InverseCommand = originalCommand,
                Response = result.Response ?? string.Empty,
                Timestamp = DateTime.UtcNow,
                HistoryStatus = "UNDONE"
            });
            System.Diagnostics.Debug.WriteLine($"UNDO: original={command.Command}, " +
                $"inverse={inverseCommand}, " + $"success={result.Success}, " + $"response={result.Response}, " + $"error={result.ErrorMessage}");

            RefreshUndoState();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Undo failed: {ex.Message}";
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
    Guid sensorId,
    CancellationToken cancellationToken = default)
    {
        var behaviors = await _userBehaviorCache.GetBySensorIdAsync(
            sensorId,
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        var ordered = behaviors
            .Where(x => x.SensorId == sensorId)
            .OrderByDescending(x => x.Timestamp)
            .ToList();

        CommandHistory.Clear();
        _commandStack.Clear();

        foreach (var behavior in ordered)
        {
            var inverse = behavior.Succeeded
                ? GetInverseCommand(behavior.Command)
                : null;

            CommandHistory.Add(new SensorCommand
            {
                SensorId = behavior.SensorId,
                Command = behavior.Command,
                InverseCommand = inverse,
                Response = behavior.Outcome ?? string.Empty,
                Timestamp = behavior.Timestamp,
                HistoryStatus = "CACHED"
            });
        }

        // Push oldest first so the newest undoable command is on top.
        foreach (var behavior in ordered
            .Where(x => x.Succeeded)
            .OrderBy(x => x.Timestamp))
        {
            var inverse = GetInverseCommand(behavior.Command);

            if (string.IsNullOrWhiteSpace(inverse))
                continue;

            _commandStack.Push(new SensorCommand
            {
                SensorId = behavior.SensorId,
                Command = behavior.Command,
                InverseCommand = inverse,
                Response = behavior.Outcome ?? string.Empty,
                Timestamp = behavior.Timestamp
            });
        }

        RefreshUndoState();
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
        return command.Trim().ToUpperInvariant() switch
        {
            "ON" => "OFF",
            "OFF" => "ON",

            "POWER ON" => "POWER OFF",
            "POWER OFF" => "POWER ON",

            "START" => "STOP",
            "STOP" => "START",

            "ENABLE" => "DISABLE",
            "DISABLE" => "ENABLE",

            "OPEN" => "CLOSE",
            "CLOSE" => "OPEN",

            "LOCK" => "UNLOCK",
            "UNLOCK" => "LOCK",

            "ACTIVATE" => "DEACTIVATE",
            "DEACTIVATE" => "ACTIVATE",

            _ => null
        };
    }

    private async Task AnalyzeSelectedSensorAsync(
    CancellationToken cancellationToken = default)
    {
        if (!SelectedSensorId.HasValue)
        {
            ErrorMessage = "Select a sensor before analyzing anomalies.";
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;
            Recommendation = null;

            var sensorId = SelectedSensorId.Value;

            Recommendation = await _anomalyService.AnalyzeAsync(
                sensorId,
                _loadedTelemetry,
                cancellationToken);

            if (Recommendation is null)
            {
                ErrorMessage =
                    "No anomaly was detected, or there is insufficient " +
                    "telemetry to establish a reliable baseline.";
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Anomaly analysis failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            _analyzeAnomalyCommand.RaiseCanExecuteChanged();
            _executeRecommendationCommand.RaiseCanExecuteChanged();
        }
    }

    private async Task ExecuteRecommendationAsync(
    CancellationToken cancellationToken = default)
    {
        var recommendation = Recommendation;

        if (recommendation is null ||
            !SelectedSensorId.HasValue ||
            SelectedSensorId.Value != recommendation.SensorId)
        {
            ErrorMessage = "Select the sensor associated with this recommendation.";
            return;
        }

        var command = recommendation.SuggestedCommand?.Trim();

        if (string.IsNullOrWhiteSpace(command))
        {
            ErrorMessage =
                "No approved command is available for this recommendation.";
            return;
        }

        var answer = MessageBox.Show(
            $"Anomaly: {recommendation.Explanation}\n\n" +
            $"Recommended action: {recommendation.RecommendedAction}\n\n" +
            $"Command to send: {command}\n\n" +
            "Only proceed if this command is safe for the connected equipment.\n\n" +
            "Send this command?",
            "Confirm corrective action",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (answer != MessageBoxResult.Yes)
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            var result = await _sensorService.SendCommandAsync(
                recommendation.SensorId,
                command,
                cancellationToken);

            await RecordUserBehaviorAsync(
                recommendation.SensorId,
                command,
                result.Success,
                result.Success
                    ? result.Response
                    : result.ErrorMessage ?? "Command failed.",
                cancellationToken);

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ??
                    "The corrective command failed.";
                return;
            }

            // Refresh the displayed telemetry and graph.
            await LoadAsync(cancellationToken);

            // Analyze again after refreshing telemetry.
            await AnalyzeSelectedSensorAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Corrective action failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            _executeRecommendationCommand.RaiseCanExecuteChanged();
            _analyzeAnomalyCommand.RaiseCanExecuteChanged();
        }
    }
    private Task ExportHistoryAsync()
    {
        if (History.Count == 0)
        {
            ErrorMessage = "There is no history to export.";
            OnPropertyChanged(nameof(ErrorMessage));
            return Task.CompletedTask;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Export Telemetry History",
            FileName = $"SmartX_History_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
            DefaultExt = ".csv",
            Filter = "CSV files (*.csv)|*.csv",
            AddExtension = true,
            OverwritePrompt = true
        };

        if (dialog.ShowDialog() != true)
        {
            return Task.CompletedTask;
        }

        try
        {
            var csv = new StringBuilder();

            csv.AppendLine(
                "Timestamp,Sensor Name,Voltage (V),Current (A),Power (W),Temperature (C)");

            foreach (var item in History)
            {
                csv.AppendLine(string.Join(",",
                    CsvEscape(item.Timestamp.ToString("O", CultureInfo.InvariantCulture)),
                    CsvEscape(item.SensorName),
                    CsvEscape(Convert.ToString(item.Voltage, CultureInfo.InvariantCulture)),
                    CsvEscape(Convert.ToString(item.Current, CultureInfo.InvariantCulture)),
                    CsvEscape(Convert.ToString(item.Power, CultureInfo.InvariantCulture)),
                    CsvEscape(Convert.ToString(item.Temperature, CultureInfo.InvariantCulture))
                ));
            }

            File.WriteAllText(
                dialog.FileName,
                csv.ToString(),
                new UTF8Encoding(true));

            ErrorMessage = string.Empty;
            OnPropertyChanged(nameof(ErrorMessage));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Export failed: {ex.Message}";
            OnPropertyChanged(nameof(ErrorMessage));
        }

        return Task.CompletedTask;
    }

    private static string CsvEscape(string? value)
    {
        value ??= string.Empty;

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private Task ApplyRecommendationAsync()
    {
        if (!SelectedSensorId.HasValue)
        {
            RecommendationStatus = "Select a sensor first.";
            return Task.CompletedTask;
        }

        var command = RecommendedCommand?.Trim();

        if (string.IsNullOrWhiteSpace(command))
        {
            RecommendationStatus =
                "No executable recommendation is available.";
            return Task.CompletedTask;
        }

        // Load the command into the manual command input.
        ManualCommand = command;

        RecommendationStatus =
            $"Command '{command}' loaded into the manual command panel. " +
            "Review it before sending.";

        return Task.CompletedTask;
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
        _exportCommand.RaiseCanExecuteChanged();
        _analyzeAnomalyCommand.RaiseCanExecuteChanged();
        _executeRecommendationCommand.RaiseCanExecuteChanged();
        _applyRecommendationCommand.RaiseCanExecuteChanged();
    }


}
