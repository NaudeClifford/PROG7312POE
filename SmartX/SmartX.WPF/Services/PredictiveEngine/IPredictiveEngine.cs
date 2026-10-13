using SmartX.Domain.Entities;
using SmartX.Shared.DTOs.Telemetry;


namespace SmartX.WPF.Services.PredictiveEngine
{
    public interface IPredictiveEngine
    {
        IReadOnlyList<PredictiveSuggestion> GenerateWarnings(
            IEnumerable<TelemetryDto> telemetryDto);
    }

}
