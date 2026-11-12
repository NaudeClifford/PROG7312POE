
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
        ValidateIds(gatewayId, sourceNodeId, destinationNodeId);

        var graph = await BuildGraphAsync(
            gatewayId, cancellationToken);

        MeshRouteResult result = algorithm switch
        {
            RoutingAlgorithm.Dijkstra =>
                _routingService.FindShortestPath(
                    graph, sourceNodeId, destinationNodeId),

            RoutingAlgorithm.BreadthFirstSearch =>
                _traversalService.FindBfsRoute(
                    graph, sourceNodeId, destinationNodeId),

            RoutingAlgorithm.DepthFirstSearch =>
                _traversalService.FindDfsRoute(
                    graph, sourceNodeId, destinationNodeId),

            _ => throw new NotSupportedException(
                $"Routing algorithm {algorithm} is not supported.")
        };

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
        if (gatewayId == Guid.Empty || startNodeId == Guid.Empty)
            throw new ArgumentException("Valid gateway and node IDs are required.");

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
        if (gatewayId == Guid.Empty || startNodeId == Guid.Empty)
            throw new ArgumentException("Valid gateway and node IDs are required.");

        var graph = await BuildGraphAsync(
            gatewayId, cancellationToken);

        return _spanningTreeService.Calculate(
            graph, startNodeId);
    }

    private static void ValidateIds(
    Guid gatewayId,
    Guid sourceNodeId,
    Guid destinationNodeId)
    {
        if (gatewayId == Guid.Empty)
            throw new ArgumentException("Gateway ID is required.");

        if (sourceNodeId == Guid.Empty ||
            destinationNodeId == Guid.Empty)
            throw new ArgumentException("Valid node IDs are required.");

        if (sourceNodeId == destinationNodeId)
            throw new ArgumentException(
                "Source and destination must be different.");
    }
}
