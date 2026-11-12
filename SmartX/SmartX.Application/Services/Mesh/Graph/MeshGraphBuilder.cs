
using SmartX.Application.Interfaces.Mesh;
using SmartX.Domain.Entities.Mesh;
using SmartX.Domain.Enums.Mesh;

namespace SmartX.Application.Services.Mesh.Graph;

public class MeshGraphBuilder
{
    private readonly IMeshLinkQualityService _linkQualityService;

    public MeshGraphBuilder(
        IMeshLinkQualityService linkQualityService)
    {
        _linkQualityService = linkQualityService;
    }

    public MeshGraph Build(
        IEnumerable<MeshNode> nodes,
        IEnumerable<MeshLink> links)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(links);

        var graph = new MeshGraph();

        var nodeLookup = nodes
            .Where(x => x.Id != Guid.Empty)
            .GroupBy(x => x.Id)
            .ToDictionary(x => x.Key, x => x.First());

        // Build vertices.
        foreach (var node in nodeLookup.Values)
        {
            graph.AddNode(new MeshGraphNode
            {
                Id = node.Id,
                Name = node.Name,
                DeviceIdentifier = node.DeviceIdentifier,
                IsGateway = node.IsGateway,
                IsOnline = node.Status == MeshNodeStatus.Online
            });
        }

        // Build validated connections.
        foreach (var link in links)
        {
            if (!IsValidLink(link, nodeLookup))
                continue;

            var source = nodeLookup[link.SourceNodeId];
            var destination = nodeLookup[link.TargetNodeId];

            if (source.Status != MeshNodeStatus.Online ||
                destination.Status != MeshNodeStatus.Online)
                continue;

            var quality = _linkQualityService.CalculateLinkQuality(
                link.SignalStrengthDbm,
                link.LatencyMilliseconds,
                link.EnergyCost);

            var latency = link.LatencyMilliseconds!.Value;
            var energy = link.EnergyCost!.Value;

            var routingCost =
                _linkQualityService.CalculateRoutingCost(
                    quality,
                    latency,
                    energy);

            if (!double.IsFinite(routingCost))
                continue;

            graph.AddConnection(
                link.SourceNodeId,
                link.TargetNodeId,
                routingCost,
                latency,
                energy,
                quality);
        }

        return graph;
    }

    private static bool IsValidLink(
        MeshLink link,
        Dictionary<Guid, MeshNode> nodes)
    {
        if (!link.IsAvailable)
            return false;

        if (link.Id == Guid.Empty ||
            link.SourceNodeId == Guid.Empty ||
            link.TargetNodeId == Guid.Empty)
            return false;

        if (link.SourceNodeId == link.TargetNodeId)
            return false;

        if (!nodes.ContainsKey(link.SourceNodeId) ||
            !nodes.ContainsKey(link.TargetNodeId))
            return false;

        if (!link.SignalStrengthDbm.HasValue ||
            !link.LatencyMilliseconds.HasValue ||
            !link.EnergyCost.HasValue)
            return false;

        return
            double.IsFinite(link.SignalStrengthDbm.Value) &&
            double.IsFinite(link.LatencyMilliseconds.Value) &&
            double.IsFinite(link.EnergyCost.Value) &&
            link.LatencyMilliseconds.Value >= 0 &&
            link.EnergyCost.Value >= 0;
    }
}
