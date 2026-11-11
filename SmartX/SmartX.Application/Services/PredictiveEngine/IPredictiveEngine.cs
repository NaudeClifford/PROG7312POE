using SmartX.Domain.Entities;
using SmartX.Shared.DTOs;
using SmartX.Shared.DTOs.Telemetry;


namespace SmartX.Application.Services.PredictiveEngine
{
    public interface IPredictiveEngine
    {
        IReadOnlyList<PredictiveSuggestion> GenerateWarnings(
            IEnumerable<TelemetryDto> telemetryDto,
            IEnumerable<UserBehaviorDto> userBehaviors);
    }

}
