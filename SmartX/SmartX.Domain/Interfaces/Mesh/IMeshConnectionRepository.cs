using SmartX.Domain.Entities.Mesh;

namespace SmartX.Domain.Interfaces.Mesh;

public interface IMeshConnectionRepository
{
    Task<IReadOnlyList<MeshConnection>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MeshConnection>> GetByNodeIdAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        MeshConnection connection,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        MeshConnection connection,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}