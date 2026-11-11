using SmartX.Application.Services.Sensors.Command;
using SmartX.Shared.DTOs;
using SmartX.Shared.DTOs.Sensors;
using SmartX.Shared.Mapping;
using SmartX.WPF.Repositories.Local;
using SmartX.WPF.Services.Api;
using SmartX.WPF.Services.Connectivity;
using SmartX.WPF.Services.Session;
using SmartX.WPF.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SmartX.WPF.ViewModels.CommandStream;

public class RealTimeCommandViewModel : ViewModelBase
{
    private readonly ISmartXApiClient _apiClient;
    private readonly ILocalSensorCache _sensorCache;
    private readonly ILocalUserBehaviorCache _userBehaviorCache;
    private readonly SmartXSession _session;
    private readonly IMapper _mapper;

    private readonly AsyncRelayCommand _loadCommand;
    private readonly AsyncRelayCommand _sendCommand;
    private readonly AsyncRelayCommand _undoCommand;
    private readonly AsyncRelayCommand _analyzeAnomalyCommand;

    private readonly AsyncRelayCommand _sendToCommandCentreCommand;

    private Guid? _selectedSensorId;
    private string _manualCommand = string.Empty;
    private string _commandResponse = "Ready to receive commands.";
    private bool _canUndo;

    // PREDICTIVE SUGGESTIONS

    public ObservableCollection<PredictiveSuggestionDto> PredictiveSuggestions { get; } = [];

    private PredictiveSuggestionDto? _recommendation;

    public PredictiveSuggestionDto? Recommendation
    {
        get => _recommendation;
        private set
        {
            if (!SetProperty(ref _recommendation, value))
                return;

            OnPropertyChanged(nameof(PredictionMessage));
            OnPropertyChanged(nameof(CanSendToCommandCentre));

            RefreshCommandStates();
        }
    }

    private string _predictionMessage =
        "Select a sensor to view predictive analysis.";

    public string PredictionMessage
    {
        get => _predictionMessage;
        private set => SetProperty(ref _predictionMessage, value);
    }

    private string _selectedRecommendedCommand = string.Empty;

    public string SelectedRecommendedCommand
    {
        get => _selectedRecommendedCommand;
        set
        {
            if (!SetProperty(
                ref _selectedRecommendedCommand,
                value))
                return;

            RefreshCommandStates();
        }
    }

    public ObservableCollection<SensorDto> Sensors { get; } = [];

    public ObservableCollection<SensorCommand> CommandHistory { get; } = [];

    public Guid? SelectedSensorId
    {
        get => _selectedSensorId;
        set
        {
            if (!SetProperty(ref _selectedSensorId, value))
                return;

            Recommendation = null;

            PredictionMessage =
                "Loading predictive analysis for selected sensor.";

            _canUndo = false;
            OnPropertyChanged(nameof(CanUndoCommand));

            _ = LoadCommandHistoryAsync();
            _ = LoadPredictiveSuggestionsAsync();

            RefreshCommandStates();
        }
    }

    public string ManualCommand
    {
        get => _manualCommand;
        set
        {
            if (!SetProperty(ref _manualCommand, value))
                return;

            RefreshCommandStates();
        }
    }

    public string CommandResponse
    {
        get => _commandResponse;
        private set => SetProperty(ref _commandResponse, value);
    }

    public bool CanUndoCommand => SelectedSensorId.HasValue && _canUndo && !IsBusy;

    public bool CanSendToCommandCentre => SelectedSensorId.HasValue && Recommendation is not null &&
        !string.IsNullOrWhiteSpace(Recommendation.RecommendedCommand) && !IsBusy;

    public ICommand LoadCommand => _loadCommand;

    public ICommand SendCommand => _sendCommand;

    public ICommand UndoCommand => _undoCommand;

    public ICommand AnalyzeAnomalyCommand => _analyzeAnomalyCommand;

    public ICommand SendToCommandCentreCommand => _sendToCommandCentreCommand;

    public RealTimeCommandViewModel(

        ILocalSensorCache sensorCache,
        ILocalUserBehaviorCache userBehaviorCache,
        ISmartXApiClient smartXApi,
        IConnectivityService connectivityService,
        SmartXSession session,
        IMapper mapper)
        : base(connectivityService, session)
    {
        _sensorCache = sensorCache;
        _userBehaviorCache = userBehaviorCache;
        _session = session;
        _apiClient = smartXApi;
        _mapper = mapper;

        _analyzeAnomalyCommand = new AsyncRelayCommand(
            () => AnalyzeAnomalyAsync(),
            () => SelectedSensorId.HasValue && !IsBusy);

        _sendToCommandCentreCommand = new AsyncRelayCommand(
            () => SendToCommandCentreAsync(),
            () => CanSendToCommandCentre);

        _loadCommand = new AsyncRelayCommand(
            () => LoadAsync(),
            () => !IsBusy);

        _sendCommand = new AsyncRelayCommand(
            () => SendManualCommandAsync(),
            () => CanSendCommand);

        _undoCommand = new AsyncRelayCommand(
            () => UndoLastCommandAsync(),
            () => CanUndoCommand);
    }

