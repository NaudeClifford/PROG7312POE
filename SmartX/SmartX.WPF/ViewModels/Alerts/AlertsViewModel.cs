using SmartX.Application.Services.Alerts;
using SmartX.Domain.Enums;
using SmartX.WPF.Services.Connectivity;
using SmartX.WPF.Services.Session;
using SmartX.WPF.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace SmartX.WPF.ViewModels.Alerts;

public sealed class AlertsViewModel : ViewModelBase
{
    private readonly AlertQueue _alertQueue;
    public AsyncRelayCommand ProcessNextAlertCommand { get; }

    public AsyncRelayCommand ProcessAllAlertsCommand { get; }
    public AsyncRelayCommand AddDemoAlertsCommand { get; }

    public AsyncRelayCommand ClearAlertsCommand { get; }
    public ObservableCollection<SensorAlert> Alerts { get; } = [];

    public PredictiveWarningViewModel PredictiveWarnings { get; }

    public ObservableCollection<SensorAlert> ProcessedAlerts { get; } = [];
    private readonly Dispatcher _dispatcher = System.Windows.Application.Current.Dispatcher;

    public int PendingAlertCount =>
        _alertQueue.Count;
    public int PredictiveWarningCount =>
        PredictiveWarnings.Suggestions.Count;

    public AlertsViewModel(
        AlertQueue alertQueue,
        IConnectivityService connectivityService,
        PredictiveWarningViewModel predictiveWarningViewModel,
        SmartXSession session)
        : base(
            connectivityService,
            session)
    {
        _alertQueue = alertQueue;
        PredictiveWarnings = predictiveWarningViewModel;

        AddDemoAlertsCommand = new AsyncRelayCommand(AddDemoAlertsAsync);


        PredictiveWarnings.Suggestions.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(PredictiveWarningCount));
        };

        ProcessNextAlertCommand = new AsyncRelayCommand(ProcessNextAlertAsync, CanProcessAlert);

        ProcessAllAlertsCommand = new AsyncRelayCommand(ProcessAllAlertsAsync, CanProcessAlert);

        ClearAlertsCommand = new AsyncRelayCommand(ClearAlertsAsync);

        _alertQueue.AlertEnqueued += OnAlertEnqueued;

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

            cancellationToken.ThrowIfCancellationRequested();

            OnPropertyChanged(
                nameof(PendingAlertCount));

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

        await Task.CompletedTask;
    }

    public Task ProcessNextAlertAsync()
    {
        if (_alertQueue.TryDequeue(out var alert) &&
            alert is not null)
        {
            // Remove the alert from the live inbox.
            Alerts.Remove(alert);

            // Retain it in processed history.
            ProcessedAlerts.Insert(0, alert);
        }

        OnPropertyChanged(nameof(PendingAlertCount));
        RaiseCommandStates();

        return Task.CompletedTask;
    }

    private bool CanProcessAlert()
    {
        return _alertQueue.Count > 0;
    }
    public Task AddDemoAlertsAsync()
    {
        var sensorId = PredictiveWarnings.Suggestions
            .FirstOrDefault()?.SensorId;

        if (sensorId is null)
        {
            ErrorMessage =
                "Load a sensor or generate telemetry before creating demo alerts.";

            return Task.CompletedTask;
        }

        _alertQueue.Enqueue(new SensorAlert
        {
            SensorId = sensorId.Value,
            Message = "DEMO: Critical temperature warning.",
            Severity = AlertSeverity.Critical,
            Timestamp = DateTime.Now
        });

        _alertQueue.Enqueue(new SensorAlert
        {
            SensorId = sensorId.Value,
            Message = "DEMO: Routine sensor status update.",
            Severity = AlertSeverity.Medium,
            Timestamp = DateTime.Now
        });

        return Task.CompletedTask;
    }
    public Task ProcessAllAlertsAsync()
    {
        while (_alertQueue.TryDequeue(out var alert))
        {
            if (alert is null)
                continue;

            // Remove each processed alert from the live inbox.
            Alerts.Remove(alert);

            // Retain the alert in history.
            ProcessedAlerts.Insert(0, alert);
        }

        OnPropertyChanged(nameof(PendingAlertCount));
        RaiseCommandStates();

        return Task.CompletedTask;
    }

    private void OnAlertEnqueued(object? sender, SensorAlert alert)
    {
        if (_dispatcher.CheckAccess())
        {
            AddAlertToInbox(alert);
            return;
        }

        _dispatcher.BeginInvoke(
            new Action(() => AddAlertToInbox(alert)));
    }

    private void AddAlertToInbox(SensorAlert alert)
    {
        Alerts.Insert(0, alert);

        OnPropertyChanged(nameof(PendingAlertCount));
        RaiseCommandStates();
    }

    public Task ClearAlertsAsync()
    {
        _alertQueue.Clear();

        Alerts.Clear();
        ProcessedAlerts.Clear();

        OnPropertyChanged(nameof(PendingAlertCount));
        RaiseCommandStates();

        return Task.CompletedTask;
    }

    protected override void RaiseCommandStates()
    {
        ProcessNextAlertCommand.RaiseCanExecuteChanged();
        ProcessAllAlertsCommand.RaiseCanExecuteChanged();
        ClearAlertsCommand.RaiseCanExecuteChanged();
        AddDemoAlertsCommand.RaiseCanExecuteChanged();
    }
}