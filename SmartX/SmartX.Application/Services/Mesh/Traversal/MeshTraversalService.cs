using SmartX.Application.Services.Mesh.Graph;
using SmartX.Application.Services.Mesh.Routing;

namespace SmartX.Application.Services.Mesh.Traversal;

public class MeshTraversalService
{
    public List<Guid> BreadthFirstSearch(
        MeshGraph graph,
        Guid startNodeId)
    {
        ArgumentNullException.ThrowIfNull(graph);

        if (graph.GetNode(startNodeId) is not { IsOnline: true })
            return [];

        var visited = new HashSet<Guid>();

        var result = new List<Guid>();

        var queue = new Queue<Guid>();

        queue.Enqueue(startNodeId);
        visited.Add(startNodeId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            result.Add(current);

            foreach (var edge in graph.GetActiveConnections(current))
            {
                if (visited.Add(edge.DestinationNodeId))
                {
                    queue.Enqueue(edge.DestinationNodeId);
                }
            }
        }

        return result;
    }

    public List<Guid> DepthFirstSearch(
        MeshGraph graph,
        Guid startNodeId)
    {
        ArgumentNullException.ThrowIfNull(graph);

        if (graph.GetNode(startNodeId) is not { IsOnline: true })
            return [];

        var visited = new HashSet<Guid>();

        var result = new List<Guid>();

        var stack = new Stack<Guid>();

        stack.Push(startNodeId);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            if (!visited.Add(current))
                continue;

            result.Add(current);

            foreach (var edge in graph
                .GetActiveConnections(current)
                .Reverse())
            {
                if (!visited.Contains(edge.DestinationNodeId))
                {
                    stack.Push(edge.DestinationNodeId);
                }
            }
        }

        return result;
    }


    public MeshRouteResult FindBfsRoute(
        MeshGraph graph,
        Guid sourceId,
        Guid destinationId)
    {
        return FindTraversalRoute(
            graph,
            sourceId,
            destinationId,
            breadthFirst: true);
    }

    public MeshRouteResult FindDfsRoute(
        MeshGraph graph,
        Guid sourceId,
        Guid destinationId)
    {
        return FindTraversalRoute(
            graph,
            sourceId,
            destinationId,
            breadthFirst: false);
    }

    private static MeshRouteResult FindTraversalRoute(
        MeshGraph graph,
        Guid sourceId,
        Guid destinationId,
        bool breadthFirst)
    {
        ArgumentNullException.ThrowIfNull(graph);

        var result = new MeshRouteResult
        {
            SourceNodeId = sourceId,
            DestinationNodeId = destinationId
        };

        if (graph.GetNode(sourceId) is not { IsOnline: true } ||
            graph.GetNode(destinationId) is not { IsOnline: true })
        {
            result.Message = "Source or destination is offline.";
            return result;
        }

        var visited = new HashSet<Guid> { sourceId };

        var predecessors = new Dictionary<Guid, Guid>();

        var queue = new Queue<Guid>();
        var stack = new Stack<Guid>();

        if (breadthFirst)
            queue.Enqueue(sourceId);
        else
            stack.Push(sourceId);

        bool found = false;

        while (breadthFirst ? queue.Count > 0 : stack.Count > 0)
        {
            var currentId = breadthFirst
                ? queue.Dequeue()
                : stack.Pop();

            if (currentId == destinationId)
            {
                found = true;
                break;
            }

            var edges = graph
                .GetActiveConnections(currentId);

            foreach (var edge in edges)
            {
                var neighbour = edge.DestinationNodeId;

                if (!visited.Add(neighbour))
                    continue;

                predecessors[neighbour] = currentId;

                if (breadthFirst)
                    queue.Enqueue(neighbour);
                else
                    stack.Push(neighbour);
            }
        }

        if (!found)
        {
            result.Message = "No available route to destination.";
            return result;
        }

        var path = new List<Guid> { destinationId };

        var node = destinationId;

        while (node != sourceId)
        {
            node = predecessors[node];
            path.Add(node);
        }

        path.Reverse();

        result.NodePath = path;
        result.Success = true;
        result.HopCount = path.Count - 1;

        // Calculate actual route metrics.
        for (int i = 0; i < path.Count - 1; i++)
        {
            var edge = graph
                .GetActiveConnections(path[i])
                .First(x => x.DestinationNodeId == path[i + 1]);

            result.TotalCost += edge.RoutingCost;
            result.TotalLatency += edge.Latency;
            result.TotalEnergyCost += edge.EnergyCost;
        }

        result.Message = "Route successfully calculated.";

        return result;
    }

}