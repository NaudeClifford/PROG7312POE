using SmartX.Domain.Entities;

namespace SmartX.WPF.Repositories.Local;

public interface ILocalCompanyCache
{
    Task<Company?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Company>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Company company,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}