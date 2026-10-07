using SmartX.Application.Services.Alerts;
using SmartX.Domain.Entities;
using SmartX.Shared.DTOs.Telemetry;


namespace SmartX.WPF.Services.PredictiveEngine
{
    public interface IPredictiveEngine
    {
        IReadOnlyList<PredictiveSuggestion> GenerateSuggestions(
            IEnumerable<TelemetryDto> telemetryDto,
            IEnumerable<SensorAlert> alerts);
    }

}
