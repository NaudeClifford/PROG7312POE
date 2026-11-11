using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Win32;
using SkiaSharp;
using SmartX.Domain.Enums;
using SmartX.Shared.DTOs;
using SmartX.Shared.DTOs.Sensors;
using SmartX.Shared.Mapping;
using SmartX.WPF.Navigation;
using SmartX.WPF.Repositories.Local;
using SmartX.WPF.Services.Api;
using SmartX.WPF.Services.Connectivity;
using SmartX.WPF.Services.Session;
using SmartX.WPF.ViewModels.Base;
using SmartX.WPF.ViewModels.CommandStream;
using SmartX.WPF.Views.Pages.CommandStream;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using DomainTelemetry = SmartX.Domain.Entities.Telemetry;

namespace SmartX.WPF.ViewModels.History;

public sealed class HistoryViewModel : ViewModelBase
{
    private int _predictiveRequestVersion;
    private int _anomalyRequestVersion;
    private readonly ILocalTelemetryCache _telemetryCache;
    private readonly ILocalSensorCache _sensorCache;
    private readonly ILocalUserBehaviorCache _userBehaviorCache;
    private readonly SmartXSession _session;
    private readonly ISmartXApiClient _smartXClient;
    private readonly IMapper _mapper;
    private bool _isAnalyzing;
    private string _predictionMessage = "Select a sensor to analyse its telemetry.";
    private readonly INavigationService _navigationService;
    public ObservableCollection<PredictiveSuggestionDto> PredictiveSuggestions { get; } = [];

