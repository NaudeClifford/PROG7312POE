
using SmartX.Application.Services.Mesh.Graph;

namespace SmartX.Application.Services.Mesh.SpanningTree;

public class MinimumSpanningTreeService
{
    public List<MeshConnectionResult> Calculate(
        MeshGraph graph,
        Guid startNodeId)
    {
        ArgumentNullException.ThrowIfNull(graph);

        if (startNodeId == Guid.Empty)
            throw new ArgumentException(
                "A valid starting node ID is required.",
                nameof(startNodeId));

        if (graph.GetNode(startNodeId) is not { IsOnline: true })
            return [];

        var visited = new HashSet<Guid>
        {
            startNodeId
        };

        var result = new List<MeshConnectionResult>();

        var queue = new PriorityQueue<
            MeshConnectionResult,
            double>();

        AddEdges(startNodeId);

        while (queue.TryDequeue(out var connection, out _))
        {
            if (!visited.Add(connection.DestinationNodeId))
                continue;

            result.Add(connection);

            AddEdges(connection.DestinationNodeId);
        }

        return result;

        void AddEdges(Guid currentId)
        {
            foreach (var edge in graph.GetActiveConnections(currentId))
            {
                if (visited.Contains(edge.DestinationNodeId))
                    continue;

                queue.Enqueue(
                    new MeshConnectionResult
                    {
                        SourceNodeId = currentId,
                        DestinationNodeId = edge.DestinationNodeId,
                        Cost = edge.RoutingCost,
                        EnergyCost = edge.EnergyCost
                    },
                    edge.RoutingCost);
            }
        }
    }
}
