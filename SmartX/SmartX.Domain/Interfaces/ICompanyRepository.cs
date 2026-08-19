using SmartX.Domain.Entities;

namespace SmartX.Domain.Interfaces
{
    public interface ICompanyRepository
    {

        Task<Company?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Company>> GetAllAsync(
            CancellationToken cancellationToken = default);

        Task AddAsync(
            Company company,
            CancellationToken cancellationToken = default);

        Task UpdateAsync(
            Company company,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(
            Guid id,
            CancellationToken cancellationToken = default);
    }
}
