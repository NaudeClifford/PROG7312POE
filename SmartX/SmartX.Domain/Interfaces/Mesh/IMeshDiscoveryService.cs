using SmartX.Domain.Entities.Mesh;

namespace SmartX.Application.Interfaces.Mesh;

public interface IMeshDiscoveryService
{
    Task<MeshDiscoveryResult> DiscoverAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default);

    Task<int> SynchronizeAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default);
}