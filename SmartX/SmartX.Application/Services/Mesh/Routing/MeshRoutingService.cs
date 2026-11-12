using SmartX.Application.Services.Mesh.Graph;

namespace SmartX.Application.Services.Mesh.Routing;

public class MeshRoutingService
{
    public MeshRouteResult FindShortestPath(
        MeshGraph graph,
        Guid sourceId,
        Guid destinationId)
    {
        ArgumentNullException.ThrowIfNull(graph);

        var result = new MeshRouteResult
        {
            SourceNodeId = sourceId,
            DestinationNodeId = destinationId
        };

        var source = graph.GetNode(sourceId);
        var destination = graph.GetNode(destinationId);

        if (source is null || destination is null)
        {
            result.Message = "Source or destination node not found.";
            return result;
        }

        if (!source.IsOnline || !destination.IsOnline)
        {
            result.Message = "Source or destination is offline.";
            return result;
        }

        var distances = new Dictionary<Guid, double>
        {
            [sourceId] = 0
        };

        var predecessors = new Dictionary<Guid, Guid>();

        var latency = new Dictionary<Guid, double>
        {
            [sourceId] = 0
        };

        var energy = new Dictionary<Guid, double>
        {
            [sourceId] = 0
        };

        var visited = new HashSet<Guid>();

        var queue = new PriorityQueue<Guid, double>();

        queue.Enqueue(sourceId, 0);

        while (queue.TryDequeue(
                   out var currentId,
                   out var currentCost))
        {
            if (visited.Contains(currentId))
                continue;

            if (currentCost > distances[currentId])
                continue;

            visited.Add(currentId);

            if (currentId == destinationId)
                break;

            foreach (var edge in graph.GetActiveConnections(currentId))
            {
                var neighbourId = edge.DestinationNodeId;

                if (visited.Contains(neighbourId))
                    continue;

                double newCost =
                    distances[currentId] + edge.RoutingCost;

                if (!distances.TryGetValue(
                        neighbourId,
                        out var existingCost) ||
                    newCost < existingCost)
                {
                    distances[neighbourId] = newCost;

                    predecessors[neighbourId] = currentId;

                    latency[neighbourId] =
                        latency[currentId] + edge.Latency;

                    energy[neighbourId] =
                        energy[currentId] + edge.EnergyCost;

                    queue.Enqueue(neighbourId, newCost);
                }
            }
        }

        if (!distances.ContainsKey(destinationId))
        {
            result.Message =
                "No available route to the destination.";

            return result;
        }

        var path = new List<Guid>();

        var node = destinationId;

        path.Add(node);

        while (node != sourceId)
        {
            node = predecessors[node];
            path.Add(node);
        }

        path.Reverse();

        result.Success = true;
        result.NodePath = path;
        result.TotalCost = distances[destinationId];
        result.TotalLatency = latency[destinationId];
        result.TotalEnergyCost = energy[destinationId];
        result.HopCount = path.Count - 1;
        result.Message = "Route successfully calculated.";

        return result;
    }
}