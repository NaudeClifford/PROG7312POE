using SmartX.Domain.Entities.Mesh;
using SmartX.Domain.Interfaces.Mesh;

namespace SmartX.Application.Services.Mesh.Discovery;

public class MeshGatewayDiscoveryClient : IMeshGatewayDiscoveryClient
{
    private readonly IMeshNodeRepository _nodeRepository;
    private readonly IMeshLinkRepository _linkRepository;

    public MeshGatewayDiscoveryClient(
        IMeshNodeRepository nodeRepository,
        IMeshLinkRepository linkRepository)
    {
        _nodeRepository = nodeRepository;
        _linkRepository = linkRepository;
    }

    public async Task<MeshDiscoveryResult> DiscoverAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        var nodes = await _nodeRepository.GetByGatewayIdAsync(
            gatewayId,
            cancellationToken);

        var links = await _linkRepository.GetByGatewayIdAsync(
            gatewayId,
            cancellationToken);

        return new MeshDiscoveryResult
        {
            DiscoveredNodes = nodes.ToList(),
            DiscoveredLinks = links.ToList()
        };
    }
}