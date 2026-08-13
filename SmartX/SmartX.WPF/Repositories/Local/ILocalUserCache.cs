using SmartX.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartX.WPF.Repositories.Local
{
    public interface ILocalUserCache
    {
        Task<User?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        Task<User?> GetByFirebaseUidAsync(
            string firebaseUid,
            CancellationToken cancellationToken = default);

        Task<User?> GetByEmailAsync(
            string email,
            CancellationToken cancellationToken = default);

        Task UpdateAsync(
            User user,
            CancellationToken cancellationToken = default);
    }
}
