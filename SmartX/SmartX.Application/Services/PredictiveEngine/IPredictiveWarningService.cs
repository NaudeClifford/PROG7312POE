using SmartX.Shared.DTOs;

namespace SmartX.Application.Services.PredictiveEngine;

public interface IPredictiveWarningService
{
    Task<IReadOnlyList<PredictiveSuggestionDto>> GetWarningsAsync(
        Guid gatewayId,
        Guid userId,
        CancellationToken cancellationToken = default);
}