using SmartX.Domain.Entities;

namespace SmartX.Domain.Interfaces
{
    public interface ICompanyConfigurationRepository
    {
        Task<CompanyConfiguration?> GetByCompanyIdAsync(
            Guid companyId,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            CompanyConfiguration configuration,
            CancellationToken cancellationToken = default);

        Task UpdateAsync(
            CompanyConfiguration configuration,
            CancellationToken cancellationToken = default);
    }

}
