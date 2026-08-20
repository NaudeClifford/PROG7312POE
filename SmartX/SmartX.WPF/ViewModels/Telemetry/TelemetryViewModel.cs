using DomainTelemetry = SmartX.Domain.Entities.Telemetry;
using SmartX.WPF.Repositories.Local;
using SmartX.WPF.Services;
using SmartX.WPF.ViewModels.Base;
using System.Collections.ObjectModel;

namespace SmartX.WPF.ViewModels.Telemetry;

public class TelemetryViewModel : ViewModelBase
{
    private readonly ILocalTelemetryCache _telemetryCache;

    private bool _isBusy;
    private string _errorMessage = string.Empty;
    private Guid _sensorId;

    public ObservableCollection<DomainTelemetry> Telemetry { get; } = [];

    public DomainTelemetry? LatestTelemetry =>
        Telemetry
            .OrderByDescending(x => x.Timestamp)
            .FirstOrDefault();

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public double? Temperature =>
        LatestTelemetry?.Temperature;

    public double? Voltage =>
        LatestTelemetry?.Voltage;

    public double? Current =>
        LatestTelemetry?.Current;

    public double? Power =>
        LatestTelemetry?.Power;

    public TelemetryViewModel(
        ILocalTelemetryCache telemetryCache,
        SmartXSession session)
    {
        _telemetryCache = telemetryCache;
    }

    public async Task LoadAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;
            _sensorId = sensorId;

            Telemetry.Clear();

            var telemetry =
                await _telemetryCache.GetBySensorIdAsync(
                    sensorId,
                    cancellationToken);

            foreach (var item in telemetry)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Telemetry.Add(item);
            }

            OnPropertyChanged(nameof(LatestTelemetry));
            OnPropertyChanged(nameof(Temperature));
            OnPropertyChanged(nameof(Voltage));
            OnPropertyChanged(nameof(Current));
            OnPropertyChanged(nameof(Power));
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
}