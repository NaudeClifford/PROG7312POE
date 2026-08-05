using SmartX.Domain.Entities;

namespace SmartX.Domain.Interfaces;

public interface ISensorRepository
{
    Task<Sensor?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Sensor>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Sensor sensor,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Sensor sensor,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}