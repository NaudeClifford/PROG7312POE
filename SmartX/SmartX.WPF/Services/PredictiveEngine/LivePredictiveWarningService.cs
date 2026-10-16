
using SmartX.Domain.Entities;
using SmartX.Shared.DTOs;
using SmartX.Shared.DTOs.Telemetry;
using SmartX.Shared.Mapping;
using SmartX.WPF.Repositories.Local;
using SmartX.WPF.Services.Session;
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

    private readonly ILocalUserBehaviorCache _behaviorCache;
    private readonly SmartXSession _session;

    public ObservableCollection<PredictiveSuggestion> Warnings { get; }
        = new();
    private List<UserBehaviorDto> _loadedBehaviors = [];

    private Guid? _loadedGatewayId;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    public LivePredictiveWarningService(
        IPredictiveEngine predictiveEngine,
        IMapper mapper,
        TelemetryCollection telemetryCollection,
        ILocalTelemetryCache telemetryCache,
        ILocalSensorCache sensorCache,
        ILocalUserBehaviorCache behaviorCache,
        SmartXSession session
        )
    {
        _predictiveEngine = predictiveEngine;
        _mapper = mapper;
        _telemetryCollection = telemetryCollection;
        _telemetryCache = telemetryCache;
        _sensorCache = sensorCache;
        _behaviorCache = behaviorCache;
        _session = session;
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

            var sensorIds = telemetry.Select(item => item.SensorId)
                .Distinct().ToList();

            var behaviors = new List<UserBehaviorDto>();

            foreach (var sensorId in sensorIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var sensorBehaviors =
                    await _behaviorCache.GetBySensorIdAsync(
                        sensorId,
                        cancellationToken);

                behaviors.AddRange(
                    sensorBehaviors.Where(
                        behavior => behavior.UserId == _session.UserId));
            }

            _loadedBehaviors = behaviors;

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

        var userBehaviors = _telemetryCollection
            .GetNewestFirst()
            .Select(reading => reading.SensorId)
            .Distinct()
            .SelectMany(sensorId =>
                _behaviorCache.GetBySensorIdAsync(sensorId)
                    .GetAwaiter()
                    .GetResult())
            .Where(behavior => behavior.UserId == _session.UserId)
            .ToList();

        var generated = _predictiveEngine.GenerateWarnings(telemetryDtos, userBehaviors);

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
        _loadedBehaviors.Clear();
        Warnings.Clear();
    }
}
