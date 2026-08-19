using SmartX.Shared.Models;

namespace SmartX.Application.Services.CRUD;

public interface ICrudService<TDto, TCreateCommand, TUpdateCommand>
{
    Task<Result<IReadOnlyList<TDto>>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Result<TDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result<Guid>> CreateAsync(
        TCreateCommand command,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> UpdateAsync(
        TUpdateCommand command,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}