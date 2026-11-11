using DomainTelemetry = SmartX.Domain.Entities.Telemetry;

namespace SmartX.Application.Services.Alerts;

public interface ISensorAlertProcessor
{
    Task<bool> ProcessAsync(
        DomainTelemetry telemetry,
        CancellationToken cancellationToken = default);
}