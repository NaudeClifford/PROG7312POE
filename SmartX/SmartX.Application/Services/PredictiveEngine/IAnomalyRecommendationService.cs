
using SmartX.Shared.DTOs;

namespace SmartX.Application.Services.PredictiveEngine;

public interface IAnomalyRecommendationService
{
    Task<AnomalyRecommendationDto?> AnalyzeAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default);
}
