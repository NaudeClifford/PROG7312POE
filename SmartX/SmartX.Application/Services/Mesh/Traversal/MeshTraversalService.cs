using SmartX.Application.Services.Mesh.Graph;

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
}