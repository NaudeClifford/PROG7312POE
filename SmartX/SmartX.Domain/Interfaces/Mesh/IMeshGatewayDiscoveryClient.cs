using SmartX.Domain.Entities.Mesh;

namespace SmartX.Domain.Interfaces.Mesh;

public interface IMeshGatewayDiscoveryClient
{
    Task<MeshDiscoveryResult> DiscoverAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default);
}