    public bool IsAnalyzing
    {
        get => _isAnalyzing;
        private set
        {
            if (!SetProperty(ref _isAnalyzing, value))
                return;

            _loadPredictiveSuggestionsCommand?
                .RaiseCanExecuteChanged();

            _analyzeAnomalyCommand?
                .RaiseCanExecuteChanged();

            _sendToCommandCentreCommand?
                .RaiseCanExecuteChanged();
        }
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
    private readonly AsyncRelayCommand _analyzeAnomalyCommand;
    private readonly RelayCommand _sendToCommandCentreCommand;
    private readonly AsyncRelayCommand _exportCsvCommand;
    private readonly AsyncRelayCommand _loadPredictiveSuggestionsCommand;


    private AnomalyRecommendationDto? _recommendation;

    public AnomalyRecommendationDto? Recommendation
    {
        get => _recommendation;
        private set
        {
            if (SetProperty(ref _recommendation, value))
            {
                OnPropertyChanged(nameof(HasRecommendation));
                OnPropertyChanged(nameof(CanExecuteRecommendation));
                OnPropertyChanged(nameof(CanSendToCommandCentre));

                _sendToCommandCentreCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool HasRecommendation => Recommendation is not null;

    public bool CanExecuteRecommendation =>
        Recommendation is not null &&
        !string.IsNullOrWhiteSpace(Recommendation.SuggestedCommand) &&
        SelectedSensorId == Recommendation.SensorId;

    public ICommand SendToCommandCentreCommand => _sendToCommandCentreCommand;

    public ICommand AnalyzeAnomalyCommand => _analyzeAnomalyCommand;

    public ICommand LoadCommand => _loadCommand;

    public ICommand ClearFiltersCommand => _clearFiltersCommand;
    public ICommand ExportCsvCommand => _exportCsvCommand;
    public ICommand LoadPredictiveSuggestionsCommand =>
    _loadPredictiveSuggestionsCommand;
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

    public ObservableCollection<SensorDto> FilteredSensors { get; } = [];

    public ObservableCollection<SensorDto> Sensors
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

            // Invalidate requests belonging to the previous sensor.
            _predictiveRequestVersion++;
            _anomalyRequestVersion++;

            Recommendation = null;
            PredictiveSuggestions.Clear();

            RecommendedCommand = string.Empty;
            RecommendationReason = string.Empty;
            RecommendationStatus = string.Empty;

            PredictionMessage = value.HasValue
                ? "Loading sensor analysis..."
                : "Select a sensor to analyse its telemetry.";

            AnomalyDescription = PredictionMessage;

            ApplyFilters();

            OnPropertyChanged(nameof(CanApplyRecommendation));
            OnPropertyChanged(nameof(CanSendToCommandCentre));

            _analyzeAnomalyCommand?.RaiseCanExecuteChanged();
            _loadPredictiveSuggestionsCommand?.RaiseCanExecuteChanged();
            _sendToCommandCentreCommand?.RaiseCanExecuteChanged();

            if (value.HasValue)
                _ = LoadPredictiveSuggestionsAsync();
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

    private string _recommendedCommand = string.Empty;

    public string RecommendedCommand
    {
        get => _recommendedCommand;
        set
        {
            if (!SetProperty(ref _recommendedCommand, value))
                return;

            OnPropertyChanged(nameof(CanApplyRecommendation));
            OnPropertyChanged(nameof(CanSendToCommandCentre));

            _sendToCommandCentreCommand?
                .RaiseCanExecuteChanged();
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

    public HistoryViewModel(
    ILocalTelemetryCache telemetryCache,
    ILocalSensorCache sensorCache,
    IConnectivityService connectivityService,
    ILocalUserBehaviorCache localUserBehaviorCache,
    ISmartXApiClient smartXClient,
    INavigationService navigationService,
    IMapper mapper,
    SmartXSession session)
    : base(connectivityService,
        session)
    {
        _telemetryCache = telemetryCache;
        _sensorCache = sensorCache;
        _userBehaviorCache = localUserBehaviorCache;
        _session = session;
        _smartXClient = smartXClient;
        _mapper = mapper;
        _navigationService = navigationService;

        _exportCsvCommand = new AsyncRelayCommand(
                () => ExportCsvAsync(),
                () => History.Count > 0);

        _analyzeAnomalyCommand = new AsyncRelayCommand(
            () => AnalyzeSelectedSensorAsync(),
            () => SelectedSensorId.HasValue &&
                  !IsBusy &&
                  !IsAnalyzing);

        _loadPredictiveSuggestionsCommand =
            new AsyncRelayCommand(
                () => LoadPredictiveSuggestionsAsync(),
                () => SelectedSensorId.HasValue && !IsAnalyzing);

        _loadCommand = new AsyncRelayCommand(() => LoadAsync());

        _clearFiltersCommand =
            new AsyncRelayCommand(
                () =>
                {
                    ClearFilters();
                    return Task.CompletedTask;
                });

        _sendToCommandCentreCommand = new RelayCommand(
     _ =>
     {
         if (!CanSendToCommandCentre)
             return;

         var request = new CommandNavigationRequest
         {
             SensorId = SelectedSensorId!.Value,
             Command = RecommendedCommand,
             Source = "History Analysis"
         };

         _navigationService.NavigateTo<RealTimeCommandStreamPage>(request);
     },
     _ => CanSendToCommandCentre);
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

            var sensorDtos = _mapper.Map<List<SensorDto>>(sensors);

            Sensors.Clear();

            foreach (var sensor in sensorDtos)
            {
                Sensors.Add(sensor);
            }

            ApplySensorFilter();



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
        _exportCsvCommand.RaiseCanExecuteChanged();
        UpdateGraph();

    }

    private async Task LoadPredictiveSuggestionsAsync(
    CancellationToken cancellationToken = default)
    {
        var requestVersion = ++_predictiveRequestVersion;
        var sensorId = SelectedSensorId;

        PredictiveSuggestions.Clear();

        if (!sensorId.HasValue)
        {
            PredictionMessage =
                "Select a sensor to analyse its telemetry.";

            AnomalyDescription = PredictionMessage;
            return;
        }

        try
        {
            IsAnalyzing = true;
            PredictionMessage = "Analysing sensor telemetry...";

            var suggestions =
                await _smartXClient.GetPredictiveSuggestionsAsync(
                    sensorId.Value,
                    cancellationToken);

            if (requestVersion != _predictiveRequestVersion ||
                    SelectedSensorId != sensorId)
            {
                return;
            }

            foreach (var suggestion in suggestions)
            {
                PredictiveSuggestions.Add(suggestion);
            }

            var selected = suggestions
                .OrderByDescending(x => x.Score)
                .FirstOrDefault();

            if (selected is null)
            {
                PredictionMessage =
                    "No anomalies detected for this sensor.";

                AnomalyDescription =
                    "No active or predictive anomaly was detected.";

                RecommendationReason =
                    "No corrective action is currently recommended.";

                RecommendationStatus =
                    "No recommendation is currently available.";

                RecommendedCommand = string.Empty;

                OnPropertyChanged(nameof(CanSendToCommandCentre));
                _sendToCommandCentreCommand.RaiseCanExecuteChanged();

                return;
            }

            RecommendedCommand =
                selected.RecommendedCommand ?? string.Empty;

            AnomalyDescription = selected.Description;

            RecommendationReason =
                $"Severity: {selected.Severity} | " +
                $"Risk score: {selected.Score}/100";

            RecommendationStatus =
                string.IsNullOrWhiteSpace(RecommendedCommand)
                    ? "No executable command is available."
                    : "Command available for review.";

            PredictionMessage =
                $"{suggestions.Count} recommendation(s) available.";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            PredictionMessage =
                $"Analysis failed: {ex.Message}";
        }
        finally
        {
            if (requestVersion == _predictiveRequestVersion)
                IsAnalyzing = false;
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

        var search = SensorSearchText?.Trim();

        IEnumerable<SensorDto> sensors = Sensors;

        if (!string.IsNullOrWhiteSpace(search))
        {
            sensors = sensors.Where(
                x => x.Name.Contains(
                    search,
                    StringComparison.OrdinalIgnoreCase));
        }

        foreach (var sensor in sensors)
        {
            FilteredSensors.Add(sensor);
        }
    }

    public bool CanSendToCommandCentre =>
        SelectedSensorId.HasValue &&
        !string.IsNullOrWhiteSpace(RecommendedCommand) &&
        !IsBusy &&
        !IsAnalyzing;

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


    private async Task ExportCsvAsync()
    {
        if (History.Count == 0)
        {
            MessageBox.Show(
                "There is no telemetry history to export.",
                "Export CSV",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Export Sensor History",
            Filter = "CSV Files (*.csv)|*.csv",
            DefaultExt = ".csv",
            AddExtension = true,
            FileName = $"SensorHistory_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            var csv = new StringBuilder();

            // CSV headers
            csv.AppendLine(
                "Telemetry ID,Sensor ID,Sensor Name,Timestamp,Voltage (V),Current (A),Power (W),Temperature (°C)");

            foreach (var item in History)
            {
                var values = new[]
{
    EscapeCsv(item.Id.ToString()),
    EscapeCsv(item.SensorId.ToString()),
    EscapeCsv(item.SensorName),
    EscapeCsv(item.Timestamp.ToString(
        "yyyy-MM-dd HH:mm:ss",
        CultureInfo.InvariantCulture)),

    EscapeCsv(FormatCsvValue(item.Voltage)),
    EscapeCsv(FormatCsvValue(item.Current)),
    EscapeCsv(FormatCsvValue(item.Power)),
    EscapeCsv(FormatCsvValue(item.Temperature))
};

                csv.AppendLine(string.Join(",", values));
            }

            await File.WriteAllTextAsync(
                dialog.FileName,
                csv.ToString(),
                new UTF8Encoding(true));

            MessageBox.Show(
                $"Successfully exported {History.Count} telemetry records.",
                "Export Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to export telemetry history:\n{ex.Message}",
                "Export Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
    private static string FormatCsvValue<T>(T? value)
    where T : struct, IFormattable
    {
        return value.HasValue
            ? value.Value.ToString(null, CultureInfo.InvariantCulture)
            : string.Empty;
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "\"\"";

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }


    private async Task AnalyzeSelectedSensorAsync(
        CancellationToken cancellationToken = default)
    {
        if (!SelectedSensorId.HasValue)
        {
            ErrorMessage = "Select a sensor first.";
            return;
        }

        var sensorId = SelectedSensorId.Value;
        var requestVersion = ++_anomalyRequestVersion;
        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            PredictionMessage = "Analysing sensor telemetry...";

            var result =
                await _smartXClient.AnalyzeSensorAnomalyAsync(
                    sensorId,
                    cancellationToken);

            // Ignore results belonging to an old sensor/request.
            if (requestVersion != _predictiveRequestVersion ||
                SelectedSensorId != sensorId)
            {
                return;
            }

            if (result is null)
            {
                PredictionMessage = "No anomaly detected.";

                AnomalyDescription =
                    "No anomaly detected or insufficient telemetry.";

                RecommendationReason =
                    "No new recommendation was generated.";

                RecommendationStatus =
                    "No recommendation available.";

                // Preserve the previous recommendation.
                OnPropertyChanged(nameof(CanSendToCommandCentre));
                _sendToCommandCentreCommand.RaiseCanExecuteChanged();

                return;
            }

            // Update the recommendation only after successful analysis.
            Recommendation = result;

            AnomalyDescription =
                $"{result.Metric}: {result.Observed:F2} " +
                $"{result.Unit} (Expected: {result.Expected:F2} " +
                $"{result.Unit})";

            RecommendationReason =
                $"{result.Explanation}\n" +
                $"Severity: {result.Severity}\n" +
                $"Confidence: {result.Confidence:P0}";

            RecommendedCommand =
                result.SuggestedCommand ?? string.Empty;

            RecommendationStatus =
                string.IsNullOrWhiteSpace(RecommendedCommand)
                    ? "Recommendation available, but no command was generated."
                    : "Command available for review.";

            PredictionMessage =
                $"Analysis completed. Severity: {result.Severity}";

            OnPropertyChanged(nameof(CanSendToCommandCentre));
            _sendToCommandCentreCommand.RaiseCanExecuteChanged();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (requestVersion == _predictiveRequestVersion)
            {
                ErrorMessage =
                    $"Anomaly analysis failed: {ex.Message}";
            }
        }
        finally
        {
            if (requestVersion == _predictiveRequestVersion)
            {
                IsBusy = false;
            }
        }
    }

    private double? GetTelemetryValue(DomainTelemetry telemetry)
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
        _analyzeAnomalyCommand.RaiseCanExecuteChanged();
        _loadPredictiveSuggestionsCommand.RaiseCanExecuteChanged();
        _exportCsvCommand.RaiseCanExecuteChanged();
        _sendToCommandCentreCommand.RaiseCanExecuteChanged();
    }


}
