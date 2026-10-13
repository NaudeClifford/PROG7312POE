
using SmartX.Domain.Entities;
using SmartX.Shared.DTOs.Telemetry;
using SmartX.Shared.Mapping;
using SmartX.WPF.Repositories.Local;
using SmartX.WPF.ViewModels.Telemetry;
using System.Collections.ObjectModel;

namespace SmartX.WPF.Services.PredictiveEngine;

public sealed class LivePredictiveWarningService
{
    private readonly IPredictiveEngine _predictiveEngine;
    private readonly IMapper _mapper;
    private readonly TelemetryCollection _telemetryCollection;
    private readonly ILocalTelemetryCache _telemetryCache;
    private readonly ILocalSensorCache _sensorCache;
    private Dictionary<Guid, string> _sensorNames = new();

    public ObservableCollection<PredictiveSuggestion> Warnings { get; }
        = new();

    private Guid? _loadedGatewayId;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    public LivePredictiveWarningService(
        IPredictiveEngine predictiveEngine,
        IMapper mapper,
        TelemetryCollection telemetryCollection,
        ILocalTelemetryCache telemetryCache,
        ILocalSensorCache sensorCache
        )
    {
        _predictiveEngine = predictiveEngine;
        _mapper = mapper;
        _telemetryCollection = telemetryCollection;
        _telemetryCache = telemetryCache;
        _sensorCache = sensorCache;
    }

    public async Task LoadAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        if (gatewayId == Guid.Empty)
        {
            Clear();
            _telemetryCollection.Clear();
            _sensorNames.Clear();
            _loadedGatewayId = null;
            return;
        }

        await _loadLock.WaitAsync(cancellationToken);

        try
        {

            // Always load all telemetry for the gateway.
            // Sensor filtering belongs to the UI, not the warning engine.
            var telemetry =
                await _telemetryCache.GetByGatewayIdAsync(
                    gatewayId,
                    cancellationToken);


            cancellationToken.ThrowIfCancellationRequested();

            // Load sensor names from the local cache.
            var sensors = await _sensorCache.GetByGatewayIdAsync(
                    gatewayId,
                    cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            _sensorNames = sensors
                .GroupBy(sensor => sensor.Id)
                .ToDictionary(
                    group => group.Key,
                    group => group.First().Name);

            _telemetryCollection.Clear();

            foreach (var item in telemetry)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _telemetryCollection.Add(item);
            }

            _loadedGatewayId = gatewayId;

            Refresh();
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public bool IsLoadedFor(Guid gatewayId)
        => _loadedGatewayId == gatewayId;

    public void Refresh()
    {
        var telemetryDtos = _mapper.Map<List<TelemetryDto>>(
            _telemetryCollection.GetNewestFirst());

        var generated = _predictiveEngine.GenerateWarnings(
            telemetryDtos);

        Warnings.Clear();

        foreach (var warning in generated)
        {
            if (_sensorNames.TryGetValue(
                    warning.SensorId,
                    out var sensorName))
            {
                warning.SensorName = sensorName;
            }
            else
            {
                warning.SensorName = "Unknown Sensor";
            }

            Warnings.Add(warning);
        }
    }

    public void Clear()
    {
        Warnings.Clear();
    }
}
