using SmartX.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartX.Domain.Interfaces
{
    public interface IGatewayRepository
    {
        Task<Gateway?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Gateway>> GetAllAsync(
            CancellationToken cancellationToken = default);

        Task AddAsync(
            Gateway gateway,
            CancellationToken cancellationToken = default);

        Task UpdateAsync(
            Gateway gateway,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(
            Guid id,
            CancellationToken cancellationToken = default);
    }
}
