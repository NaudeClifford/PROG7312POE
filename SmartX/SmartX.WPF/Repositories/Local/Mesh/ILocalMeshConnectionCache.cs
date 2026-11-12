using SmartX.Domain.Entities.Mesh;

namespace SmartX.WPF.Repositories.Local;

public interface ILocalMeshConnectionCache
{
    Task<IReadOnlyList<MeshConnection>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MeshConnection>> GetByGatewayIdAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MeshConnection>> GetByNodeIdAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default);

    Task<MeshConnection?> GetByIdAsync(
        Guid id,
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