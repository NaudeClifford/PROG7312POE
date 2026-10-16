using SmartX.WPF.Models.History;

namespace SmartX.WPF.Services.PredictiveEngine;

public interface IAnomalyRecommendationService
{
    Task<AnomalyRecommendation?> AnalyzeAsync(
        Guid sensorId,
        IReadOnlyCollection<SmartX.Domain.Entities.Telemetry> telemetry,
        CancellationToken cancellationToken = default);
}
