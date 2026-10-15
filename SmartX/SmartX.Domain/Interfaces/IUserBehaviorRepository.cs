using SmartX.Domain.Entities;

namespace SmartX.Domain.Interfaces
{
    public interface IUserBehaviorRepository
    {

        Task<UserBehavior?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<UserBehavior>> GetAllAsync(
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<UserBehavior>> GetByCompanyIdAsync(
            Guid companyId,
            CancellationToken cancellationToken = default);

        Task<UserBehavior?> GetSensorByIdAsync(
            Guid sensorId,
            CancellationToken cancellationToken = default);

        Task<UserBehavior?> GetSensorCompanyIdAsync(
            Guid sensorId,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            UserBehavior userBehavior,
            CancellationToken cancellationToken = default);

        Task UpdateAsync(
            UserBehavior userBehavior,
            CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid Id, CancellationToken cancellationToken);
    }
}
