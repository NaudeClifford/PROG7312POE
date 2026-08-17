using SmartX.WPF.Cache;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartX.WPF.Repositories.Local
{
    public interface ICacheMetadataRepository
    {

        Task<CacheMetadata?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<CacheMetadata>> GetAllAsync(
            CancellationToken cancellationToken = default);

        Task UpdateAsync(
            CacheMetadata data, CancellationToken cancellationToken = default);

        Task DeleteAsync(
            Guid id, CancellationToken cancellationToken = default);
    }
}
