using SmartX.Domain.Enums;
using SmartX.Shared.DTOs;
using SmartX.WPF.Services.Api;
using SmartX.WPF.Services.Connectivity;
using SmartX.WPF.Services.Session;
using SmartX.WPF.ViewModels.Base;
using System.Collections.ObjectModel;

namespace SmartX.WPF.ViewModels.Alerts;

public sealed class AlertsViewModel : ViewModelBase
{
    private readonly ISmartXApiClient _apiClient;

    public AsyncRelayCommand ProcessNextAlertCommand { get; }

    public AsyncRelayCommand ProcessAllAlertsCommand { get; }
    public AsyncRelayCommand AddDemoAlertsCommand { get; }

    public AsyncRelayCommand ClearAlertsCommand { get; }
    public ObservableCollection<SensorAlert> Alerts { get; } = [];

    public PredictiveWarningViewModel PredictiveWarnings { get; }

    public ObservableCollection<SensorAlert> ProcessedAlerts { get; } = [];

    private int _pendingAlertCount;

    public int PendingAlertCount
    {
        get => _pendingAlertCount;
        private set => SetProperty(
            ref _pendingAlertCount,
            value);
    }

    public int PredictiveWarningCount =>
        PredictiveWarnings.Suggestions.Count;

    public AlertsViewModel(
    IConnectivityService connectivityService,
    PredictiveWarningViewModel predictiveWarningViewModel,
    ISmartXApiClient apiClient,
    SmartXSession session)
    : base(connectivityService, session)
    {
        _apiClient = apiClient;
        PredictiveWarnings = predictiveWarningViewModel;

        AddDemoAlertsCommand = new AsyncRelayCommand(AddDemoAlertsAsync);

        PredictiveWarnings.Suggestions.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(PredictiveWarningCount));
        };

        ProcessNextAlertCommand =
            new AsyncRelayCommand(ProcessNextAlertAsync, CanProcessAlert);

        ProcessAllAlertsCommand =
            new AsyncRelayCommand(ProcessAllAlertsAsync, CanProcessAlert);

        ClearAlertsCommand =
            new AsyncRelayCommand(ClearAlertsAsync);
    }

    public async Task LoadAsync(
    CancellationToken cancellationToken = default)
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            var alerts = await _apiClient.GetAlertsAsync(
                cancellationToken);

            Alerts.Clear();

            foreach (var alert in alerts)
                Alerts.Add(alert);

            PendingAlertCount = alerts.Count;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Unable to load alerts: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            RaiseCommandStates();
        }
    }

    public async Task ProcessNextAlertAsync()
    {
        if (IsBusy || PendingAlertCount == 0)
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            var processed = await _apiClient.ProcessNextAlertAsync();

            if (processed is not null)
                ProcessedAlerts.Insert(0, processed);

            await RefreshAlertsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Unable to process alert: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            RaiseCommandStates();
        }
    }

    public async Task ProcessAllAlertsAsync()
    {
        if (IsBusy || PendingAlertCount == 0)
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            var processed = await _apiClient.ProcessAllAlertsAsync();

            foreach (var alert in processed)
                ProcessedAlerts.Insert(0, alert);

            await RefreshAlertsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Unable to process alerts: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            RaiseCommandStates();
        }
    }

    private async Task RefreshAlertsAsync(
    CancellationToken cancellationToken = default)
    {
        var alerts = await _apiClient.GetAlertsAsync(cancellationToken);

        Alerts.Clear();

        foreach (var alert in alerts)
            Alerts.Add(alert);

        PendingAlertCount = Alerts.Count;

        RaiseCommandStates();
    }

    private bool CanProcessAlert()
    {
        return PendingAlertCount > 0 && !IsBusy;
    }

    public async Task AddDemoAlertsAsync()
    {
        var sensorId = PredictiveWarnings.Suggestions
            .FirstOrDefault()?.SensorId;

        if (sensorId is null)
        {
            ErrorMessage =
                "Select a sensor or load predictive suggestions first.";

            return;
        }

        try
        {
            await _apiClient.EnqueueAlertAsync(new SensorAlert
            {
                SensorId = sensorId.Value,
                Message = "DEMO: Critical temperature warning.",
                Severity = AlertSeverity.Critical,
                Timestamp = DateTime.UtcNow
            });

            await _apiClient.EnqueueAlertAsync(new SensorAlert
            {
                SensorId = sensorId.Value,
                Message = "DEMO: Routine sensor status update.",
                Severity = AlertSeverity.Medium,
                Timestamp = DateTime.UtcNow
            });

            await RefreshAlertsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Unable to add demo alerts: {ex.Message}";
        }
    }

    public async Task ClearAlertsAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            await _apiClient.ClearAlertsAsync();

            Alerts.Clear();
            ProcessedAlerts.Clear();

            PendingAlertCount = 0;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Unable to clear alerts: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            RaiseCommandStates();
        }
    }

    protected override void RaiseCommandStates()
    {
        ProcessNextAlertCommand.RaiseCanExecuteChanged();
        ProcessAllAlertsCommand.RaiseCanExecuteChanged();
        ClearAlertsCommand.RaiseCanExecuteChanged();
        AddDemoAlertsCommand.RaiseCanExecuteChanged();
    }
}