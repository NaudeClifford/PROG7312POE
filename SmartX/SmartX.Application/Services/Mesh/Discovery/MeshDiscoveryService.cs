using SmartX.Application.Interfaces.Mesh;
using SmartX.Domain.Entities.Mesh;
using SmartX.Domain.Enums.Mesh;
using SmartX.Domain.Interfaces.Mesh;

namespace SmartX.Application.Services.Mesh.Discovery;

public class MeshDiscoveryService(
    IMeshDiscoveryProvider discoveryProvider,
    IMeshTopologyService topologyService,
    IMeshNodeRepository nodeRepository,
    IMeshLinkRepository linkRepository,
    IMeshLogRepository logRepository)
    : IMeshDiscoveryService
{
    private readonly IMeshDiscoveryProvider _discoveryProvider =
        discoveryProvider;

    private readonly IMeshTopologyService _topologyService =
        topologyService;

    private readonly IMeshNodeRepository _nodeRepository =
        nodeRepository;

    private readonly IMeshLinkRepository _linkRepository =
        linkRepository;

    private readonly IMeshLogRepository _logRepository =
        logRepository;

    public async Task<MeshDiscoveryResult> DiscoverAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        ValidateGateway(gatewayId);

        var result = await _discoveryProvider.DiscoverAsync(
            gatewayId,
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        // Ensure all discovered devices belong to this gateway.
        foreach (var node in result.DiscoveredNodes)
        {
            if (node.GatewayId != gatewayId)
            {
                throw new InvalidOperationException(
                    "A discovered node belongs to another gateway.");
            }
        }

        foreach (var link in result.DiscoveredLinks)
        {
            if (link.GatewayId != gatewayId)
            {
                throw new InvalidOperationException(
                    "A discovered link belongs to another gateway.");
            }
        }

        return result;
    }

    public async Task<int> SynchronizeAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        ValidateGateway(gatewayId);

        var discovery = await DiscoverAsync(
            gatewayId,
            cancellationToken);

        var existingNodes =
            await _nodeRepository.GetByGatewayIdAsync(
                gatewayId,
                cancellationToken);

        var existingLinks =
            await _linkRepository.GetByGatewayIdAsync(
                gatewayId,
                cancellationToken);

        var nodesByDevice = existingNodes.ToDictionary(
            x => (x.DeviceId, x.NodeType));

        var linksByEndpoints = existingLinks.ToDictionary(
            x => (x.SourceNodeId, x.TargetNodeId));

        int changes = 0;

        // Synchronize discovered nodes.
        foreach (var discovered in discovery.DiscoveredNodes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (nodesByDevice.TryGetValue(
                (discovered.DeviceId, discovered.NodeType),
                out var existing))
            {
                discovered.Id = existing.Id;
                discovered.CreatedAt = existing.CreatedAt;

                await _topologyService.UpdateNodeAsync(
                    discovered,
                    cancellationToken);
            }
            else
            {
                await _topologyService.RegisterNodeAsync(
                    discovered,
                    cancellationToken);
            }

            changes++;
        }

        // Refresh the node list because newly discovered nodes
        // may be required by discovered links.
        var synchronizedNodes =
            await _nodeRepository.GetByGatewayIdAsync(
                gatewayId,
                cancellationToken);

        var validNodeIds = synchronizedNodes
            .Select(x => x.Id)
            .ToHashSet();

        // Synchronize discovered links.
        foreach (var discovered in discovery.DiscoveredLinks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!validNodeIds.Contains(discovered.SourceNodeId) ||
                !validNodeIds.Contains(discovered.TargetNodeId))
            {
                continue;
            }

            if (linksByEndpoints.TryGetValue(
                (discovered.SourceNodeId, discovered.TargetNodeId),
                out var existing))
            {
                discovered.Id = existing.Id;
                discovered.CreatedAt = existing.CreatedAt;

                await _topologyService.UpdateLinkAsync(
                    discovered,
                    cancellationToken);
            }
            else
            {
                await _topologyService.RegisterLinkAsync(
                    discovered,
                    cancellationToken);
            }

            changes++;
        }

        await _logRepository.AddAsync(
            new MeshLog
            {
                Id = Guid.NewGuid(),
                GatewayId = gatewayId,
                EventType = "MeshDiscoveryCompleted",
                Message =
                    $"Mesh discovery completed. " +
                    $"Nodes discovered: {discovery.DiscoveredNodes.Count}. " +
                    $"Links discovered: {discovery.DiscoveredLinks.Count}.",
                Severity = MeshLogSeverity.Information,
                Timestamp = DateTime.UtcNow
            },
            cancellationToken);

        return changes;
    }

    private static void ValidateGateway(Guid gatewayId)
    {
        if (gatewayId == Guid.Empty)
        {
            throw new ArgumentException(
                "A valid gateway ID is required.",
                nameof(gatewayId));
        }
    }
}