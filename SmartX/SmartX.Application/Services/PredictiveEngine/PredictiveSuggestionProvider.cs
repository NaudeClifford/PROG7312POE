using SmartX.Domain.Interfaces;
using SmartX.Shared.DTOs;

namespace SmartX.Application.Services.PredictiveEngine;

public sealed class PredictiveSuggestionProvider
    : IPredictiveSuggestionProvider
{
    private readonly IPredictiveWarningService _service;
    private readonly ISensorRepository _sensorRepository;
    private readonly IGatewayRepository _gatewayRepository;
    public PredictiveSuggestionProvider(
        IPredictiveWarningService service,
        ISensorRepository sensorRepository,
        IGatewayRepository gatewayRepository,
        IUserRepository userRepository)
    {
        _service = service;
        _sensorRepository = sensorRepository;
        _gatewayRepository = gatewayRepository;
    }


    public async Task<IReadOnlyList<PredictiveSuggestionDto>>
        GetSuggestionsAsync(
            Guid sensorId,
            Guid userId,
            CancellationToken cancellationToken = default)
    {
        if (sensorId == Guid.Empty)
            return [];

        var sensor = await _sensorRepository.GetByIdAsync(
            sensorId,
            cancellationToken);

        if (sensor?.GatewayId is not Guid gatewayId)
            return [];

        var gateway = await _gatewayRepository.GetByIdAsync(
            gatewayId,
            cancellationToken);

        if (gateway is null)
            return [];

        // Retrieve gateway warnings.
        var warnings = await _service.GetWarningsAsync(
            gatewayId,
            userId,
            cancellationToken);

        // Return only warnings belonging to the selected sensor.
        return warnings
            .Where(x => x.SensorId == sensorId)
            .ToList();
    }

}
