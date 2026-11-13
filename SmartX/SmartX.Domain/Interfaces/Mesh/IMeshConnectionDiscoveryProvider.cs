using SmartX.Domain.Entities.Mesh;

namespace SmartX.Domain.Interfaces.Mesh;

public interface IMeshConnectionDiscoveryProvider
{
    Task<IReadOnlyList<MeshLink>> DiscoverAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default);
}