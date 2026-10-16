using SmartX.Domain.Entities;

namespace SmartX.Domain.Interfaces;

public interface IUserBehaviorRepository
{
    Task<UserBehavior?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserBehavior>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserBehavior>> GetSensorByIdAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserBehavior>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        UserBehavior userBehavior,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        UserBehavior userBehavior,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}