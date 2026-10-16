using SmartX.Shared.DTOs;

namespace SmartX.WPF.Repositories.Local
{
    public interface ILocalUserBehaviorCache
    {
        Task<UserBehaviorDto?> GetByIdAsync(
                Guid id,
                CancellationToken cancellationToken = default);

        Task<IReadOnlyList<UserBehaviorDto>> GetAllAsync(
            CancellationToken cancellationToken = default);

        Task AddAsync(UserBehaviorDto userBehavior, CancellationToken cancellationToken = default);

        Task UpdateAsync(UserBehaviorDto userBehavior, CancellationToken cancellationToken = default);

        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<UserBehaviorDto>> GetBySensorIdAsync(
            Guid sensorId,
            CancellationToken cancellationToken = default);
    }
}
