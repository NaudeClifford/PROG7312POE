using SmartX.Domain.Entities;

namespace SmartX.Domain.Interfaces;

public interface ITelemetryRepository
{
    Task AddAsync(
        Telemetry telemetry,
        CancellationToken cancellationToken = default);

    Task<Telemetry?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Telemetry>> GetBySensorIdAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default);

    Task<Telemetry?> GetLatestBySensorIdAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Telemetry>> GetBySensorAndDateAsync(
        Guid sensorId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);
}