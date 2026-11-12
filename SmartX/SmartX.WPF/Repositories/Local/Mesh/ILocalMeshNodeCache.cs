using SmartX.Domain.Entities.Mesh;

namespace SmartX.WPF.Repositories.Local;

public interface ILocalMeshNodeCache
{
    Task<MeshNode?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MeshNode>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MeshNode>> GetByGatewayIdAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        MeshNode node,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}