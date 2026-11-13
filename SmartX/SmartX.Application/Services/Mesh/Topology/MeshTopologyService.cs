using SmartX.Application.Interfaces.Mesh;
using SmartX.Domain.Entities.Mesh;
using SmartX.Domain.Enums.Mesh;
using SmartX.Domain.Interfaces.Mesh;

namespace SmartX.Application.Services.Mesh.Topology;

public class MeshTopologyService(
    IMeshNodeRepository nodeRepository,
    IMeshLinkRepository linkRepository,
    IMeshLogRepository logRepository) : IMeshTopologyService
{
    private readonly IMeshNodeRepository _nodeRepository =
        nodeRepository;

    private readonly IMeshLinkRepository _linkRepository =
        linkRepository;

    private readonly IMeshLogRepository _logRepository =
        logRepository;

    public async Task RegisterNodeAsync(
        MeshNode node,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(node);

        ValidateGateway(node.GatewayId);

        if (node.Id == Guid.Empty)
            node.Id = Guid.NewGuid();

        node.CreatedAt = DateTime.UtcNow;
        node.UpdatedAt = DateTime.UtcNow;

        await _nodeRepository.AddAsync(
            node,
            cancellationToken);

        await LogAsync(
            node.GatewayId,
            node.Id,
            "NodeRegistered",
            $"Node {node.Name} was registered.",
            MeshLogSeverity.Information,
            cancellationToken);
    }

    public async Task UpdateNodeAsync(
        MeshNode node,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(node);

        ValidateGateway(node.GatewayId);

        node.UpdatedAt = DateTime.UtcNow;

        await _nodeRepository.UpdateAsync(
            node,
            cancellationToken);

        await LogAsync(
            node.GatewayId,
            node.Id,
            "NodeUpdated",
            $"Node {node.Name} was updated.",
            MeshLogSeverity.Information,
            cancellationToken);
    }

    public async Task RemoveNodeAsync(
        Guid gatewayId,
        Guid nodeId,
        CancellationToken cancellationToken = default)
    {
        ValidateGateway(gatewayId);

        if (nodeId == Guid.Empty)
            throw new ArgumentException("Invalid node ID.");

        var nodes = await _nodeRepository.GetByGatewayIdAsync(
            gatewayId,
            cancellationToken);

        if (!nodes.Any(x => x.Id == nodeId))
            return;

        var links = await _linkRepository.GetByGatewayIdAsync(
            gatewayId,
            cancellationToken);

        foreach (var link in links.Where(x =>
            x.SourceNodeId == nodeId ||
            x.TargetNodeId == nodeId))
        {
            await _linkRepository.DeleteAsync(
                link.Id,
                cancellationToken);
        }

        await _nodeRepository.DeleteAsync(
            nodeId,
            cancellationToken);

        await LogAsync(
            gatewayId,
            nodeId,
            "NodeRemoved",
            $"Node {nodeId} was removed from the mesh.",
            MeshLogSeverity.Warning,
            cancellationToken);
    }

    public async Task RegisterLinkAsync(
        MeshLink link,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(link);

        ValidateGateway(link.GatewayId);

        if (link.Id == Guid.Empty)
            link.Id = Guid.NewGuid();

        if (link.SourceNodeId == link.TargetNodeId)
            throw new ArgumentException(
                "A node cannot link to itself.");

        var nodes = await _nodeRepository.GetByGatewayIdAsync(
            link.GatewayId,
            cancellationToken);

        var sourceExists = nodes.Any(x =>
            x.Id == link.SourceNodeId);

        var targetExists = nodes.Any(x =>
            x.Id == link.TargetNodeId);

        if (!sourceExists || !targetExists)
            throw new InvalidOperationException(
                "Both link endpoints must exist in the gateway mesh.");

        link.CreatedAt = DateTime.UtcNow;
        link.UpdatedAt = DateTime.UtcNow;
        link.LastSeenAt = DateTime.UtcNow;
        link.IsAvailable = link.SignalStrengthDbm.HasValue
            && link.LatencyMilliseconds.HasValue
            && link.EnergyCost.HasValue;

        await _linkRepository.AddAsync(
            link,
            cancellationToken);

        await LogAsync(
            link.GatewayId,
            link.SourceNodeId,
            "LinkRegistered",
            $"Link between {link.SourceNodeId} and {link.TargetNodeId} was registered.",
            MeshLogSeverity.Information,
            cancellationToken);
    }

    public async Task UpdateLinkAsync(
        MeshLink link,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(link);

        ValidateGateway(link.GatewayId);

        link.UpdatedAt = DateTime.UtcNow;

        await _linkRepository.UpdateAsync(
            link,
            cancellationToken);

        await LogAsync(
            link.GatewayId,
            link.SourceNodeId,
            "LinkUpdated",
            $"Link {link.Id} was updated.",
            MeshLogSeverity.Information,
            cancellationToken);
    }

    public async Task RemoveLinkAsync(
        Guid gatewayId,
        Guid linkId,
        CancellationToken cancellationToken = default)
    {
        ValidateGateway(gatewayId);

        if (linkId == Guid.Empty)
            throw new ArgumentException("Invalid link ID.");

        var links = await _linkRepository.GetByGatewayIdAsync(
            gatewayId,
            cancellationToken);

        var link = links.FirstOrDefault(x =>
            x.Id == linkId);

        if (link is null)
            return;

        await _linkRepository.DeleteAsync(
            linkId,
            cancellationToken);

        await LogAsync(
            gatewayId,
            link.SourceNodeId,
            "LinkRemoved",
            $"Link {linkId} was removed.",
            MeshLogSeverity.Warning,
            cancellationToken);
    }

    public async Task<IReadOnlyList<MeshNode>> GetNodesAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        ValidateGateway(gatewayId);

        return await _nodeRepository.GetByGatewayIdAsync(
            gatewayId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<MeshLink>> GetLinksAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        ValidateGateway(gatewayId);

        return await _linkRepository.GetByGatewayIdAsync(
            gatewayId,
            cancellationToken);
    }

    private async Task LogAsync(
        Guid gatewayId,
        Guid? nodeId,
        string eventType,
        string message,
        MeshLogSeverity severity,
        CancellationToken cancellationToken)
    {
        await _logRepository.AddAsync(
            new MeshLog
            {
                Id = Guid.NewGuid(),
                GatewayId = gatewayId,
                NodeId = nodeId,
                EventType = eventType,
                Message = message,
                Severity = severity,
                Timestamp = DateTime.UtcNow
            },
            cancellationToken);
    }

    private static void ValidateGateway(Guid gatewayId)
    {
        if (gatewayId == Guid.Empty)
            throw new ArgumentException(
                "A valid gateway ID is required.",
                nameof(gatewayId));
    }
}