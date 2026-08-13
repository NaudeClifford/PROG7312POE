

using SmartX.Domain.Entities;

namespace SmartX.WPF.Repositories.Local
{
    public interface ILocalTelemetryCache
    {

        Task<Telemetry?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Telemetry>> GetAllAsync(
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Telemetry>> GetBySensorIdAsync(
            Guid sensorId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Telemetry>> GetLatestBySensorIdAsync(
            Guid sensorId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Telemetry>> GetBySensorAndDateAsync(
        Guid sensorId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

        Task UpdateAsync(
            Telemetry telemetry, CancellationToken cancellationToken = default);
    }
}
