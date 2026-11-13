
using SmartX.Application.Interfaces.Mesh;
using SmartX.Domain.Entities.Mesh;
using SmartX.Domain.Interfaces.Mesh;

namespace SmartX.Application.Services.Mesh.Discovery;

public class MeshConnectionDiscoveryService
    : IMeshConnectionDiscoveryService
{
    private readonly IMeshConnectionDiscoveryProvider _discoveryProvider;
    private readonly IMeshNodeRepository _nodeRepository;

    public MeshConnectionDiscoveryService(
        IMeshConnectionDiscoveryProvider discoveryProvider,
        IMeshNodeRepository nodeRepository)
    {
        _discoveryProvider = discoveryProvider;
        _nodeRepository = nodeRepository;
    }

    public async Task<IReadOnlyList<MeshLink>>
        DiscoverConnectionsAsync(
            Guid gatewayId,
            CancellationToken cancellationToken = default)
    {
        if (gatewayId == Guid.Empty)
            throw new ArgumentException(
                "A valid gateway ID is required.",
                nameof(gatewayId));

        var nodes = await _nodeRepository.GetByGatewayIdAsync(
            gatewayId,
            cancellationToken);

        var validNodeIds = nodes
            .Select(x => x.Id)
            .Where(x => x != Guid.Empty)
            .ToHashSet();

        var links = await _discoveryProvider.DiscoverAsync(
            gatewayId,
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        if (links is null)
            throw new InvalidOperationException(
                "Connection discovery returned no result.");

        var discoveredLinks = links.ToList();

        var duplicateConnections = new HashSet<
            (Guid First, Guid Second)>();

        foreach (var link in discoveredLinks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (link is null)
                throw new InvalidOperationException(
                    "Connection discovery returned a null link.");

            if (link.GatewayId != gatewayId)
                throw new InvalidOperationException(
                    "A discovered connection belongs to another gateway.");

            if (link.SourceNodeId == Guid.Empty ||
                link.TargetNodeId == Guid.Empty ||
                link.SourceNodeId == link.TargetNodeId)
                throw new InvalidOperationException(
                    "A discovered connection has invalid endpoints.");

            if (!validNodeIds.Contains(link.SourceNodeId) ||
                !validNodeIds.Contains(link.TargetNodeId))
                throw new InvalidOperationException(
                    "A discovered connection references an unregistered node.");

            if (link.Id == Guid.Empty)
                throw new InvalidOperationException(
                    "A discovered connection has an invalid ID.");

            if (!link.SignalStrengthDbm.HasValue ||
                !link.LatencyMilliseconds.HasValue ||
                !link.EnergyCost.HasValue ||
                !double.IsFinite(link.SignalStrengthDbm.Value) ||
                !double.IsFinite(link.LatencyMilliseconds.Value) ||
                !double.IsFinite(link.EnergyCost.Value) ||
                link.LatencyMilliseconds.Value < 0 ||
                link.EnergyCost.Value < 0)
                throw new InvalidOperationException(
                    "A discovered connection has invalid measurements.");

            var key = link.SourceNodeId.CompareTo(
                link.TargetNodeId) <= 0
                ? (link.SourceNodeId, link.TargetNodeId)
                : (link.TargetNodeId, link.SourceNodeId);

            if (!duplicateConnections.Add(key))
                throw new InvalidOperationException(
                    "Duplicate mesh connections were discovered.");
        }

        return discoveredLinks;
    }
}