    public bool CanSendCommand =>
        SelectedSensorId.HasValue &&
        !string.IsNullOrWhiteSpace(ManualCommand) &&
        !IsBusy;

    public async Task LoadAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_session.GatewayId.HasValue)
        {
            ErrorMessage = "No gateway is currently selected.";
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            var sensors = await _sensorCache.GetByGatewayIdAsync(
                _session.GatewayId.Value,
                cancellationToken);

            var sensorDtos = _mapper.Map<List<SensorDto>>(sensors);

            Sensors.Clear();

            foreach (var sensor in sensorDtos)
                Sensors.Add(sensor);

            if (SelectedSensorId.HasValue &&
                Sensors.Any(x => x.Id == SelectedSensorId.Value))
            {
                await LoadCommandHistoryAsync(cancellationToken);
            }

            CommandResponse = "Command centre ready.";
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
            RefreshCommandStates();
        }
    }

    private async Task SendManualCommandAsync(
        CancellationToken cancellationToken = default)
    {
        if (!SelectedSensorId.HasValue ||
            string.IsNullOrWhiteSpace(ManualCommand))
        {
            return;
        }

        var sensorId = SelectedSensorId.Value;
        var commandText = ManualCommand.Trim();

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            CommandResponse = $"Sending command: {commandText}...";

            var result = await _apiClient.SendSensorCommandAsync(
                sensorId,
                commandText,
                cancellationToken);

            await RecordUserBehaviorAsync(
                sensorId,
                commandText,
                result.Success,
                result.Success
                    ? result.Response
                    : result.ErrorMessage ?? "Command failed.",
                cancellationToken);

            if (!result.Success)
            {
                CommandResponse = result.ErrorMessage ??
                    "Command execution failed.";

                ErrorMessage = CommandResponse;

                await LoadCommandHistoryAsync(cancellationToken);
                return;
            }

            var command = new SensorCommand
            {
                SensorId = sensorId,
                Command = commandText,
                InverseCommand = result.InverseCommand ?? string.Empty,
                Response = result.Response ?? string.Empty,
                Timestamp = DateTime.UtcNow,
                HistoryStatus = "EXECUTED"
            };

            CommandHistory.Insert(0, command);

            _canUndo = !string.IsNullOrWhiteSpace(
                result.InverseCommand);

            CommandResponse = result.Response ??
                "Command executed successfully.";

            ManualCommand = string.Empty;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            CommandResponse = $"Command error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            RefreshCommandStates();
        }
    }

    private async Task UndoLastCommandAsync(
        CancellationToken cancellationToken = default)
    {
        if (!SelectedSensorId.HasValue || !_canUndo)
            return;

        var sensorId = SelectedSensorId.Value;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            CommandResponse = "Executing undo operation...";

            var result = await _apiClient.UndoLastSensorCommandAsync(
                sensorId,
                cancellationToken);

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ??
                    "Undo operation failed.";

                CommandResponse = ErrorMessage;
                return;
            }

            await RecordUserBehaviorAsync(
                sensorId,
                $"UNDO: {result.InverseCommand}",
                true,
                result.Response ?? "Command successfully reversed.",
                cancellationToken);

            var undone = CommandHistory.FirstOrDefault(
                x => x.SensorId == sensorId &&
                     x.HistoryStatus == "EXECUTED" &&
                     !string.IsNullOrWhiteSpace(x.InverseCommand));

            if (undone is not null)
                undone.HistoryStatus = "UNDONE";

            _canUndo = false;

            CommandResponse = result.Response ??
                "Undo completed successfully.";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Undo failed: {ex.Message}";
            CommandResponse = ErrorMessage;
        }
        finally
        {
            IsBusy = false;
            RefreshCommandStates();
        }
    }

    private async Task LoadCommandHistoryAsync(
        CancellationToken cancellationToken = default)
    {
        if (!SelectedSensorId.HasValue)
        {
            CommandHistory.Clear();
            return;
        }

        var sensorId = SelectedSensorId.Value;

        var behaviors = await _userBehaviorCache.GetBySensorIdAsync(
            sensorId,
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        CommandHistory.Clear();

        foreach (var behavior in behaviors
            .Where(x => x.SensorId == sensorId)
            .OrderByDescending(x => x.Timestamp))
        {
            CommandHistory.Add(new SensorCommand
            {
                SensorId = behavior.SensorId,
                Command = behavior.Command,
                Response = behavior.Outcome ?? string.Empty,
                Timestamp = behavior.Timestamp,
                HistoryStatus = behavior.Succeeded
                    ? "RECORDED"
                    : "FAILED"
            });
        }

        // The API remains authoritative for undo availability.
        _canUndo = false;

        RefreshCommandStates();
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

        await _userBehaviorCache.AddAsync(
            new UserBehaviorDto
            {
                UserId = _session.UserId,
                SensorId = sensorId,
                Command = command,
                Timestamp = now,
                Succeeded = succeeded,
                Outcome = outcome,
                CreatedAt = now,
                UpdatedAt = now
            },
            cancellationToken);
    }

    private async Task AnalyzeAnomalyAsync(
    CancellationToken cancellationToken = default)
    {
        if (!SelectedSensorId.HasValue)
            return;

        var sensorId = SelectedSensorId.Value;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            PredictionMessage = "Analysing sensor data...";

            await LoadPredictiveSuggestionsAsync(cancellationToken);

            if (SelectedSensorId != sensorId)
                return;

            Recommendation = PredictiveSuggestions
                .OrderByDescending(x => x.Score)
                .FirstOrDefault();

            if (Recommendation is null)
            {
                PredictionMessage =
                    "No predictive recommendations are currently available.";

                return;
            }

            PredictionMessage =
                $"Analysis completed. {PredictiveSuggestions.Count} suggestion(s) found.";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            PredictionMessage = "Predictive analysis failed.";
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
            RefreshCommandStates();
        }
    }

    private Task SendToCommandCentreAsync()
    {
        if (Recommendation is null ||
            string.IsNullOrWhiteSpace(Recommendation.RecommendedCommand))
        {
            return Task.CompletedTask;
        }

        ManualCommand = Recommendation.RecommendedCommand.Trim();

        CommandResponse =
            "Predictive recommendation transferred. Review the command before execution.";

        PredictionMessage =
            "Recommendation transferred successfully. Awaiting user confirmation.";

        return Task.CompletedTask;
    }

    private async Task LoadPredictiveSuggestionsAsync(
    CancellationToken cancellationToken = default)
    {
        PredictiveSuggestions.Clear();
        Recommendation = null;

        if (!SelectedSensorId.HasValue)
        {
            PredictionMessage = "No sensor selected.";
            return;
        }

        var sensorId = SelectedSensorId.Value;

        try
        {
            PredictionMessage = "Connecting to predictive API...";

            System.Diagnostics.Debug.WriteLine(
                $"ANALYSIS SENSOR: {sensorId}");

            var suggestions =
                await _apiClient.GetPredictiveSuggestionsAsync(
                    sensorId,
                    cancellationToken);

            System.Diagnostics.Debug.WriteLine(
                $"SUGGESTIONS FOUND: {suggestions?.Count ?? 0}");

            if (SelectedSensorId != sensorId)
                return;

            if (suggestions is null || suggestions.Count == 0)
            {
                PredictionMessage =
                    "No predictive recommendations found.";

                return;
            }

            foreach (var suggestion in suggestions)
            {
                PredictiveSuggestions.Add(suggestion);
            }

            Recommendation = suggestions
                .OrderByDescending(x => x.Score)
                .FirstOrDefault();

            PredictionMessage =
                $"{suggestions.Count} predictive recommendation(s) loaded.";
        }
        catch (OperationCanceledException)
        {
            PredictionMessage = "Analysis request was cancelled.";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"PREDICTIVE ERROR: {ex}");

            PredictionMessage =
                $"Analysis failed: {ex.Message}";
        }
    }
    public async Task ApplyNavigationRequestAsync(
     CommandNavigationRequest request,
     CancellationToken cancellationToken = default)
    {
        if (request is null ||
            request.SensorId == Guid.Empty ||
            string.IsNullOrWhiteSpace(request.Command))
            return;

        SelectedSensorId = request.SensorId;

        ManualCommand = request.Command.Trim();

        CommandResponse =
            $"Recommendation loaded from {request.Source}. " +
            "Review the command before sending.";

        await LoadCommandHistoryAsync(cancellationToken);
    }
    public void ReceiveRecommendation(
    CommandNavigationRequest request)
    {
        if (request.SensorId == Guid.Empty ||
            string.IsNullOrWhiteSpace(request.Command))
            return;

        // Populate your existing command review properties here.
        SelectedSensorId = request.SensorId;

        // Store the proposed command for administrator review.
    }
    private void RefreshCommandStates()
    {
        OnPropertyChanged(nameof(CanUndoCommand));
        OnPropertyChanged(nameof(CanSendCommand));
        OnPropertyChanged(nameof(CanSendToCommandCentre));

        _sendToCommandCentreCommand.RaiseCanExecuteChanged();
        _loadCommand.RaiseCanExecuteChanged();
        _sendCommand.RaiseCanExecuteChanged();
        _undoCommand.RaiseCanExecuteChanged();
        _analyzeAnomalyCommand.RaiseCanExecuteChanged();
    }

    protected override void RaiseCommandStates()
    {
        RefreshCommandStates();
    }
}