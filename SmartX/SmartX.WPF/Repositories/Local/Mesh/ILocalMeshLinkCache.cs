using SmartX.Domain.Entities.Mesh;

namespace SmartX.WPF.Repositories.Local;

public interface ILocalMeshLinkCache
{
    Task<MeshLink?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MeshLink>> GetByGatewayIdAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MeshLink>> GetByNodeIdAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        MeshLink link,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}