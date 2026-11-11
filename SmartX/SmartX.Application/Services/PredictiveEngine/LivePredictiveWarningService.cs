
using SmartX.Application.Services.PredictiveEngine;
using SmartX.Domain.Interfaces;
using SmartX.Shared.DTOs;
using SmartX.Shared.DTOs.Telemetry;
using SmartX.Shared.Mapping;

namespace SmartX.API.Services.PredictiveEngine;

public sealed class LivePredictiveWarningService
    : IPredictiveWarningService
{
    private readonly IPredictiveEngine _predictiveEngine;
    private readonly ITelemetryRepository _telemetryRepository;
    private readonly ISensorRepository _sensorRepository;
    private readonly IUserBehaviorRepository _behaviorRepository;
    private readonly IMapper _mapper;
    private const int LiveHistoryWindow = 10;

    public LivePredictiveWarningService(
        IPredictiveEngine predictiveEngine,
        ITelemetryRepository telemetryRepository,
        ISensorRepository sensorRepository,
        IUserBehaviorRepository behaviorRepository,
        IMapper mapper)
    {
        _predictiveEngine = predictiveEngine;
        _telemetryRepository = telemetryRepository;
        _sensorRepository = sensorRepository;
        _behaviorRepository = behaviorRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<PredictiveSuggestionDto>>
        GetWarningsAsync(
            Guid gatewayId,
            Guid userId,
            CancellationToken cancellationToken = default)
    {
        if (gatewayId == Guid.Empty)
            throw new ArgumentException("Gateway ID is required.");

        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.");

        // Load gateway telemetry.
        var telemetry =
            await _telemetryRepository.GetLatestByGatewayIdAsync(
                gatewayId,
                LiveHistoryWindow,
                cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        if (telemetry.Count == 0)
            return [];

        // Load sensors for their display names.
        var sensors =
            await _sensorRepository.GetByGatewayIdAsync(
                gatewayId,
                cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        var sensorNames = sensors.ToDictionary(
            sensor => sensor.Id,
            sensor => sensor.Name);

        // Only use behavior belonging to the requesting user.
        var userBehaviors =
            await _behaviorRepository.GetByUserIdAsync(
                userId,
                cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        var relevantSensorIds = telemetry
            .Select(x => x.SensorId)
            .ToHashSet();

        var relevantBehaviors = userBehaviors
            .Where(x => relevantSensorIds.Contains(x.SensorId))
            .ToList();

        // Convert repository entities into engine inputs.
        var telemetryDtos =
            _mapper.Map<List<TelemetryDto>>(telemetry);

        var behaviorDtos =
            _mapper.Map<List<UserBehaviorDto>>(relevantBehaviors);

        // Generate warnings.
        var generated = _predictiveEngine.GenerateWarnings(
            telemetryDtos,
            behaviorDtos);

        // Convert predictions into API response DTOs.
        var results = generated.Select(warning =>
            new PredictiveSuggestionDto
            {
                SensorId = warning.SensorId,

                SensorName = sensorNames.TryGetValue(
                    warning.SensorId,
                    out var sensorName)
                        ? sensorName
                        : "Unknown Sensor",

                Title = warning.Title,
                Description = warning.Description,
                Type = warning.Type,
                Severity = warning.Severity,
                Score = warning.Score,
                RecommendedAction = warning.RecommendedAction,
                RecommendedCommand = warning.RecommendedCommand,
                RecommendationReason = warning.RecommendationReason
            })
            .ToList();

        return results;
    }



}
