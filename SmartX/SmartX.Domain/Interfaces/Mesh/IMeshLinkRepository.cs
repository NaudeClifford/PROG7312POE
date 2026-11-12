using SmartX.Domain.Entities.Mesh;

namespace SmartX.Domain.Interfaces.Mesh;

public interface IMeshLinkRepository
{
    Task<IReadOnlyList<MeshLink>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<MeshLink?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MeshLink>> GetByGatewayIdAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        MeshLink link,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        MeshLink link,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}