
using SmartX.Application.Interfaces.Mesh;
using SmartX.Domain.Entities.Mesh;
using SmartX.Domain.Enums.Mesh;
using System.Diagnostics;

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

        var nodeList = nodes.ToList();
        var linkList = links.ToList();

        var graph = new MeshGraph();

        var nodeLookup = nodeList
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

        int rejectedLinks = 0;
        int acceptedLinks = 0;

        // Build connections.
        foreach (var link in linkList)
        {
            if (!IsValidLink(link, nodeLookup))
            {
                rejectedLinks++;
                continue;
            }

            var source = nodeLookup[link.SourceNodeId];
            var destination = nodeLookup[link.TargetNodeId];

            if (source.Status != MeshNodeStatus.Online ||
                destination.Status != MeshNodeStatus.Online)
            {
                rejectedLinks++;
                continue;
            }

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

            if (!double.IsFinite(routingCost) ||
                !double.IsFinite(quality) ||
                quality < 0 ||
                quality > 100)
            {
                rejectedLinks++;
                continue;
            }

            graph.AddConnection(
                link.SourceNodeId,
                link.TargetNodeId,
                routingCost,
                latency,
                energy,
                quality);

            acceptedLinks++;
        }

        System.Diagnostics.Debug.WriteLine(
            $"[MESH GRAPH] Nodes loaded: {graph.NodeCount}");

        System.Diagnostics.Debug.WriteLine(
            $"[MESH GRAPH] Links received: {linkList.Count}");

        System.Diagnostics.Debug.WriteLine(
            $"[MESH GRAPH] Links accepted: {acceptedLinks}");

        System.Diagnostics.Debug.WriteLine(
            $"[MESH GRAPH] Links rejected: {rejectedLinks}");

        System.Diagnostics.Debug.WriteLine(
            $"[MESH GRAPH] Edges created: {graph.EdgeCount}");

        return graph;
    }

    private static bool IsValidLink(
     MeshLink link,
     Dictionary<Guid, MeshNode> nodes)
    {
        if (!link.IsAvailable)
        {
            Debug.WriteLine(
                $"[REJECTED] Link {link.Id}: IsAvailable = false");

            return false;
        }

        if (link.Id == Guid.Empty)
        {
            Debug.WriteLine("[REJECTED] Empty Link ID");
            return false;
        }

        if (link.SourceNodeId == Guid.Empty ||
            link.TargetNodeId == Guid.Empty)
        {
            Debug.WriteLine("[REJECTED] Empty node ID");
            return false;
        }

        if (link.SourceNodeId == link.TargetNodeId)
        {
            Debug.WriteLine("[REJECTED] Self connection");
            return false;
        }

        if (!nodes.ContainsKey(link.SourceNodeId))
        {
            Debug.WriteLine("[REJECTED] Source node missing");
            return false;
        }

        if (!nodes.ContainsKey(link.TargetNodeId))
        {
            Debug.WriteLine("[REJECTED] Destination node missing");
            return false;
        }

        if (!link.SignalStrengthDbm.HasValue ||
            !link.LatencyMilliseconds.HasValue ||
            !link.EnergyCost.HasValue)
        {
            Debug.WriteLine("[REJECTED] Missing connection metrics");
            return false;
        }

        if (!double.IsFinite(link.SignalStrengthDbm.Value) ||
            !double.IsFinite(link.LatencyMilliseconds.Value) ||
            !double.IsFinite(link.EnergyCost.Value))
        {
            Debug.WriteLine("[REJECTED] Invalid numeric metrics");
            return false;
        }

        if (link.LatencyMilliseconds.Value < 0 ||
            link.EnergyCost.Value < 0)
        {
            Debug.WriteLine("[REJECTED] Negative metrics");
            return false;
        }

        return true;
    }
}
