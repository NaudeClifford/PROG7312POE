using SmartX.Domain.Entities;

namespace SmartX.Domain.Interfaces;

public interface ISensorLogFileRepository
{
    Task<SensorLogFile?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SensorLogFile>> GetBySensorIdAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SensorLogFile>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        SensorLogFile logFile,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        SensorLogFile logFile,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}