using SmartX.Domain.Entities.Mesh;

namespace SmartX.Application.Interfaces.Mesh;

public interface IMeshDiscoveryProvider
{
    Task<MeshDiscoveryResult> DiscoverAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default);
}