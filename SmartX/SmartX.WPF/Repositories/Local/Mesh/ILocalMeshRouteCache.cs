using SmartX.Domain.Entities.Mesh;

namespace SmartX.WPF.Repositories.Local;

public interface ILocalMeshRouteCache
{
    Task<IReadOnlyList<MeshRoute>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MeshRoute>> GetByGatewayIdAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MeshRoute>> GetByNodeIdAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        MeshRoute route,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        MeshRoute route,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}