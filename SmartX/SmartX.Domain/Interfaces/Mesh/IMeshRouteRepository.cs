using SmartX.Domain.Entities.Mesh;

namespace SmartX.Domain.Interfaces.Mesh;

public interface IMeshRouteRepository
{
    Task<IReadOnlyList<MeshRoute>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MeshRoute>> GetByNodeAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        MeshRoute route,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MeshRoute>> GetByGatewayIdAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default);
}