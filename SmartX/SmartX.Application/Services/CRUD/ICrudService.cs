using System.Security.Claims;
using SmartX.Shared.Models;

namespace SmartX.Domain.Interfaces;

public interface ICrudService<TDto, TCreateRequest, TUpdateRequest>
{
    Task<Result<IReadOnlyList<TDto>>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Result<TDto>> GetByIdAsync(
        Guid id,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<Result<Guid>> CreateAsync(
        TCreateRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> UpdateAsync(
        TUpdateRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> DeleteAsync(
        Guid id,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);
}
