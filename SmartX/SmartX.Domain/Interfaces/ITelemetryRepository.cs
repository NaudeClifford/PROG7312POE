using SmartX.Domain.Entities;

namespace SmartX.Domain.Interfaces;

public interface ITelemetryRepository
{
    Task AddAsync(
        Telemetry telemetry,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Telemetry>> GetBySensorAsync(
        Guid sensorId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);
}