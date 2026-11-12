
using SmartX.Application.Services.Mesh.Graph;
using SmartX.Application.Services.Mesh.Routing;
using SmartX.Application.Services.Mesh.SpanningTree;
using SmartX.Application.Services.Mesh.Traversal;
using SmartX.Domain.Entities.Mesh;
using SmartX.Domain.Enums.Mesh;
using SmartX.Domain.Interfaces.Mesh;

namespace SmartX.Application.Services.Mesh;

public class MeshNetworkService
{
    private readonly IMeshNodeRepository _nodeRepository;
    private readonly IMeshLinkRepository _linkRepository;
    private readonly IMeshRouteRepository _routeRepository;

    private readonly MeshGraphBuilder _graphBuilder;
    private readonly MeshRoutingService _routingService;
    private readonly MeshTraversalService _traversalService;
    private readonly MinimumSpanningTreeService _spanningTreeService;

    public MeshNetworkService(
        IMeshNodeRepository nodeRepository,
        IMeshLinkRepository linkRepository,
        IMeshRouteRepository routeRepository,
        MeshGraphBuilder graphBuilder,
        MeshRoutingService routingService,
        MeshTraversalService traversalService,
        MinimumSpanningTreeService spanningTreeService)
    {
        _nodeRepository = nodeRepository;
        _linkRepository = linkRepository;
        _routeRepository = routeRepository;
        _graphBuilder = graphBuilder;
        _routingService = routingService;
        _traversalService = traversalService;
        _spanningTreeService = spanningTreeService;
    }

    public async Task<MeshGraph> BuildGraphAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        if (gatewayId == Guid.Empty)
            throw new ArgumentException("Gateway ID is required.");

        var nodes = await _nodeRepository.GetByGatewayIdAsync(
            gatewayId, cancellationToken);

        var links = await _linkRepository.GetByGatewayIdAsync(
            gatewayId, cancellationToken);

        return _graphBuilder.Build(nodes, links);
    }

    public async Task<MeshRouteResult> CalculateRouteAsync(
        Guid gatewayId,
        Guid sourceNodeId,
        Guid destinationNodeId,
        RoutingAlgorithm algorithm,
        CancellationToken cancellationToken = default)
    {
        var graph = await BuildGraphAsync(
            gatewayId, cancellationToken);

        MeshRouteResult result;

        switch (algorithm)
        {
            case RoutingAlgorithm.Dijkstra:
                result = _routingService.FindShortestPath(
                    graph, sourceNodeId, destinationNodeId);
                break;

            case RoutingAlgorithm.BreadthFirstSearch:
                result = CalculateBfsRoute(
                    graph, sourceNodeId, destinationNodeId);
                break;

            case RoutingAlgorithm.DepthFirstSearch:
                result = CalculateDfsRoute(
                    graph, sourceNodeId, destinationNodeId);
                break;

            default:
                throw new NotSupportedException(
                    $"Routing algorithm {algorithm} is not supported " +
                    "for source-to-destination route calculation.");
        }

        if (!result.Success)
            return result;

        var route = new MeshRoute
        {
            Id = Guid.NewGuid(),
            GatewayId = gatewayId,
            SourceNodeId = sourceNodeId,
            DestinationNodeId = destinationNodeId,
            NodePath = result.NodePath,
            Algorithm = algorithm,
            TotalCost = result.TotalCost,
            TotalLatency = result.TotalLatency,
            HopCount = result.HopCount,
            IsSuccessful = true,
            CalculatedAt = DateTime.UtcNow
        };

        await _routeRepository.AddAsync(
            route, cancellationToken);

        return result;
    }

    public async Task<List<Guid>> TraverseAsync(
        Guid gatewayId,
        Guid startNodeId,
        RoutingAlgorithm algorithm,
        CancellationToken cancellationToken = default)
    {
        var graph = await BuildGraphAsync(
            gatewayId, cancellationToken);

        return algorithm switch
        {
            RoutingAlgorithm.BreadthFirstSearch =>
                _traversalService.BreadthFirstSearch(
                    graph, startNodeId),

            RoutingAlgorithm.DepthFirstSearch =>
                _traversalService.DepthFirstSearch(
                    graph, startNodeId),

            _ => throw new NotSupportedException(
                "Only BFS and DFS are supported for traversal.")
        };
    }

    public async Task<List<MeshConnectionResult>> CalculateSpanningTreeAsync(
        Guid gatewayId,
        Guid startNodeId,
        CancellationToken cancellationToken = default)
    {
        var graph = await BuildGraphAsync(
            gatewayId, cancellationToken);

        return _spanningTreeService.Calculate(
            graph, startNodeId);
    }

    private MeshRouteResult CalculateBfsRoute(
        MeshGraph graph,
        Guid sourceId,
        Guid destinationId)
    {
        var path = _traversalService.BreadthFirstSearch(
            graph, sourceId);

        return BuildTraversalResult(
            graph, sourceId, destinationId, path);
    }

    private MeshRouteResult CalculateDfsRoute(
        MeshGraph graph,
        Guid sourceId,
        Guid destinationId)
    {
        var path = _traversalService.DepthFirstSearch(
            graph, sourceId);

        return BuildTraversalResult(
            graph, sourceId, destinationId, path);
    }

    private static MeshRouteResult BuildTraversalResult(
        MeshGraph graph,
        Guid sourceId,
        Guid destinationId,
        List<Guid> traversal)
    {
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

        var destinationIndex = traversal.IndexOf(destinationId);

        if (destinationIndex < 0)
        {
            result.Message = "No available route to destination.";
            return result;
        }

        var path = traversal
            .Take(destinationIndex + 1)
            .ToList();

        double cost = 0;
        double latency = 0;
        double energy = 0;

        for (int i = 0; i < path.Count - 1; i++)
        {
            var current = graph.GetNode(path[i])!;

            var edge = current.Connections.FirstOrDefault(
                x => x.DestinationNodeId == path[i + 1] &&
                     x.IsActive);

            if (edge is null)
            {
                result.Message = "Route contains an unavailable link.";
                return result;
            }

            cost += edge.RoutingCost;
            latency += edge.Latency;
            energy += edge.EnergyCost;
        }

        result.Success = true;
        result.NodePath = path;
        result.TotalCost = cost;
        result.TotalLatency = latency;
        result.TotalEnergyCost = energy;
        result.HopCount = path.Count - 1;
        result.Message = "Route successfully calculated.";

        return result;
    }
}
