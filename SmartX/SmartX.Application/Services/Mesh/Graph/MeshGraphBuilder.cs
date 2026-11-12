
using SmartX.Domain.Entities.Mesh;
using SmartX.Domain.Enums.Mesh;

namespace SmartX.Application.Services.Mesh.Graph;

public class MeshGraphBuilder
{
    public MeshGraph Build(
        IEnumerable<MeshNode> nodes,
        IEnumerable<MeshLink> links)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(links);

        var graph = new MeshGraph();

        var nodeList = nodes.ToList();

        // Create graph vertices.
        foreach (var node in nodeList)
        {
            graph.AddNode(new MeshGraphNode
            {
                Id = node.Id,
                Name = node.Name,
                DeviceIdentifier = node.DeviceIdentifier,
                IsOnline = node.Status ==
                    MeshNodeStatus.Online
            });
        }

        var nodeIds = nodeList
            .Select(x => x.Id)
            .ToHashSet();

        // Create graph edges.
        foreach (var link in links)
        {
            if (!link.IsAvailable)
                continue;

            if (!nodeIds.Contains(link.SourceNodeId) ||
                !nodeIds.Contains(link.TargetNodeId))
                continue;

            if (!link.LatencyMilliseconds.HasValue ||
                !link.EnergyCost.HasValue)
                continue;

            var source = nodeList.First(
                x => x.Id == link.SourceNodeId);

            var destination = nodeList.First(
                x => x.Id == link.TargetNodeId);

            // Do not create links to offline devices.
            if (source.Status !=
                    MeshNodeStatus.Online ||
                destination.Status !=
                    MeshNodeStatus.Online)
                continue;

            var latency = link.LatencyMilliseconds.Value;
            var energy = link.EnergyCost.Value;

            var signalQuality = CalculateSignalQuality(
                link.SignalStrengthDbm);

            var routingCost = CalculateRoutingCost(
                latency,
                energy,
                signalQuality);

            graph.AddConnection(
                link.SourceNodeId,
                link.TargetNodeId,
                routingCost,
                latency,
                energy,
                signalQuality);
        }

        return graph;
    }

    private static double CalculateSignalQuality(
        double? signalStrengthDbm)
    {
        if (!signalStrengthDbm.HasValue)
            return 0;

        // Convert approximate Wi-Fi dBm values
        // into a 0-100 quality score.
        return Math.Clamp(
            (signalStrengthDbm.Value + 100) * 2,
            0,
            100);
    }

    private static double CalculateRoutingCost(
        double latency,
        double energy,
        double signalQuality)
    {
        return
            (latency * 0.4) +
            (energy * 0.4) +
            ((100 - signalQuality) * 0.2);
    }
}
