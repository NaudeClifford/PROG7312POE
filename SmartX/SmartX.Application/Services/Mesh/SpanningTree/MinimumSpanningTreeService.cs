using SmartX.Application.Services.Mesh.Graph;

namespace SmartX.Application.Services.Mesh.SpanningTree;

public class MinimumSpanningTreeService
{
    public List<MeshConnectionResult> Calculate(
        MeshGraph graph,
        Guid startNodeId)
    {
        ArgumentNullException.ThrowIfNull(graph);

        if (graph.GetNode(startNodeId) is not { IsOnline: true })
            return [];

        var visited = new HashSet<Guid>();

        var result = new List<MeshConnectionResult>();

        var queue = new PriorityQueue<
            MeshConnectionResult,
            double>();

        visited.Add(startNodeId);

        AddEdges(startNodeId);

        while (queue.TryDequeue(
                   out var connection,
                   out _))
        {
            if (visited.Contains(connection.DestinationNodeId))
                continue;

            visited.Add(connection.DestinationNodeId);

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