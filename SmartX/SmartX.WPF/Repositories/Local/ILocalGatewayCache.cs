using SmartX.Domain.Entities;

namespace SmartX.WPF.Repositories.Local;

public interface ILocalGatewayCache
{
    Task<IReadOnlyList<Gateway>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Gateway?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Gateway>> GetByCompanyIdAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Gateway gateway,
        CancellationToken cancellationToken = default);
}