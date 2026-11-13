namespace SmartX.Application.Services.Mesh.Graph;

public class MeshGraph
{
    private readonly Dictionary<Guid, MeshGraphNode> _nodes = [];

    public int NodeCount => _nodes.Count;

    public int EdgeCount =>
        _nodes.Values.Sum(x => x.Connections.Count) / 2;

    public IReadOnlyCollection<MeshGraphNode> Nodes =>
        _nodes.Values;

    public bool ContainsNode(Guid nodeId)
    {
        return _nodes.ContainsKey(nodeId);
    }

    public MeshGraphNode? GetNode(Guid nodeId)
    {
        return _nodes.GetValueOrDefault(nodeId);
    }

    public void AddNode(MeshGraphNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (node.Id == Guid.Empty)
            throw new ArgumentException("Node ID cannot be empty.");

        _nodes[node.Id] = node;
    }

    public void AddConnection(
        Guid sourceId,
        Guid destinationId,
        double routingCost,
        double latency,
        double energyCost,
        double linkQuality)
    {
        if (sourceId == destinationId)
            throw new ArgumentException(
                "A node cannot connect to itself.");

        if (!_nodes.TryGetValue(sourceId, out var source))
            throw new InvalidOperationException(
                "Source node does not exist.");

        if (!_nodes.TryGetValue(destinationId, out var destination))
            throw new InvalidOperationException(
                "Destination node does not exist.");

        if (!double.IsFinite(routingCost) || routingCost < 0 ||
            !double.IsFinite(latency) || latency < 0 ||
            !double.IsFinite(energyCost) || energyCost < 0 ||
            !double.IsFinite(linkQuality) ||
            linkQuality < 0 || linkQuality > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(routingCost),
                "Invalid connection metrics.");
        }

        // Add both directions.

        UpsertEdge(
            source,
            destinationId,
            routingCost,
            latency,
            energyCost,
            linkQuality);

        UpsertEdge(
            destination,
            sourceId,
            routingCost,
            latency,
            energyCost,
            linkQuality);
    }

    private static void UpsertEdge(
        MeshGraphNode node,
        Guid destinationId,
        double routingCost,
        double latency,
        double energyCost,
        double linkQuality)
    {
        var existing = node.Connections.FirstOrDefault(
            x => x.DestinationNodeId == destinationId);

        if (existing is null)
        {
            node.Connections.Add(new MeshGraphEdge
            {
                DestinationNodeId = destinationId,
                RoutingCost = routingCost,
                Latency = latency,
                EnergyCost = energyCost,
                LinkQuality = linkQuality,
                IsActive = true
            });

            return;
        }

        existing.RoutingCost = routingCost;
        existing.Latency = latency;
        existing.EnergyCost = energyCost;
        existing.LinkQuality = linkQuality;
        existing.IsActive = true;
    }

    public void RemoveConnection(
        Guid sourceId,
        Guid destinationId)
    {
        if (_nodes.TryGetValue(sourceId, out var source))
        {
            source.Connections.RemoveAll(
                x => x.DestinationNodeId == destinationId);
        }

        if (_nodes.TryGetValue(destinationId, out var destination))
        {
            destination.Connections.RemoveAll(
                x => x.DestinationNodeId == sourceId);
        }
    }

    public void SetNodeStatus(
        Guid nodeId,
        bool isOnline)
    {
        if (!_nodes.TryGetValue(nodeId, out var node))
            throw new InvalidOperationException(
                "Node does not exist.");

        node.IsOnline = isOnline;
    }

    public IEnumerable<MeshGraphEdge> GetActiveConnections(
        Guid nodeId)
    {
        if (!_nodes.TryGetValue(nodeId, out var node))
            return [];

        return node.Connections.Where(x =>
            x.IsActive &&
            _nodes.TryGetValue(
                x.DestinationNodeId,
                out var destination) &&
            destination.IsOnline);
    }

    public void PrintGraph()
    {
        foreach (var node in _nodes.Values)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Node: {node.Name} | " +
                $"ID: {node.Id} | " +
                $"Online: {node.IsOnline}");

            foreach (var connection in node.Connections)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"  -> {connection.DestinationNodeId} | " +
                    $"Active: {connection.IsActive} | " +
                    $"Cost: {connection.RoutingCost}");
            }
        }
    }
}