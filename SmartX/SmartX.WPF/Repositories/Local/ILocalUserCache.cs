using SmartX.Shared.DTOs;

namespace SmartX.WPF.Repositories.Local;

public interface ILocalUserCache
{
    Task<UserDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserDto>> GetByCompanyIdAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task<UserDto?> GetByFirebaseUidAsync(
        string firebaseUid,
        CancellationToken cancellationToken = default);

    Task<UserDto?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        UserDto user,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}