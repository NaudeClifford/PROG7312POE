
using SmartX.Domain.Entities.Mesh;

namespace SmartX.Application.Interfaces.Mesh;

public interface IMeshConnectionDiscoveryService
{
    Task<IReadOnlyList<MeshLink>> DiscoverConnectionsAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default);
}
