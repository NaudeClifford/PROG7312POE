using SmartX.Application.Services.Alerts;
using SmartX.WPF.Services.Connectivity;
using SmartX.WPF.Services.Session;
using SmartX.WPF.ViewModels.Base;
using System.Collections.ObjectModel;

namespace SmartX.WPF.ViewModels.Alerts;

public sealed class AlertsViewModel : ViewModelBase
{
    private readonly AlertQueue _alertQueue;
    public AsyncRelayCommand ProcessNextAlertCommand { get; }

    public AsyncRelayCommand ProcessAllAlertsCommand { get; }

    public AsyncRelayCommand ClearAlertsCommand { get; }
    public ObservableCollection<SensorAlert> Alerts { get; } = [];

    public int PendingAlertCount =>
        _alertQueue.Count;

    public AlertsViewModel(
        AlertQueue alertQueue,
        IConnectivityService connectivityService,
        SmartXSession session)
        : base(
            connectivityService,
            session)
    {
        _alertQueue = alertQueue;

        ProcessNextAlertCommand =
       new AsyncRelayCommand(
           ProcessNextAlertAsync,
           CanProcessAlert);

        ProcessAllAlertsCommand =
            new AsyncRelayCommand(
                ProcessAllAlertsAsync,
                CanProcessAlert);

        ClearAlertsCommand =
            new AsyncRelayCommand(
                ClearAlertsAsync);
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

    public async Task ProcessNextAlertAsync()
    {
        if (!_alertQueue.TryDequeue(
                out var alert))
        {
            return;
        }

        if (alert is null)
            return;

        Alerts.Insert(
            0,
            alert);

        OnPropertyChanged(nameof(PendingAlertCount));
        RaiseCommandStates();

        await Task.CompletedTask;

    }
    private bool CanProcessAlert()
    {
        return _alertQueue.Count > 0;
    }

    public async Task ProcessAllAlertsAsync()
    {
    

        while (_alertQueue.TryDequeue(
                   out var alert))
        {
            if (alert is null)
                continue;

            Alerts.Insert(
                0,
                alert);
        }

        OnPropertyChanged(nameof(PendingAlertCount));
        RaiseCommandStates();

        await Task.CompletedTask;
    }

    public async Task ClearAlertsAsync()
    {
        _alertQueue.Clear();
        Alerts.Clear();

        OnPropertyChanged(
            nameof(PendingAlertCount));

        await Task.CompletedTask;

    }

    protected override void RaiseCommandStates()
    {
        ProcessNextAlertCommand.RaiseCanExecuteChanged();
        ProcessAllAlertsCommand.RaiseCanExecuteChanged();
        ClearAlertsCommand.RaiseCanExecuteChanged();
    }
}