using SmartX.Domain.Entities;
using SmartX.Shared.DTOs.Telemetry;
using SmartX.Shared.Mapping;
using SmartX.WPF.Repositories.Local;

namespace SmartX.WPF.Services.PredictiveEngine
{
    public sealed class PredictiveSuggestionService(
        ILocalUserBehaviorCache localUserBehaviorCache,
        ILocalTelemetryCache telemetryCache,
        IPredictiveEngine predictiveEngine,
        IMapper mapper)
    {
        private readonly ILocalTelemetryCache _telemetryCache = telemetryCache;
        private readonly IPredictiveEngine _predictiveEngine = predictiveEngine;
        private readonly IMapper _mapper = mapper;
        private readonly ILocalUserBehaviorCache _localUserBehaviorCache = localUserBehaviorCache;

        public async Task<IReadOnlyList<PredictiveSuggestion>> GetWarningsAsync(
            CancellationToken cancellationToken = default)
        {

            var telemetry = await _telemetryCache.GetAllAsync(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            var dto = _mapper.Map<IReadOnlyList<TelemetryDto>>(telemetry);

            var sensorIds = dto.Select(t => t.SensorId).Distinct().ToList();

            var behaviorTask = sensorIds.Select(sensorId => _localUserBehaviorCache.GetBySensorIdAsync(sensorId, cancellationToken));

            var behaviorResults = await Task.WhenAll(behaviorTask);

            cancellationToken.ThrowIfCancellationRequested();

            var behaviors = behaviorResults.SelectMany(b => b).ToList();

            return _predictiveEngine.GenerateWarnings(dto, behaviors);
        }
    }
}
