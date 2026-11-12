using SmartX.Application.Interfaces.Mesh;
using SmartX.Domain.Entities.Mesh;
using SmartX.Domain.Interfaces.Mesh;

namespace SmartX.Application.Services.Mesh.Discovery;

public class MeshDiscoveryProvider : IMeshDiscoveryProvider
{
    private readonly IMeshGatewayDiscoveryClient _discoveryClient;

    public MeshDiscoveryProvider(
        IMeshGatewayDiscoveryClient discoveryClient)
    {
        _discoveryClient = discoveryClient;
    }

    public async Task<MeshDiscoveryResult> DiscoverAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        if (gatewayId == Guid.Empty)
        {
            throw new ArgumentException(
                "Invalid gateway ID.",
                nameof(gatewayId));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var result = await _discoveryClient.DiscoverAsync(
            gatewayId,
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        return result;
    }
}