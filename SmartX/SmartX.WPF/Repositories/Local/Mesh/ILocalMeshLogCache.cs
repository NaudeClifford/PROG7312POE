using SmartX.Domain.Entities.Mesh;

namespace SmartX.WPF.Repositories.Local;

public interface ILocalMeshLogCache
{
    Task<IReadOnlyList<MeshLog>> GetByGatewayIdAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MeshLog>> GetByNodeIdAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        MeshLog log,
        CancellationToken cancellationToken = default);

    Task DeleteOlderThanAsync(
        DateTime cutoff,
        CancellationToken cancellationToken = default);
}