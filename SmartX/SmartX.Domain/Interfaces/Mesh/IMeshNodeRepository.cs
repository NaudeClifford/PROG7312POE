using SmartX.Domain.Entities.Mesh;

namespace SmartX.Domain.Interfaces.Mesh;

public interface IMeshNodeRepository
{
    Task<IReadOnlyList<MeshNode>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<MeshNode?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MeshNode>> GetByGatewayIdAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        MeshNode node,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        MeshNode node,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}