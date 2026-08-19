using SmartX.Shared.Models;

namespace SmartX.Application.Common;

public interface ICrudHandler<TCreateCommand, TUpdateCommand, TGetByIdQuery, TGetAllQuery, TDto>
{
    Task<Result<Guid>> CreateAsync(
        TCreateCommand command,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> UpdateAsync(
        TUpdateCommand command,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result<TDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<TDto>>> GetAllAsync(
        CancellationToken cancellationToken = default);
}