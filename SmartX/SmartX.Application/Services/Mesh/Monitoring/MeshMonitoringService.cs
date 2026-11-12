using SmartX.Application.Interfaces.Mesh;
using SmartX.Application.Services.Mesh.Graph;
using SmartX.Application.Services.Mesh.Routing;
using SmartX.Domain.Entities.Mesh;
using SmartX.Domain.Enums.Mesh;
using SmartX.Domain.Interfaces.Mesh;

namespace SmartX.Application.Services.Mesh.Monitoring;

public class MeshMonitoringService : IMeshMonitoringService
{
    private readonly IMeshNodeRepository _nodeRepository;
    private readonly IMeshLinkRepository _linkRepository;
    private readonly IMeshLogRepository _logRepository;
    private readonly IMeshRouteRepository _routeRepository;

    private readonly MeshGraphBuilder _graphBuilder;
    private readonly MeshRoutingService _routingService;

    public MeshMonitoringService(
        IMeshNodeRepository nodeRepository,
        IMeshLinkRepository linkRepository,
        IMeshLogRepository logRepository,
        IMeshRouteRepository routeRepository,
        MeshGraphBuilder graphBuilder,
        MeshRoutingService routingService)
    {
        _nodeRepository = nodeRepository;
        _linkRepository = linkRepository;
        _logRepository = logRepository;
        _routeRepository = routeRepository;
        _graphBuilder = graphBuilder;
        _routingService = routingService;
    }

    public async Task RefreshAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        ValidateGateway(gatewayId);

        await CheckNodeStatusAsync(
            gatewayId,
            cancellationToken);

        await RecalculateRoutesAsync(
            gatewayId,
            cancellationToken);
    }

    public async Task CheckNodeStatusAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        ValidateGateway(gatewayId);

        var nodes = await _nodeRepository.GetByGatewayIdAsync(
            gatewayId,
            cancellationToken);

        var links = await _linkRepository.GetByGatewayIdAsync(
            gatewayId,
            cancellationToken);

        var activeNodeIds = links
            .Where(x => x.IsAvailable)
            .SelectMany(x => new[]
            {
                x.SourceNodeId,
                x.TargetNodeId
            })
            .ToHashSet();

        foreach (var node in nodes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var previousStatus = node.Status;

            var isGateway = node.IsGateway;

            var hasRecentActivity =
                node.LastSeen.HasValue &&
                DateTime.UtcNow - node.LastSeen.Value <=
                TimeSpan.FromMinutes(2);

            if (isGateway)
            {
                node.Status = hasRecentActivity
                    ? MeshNodeStatus.Online
                    : MeshNodeStatus.Offline;
            }
            else if (!hasRecentActivity)
            {
                node.Status = activeNodeIds.Contains(node.Id)
                    ? MeshNodeStatus.Degraded
                    : MeshNodeStatus.Offline;
            }
            else
            {
                node.Status = MeshNodeStatus.Online;
            }

            if (previousStatus != node.Status)
            {
                node.UpdatedAt = DateTime.UtcNow;

                await _nodeRepository.UpdateAsync(
                    node,
                    cancellationToken);

                await _logRepository.AddAsync(
                    new MeshLog
                    {
                        Id = Guid.NewGuid(),
                        GatewayId = gatewayId,
                        NodeId = node.Id,
                        EventType = "NodeStatusChanged",
                        Severity = node.Status ==
                            MeshNodeStatus.Offline
                            ? MeshLogSeverity.Warning
                            : MeshLogSeverity.Information,
                        Message =
                            $"Node {node.Name} changed from " +
                            $"{previousStatus} to {node.Status}.",
                        Timestamp = DateTime.UtcNow
                    },
                    cancellationToken);
            }
        }
    }

    public async Task RecalculateRoutesAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        ValidateGateway(gatewayId);

        var nodes = await _nodeRepository.GetByGatewayIdAsync(
            gatewayId,
            cancellationToken);

        var links = await _linkRepository.GetByGatewayIdAsync(
            gatewayId,
            cancellationToken);

        var graph = _graphBuilder.Build(nodes, links);

        var gateway = nodes.FirstOrDefault(x => x.IsGateway);

        if (gateway is null)
            return;

        foreach (var destination in nodes.Where(x =>
            x.Id != gateway.Id &&
            x.Status == MeshNodeStatus.Online))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = _routingService.FindShortestPath(
                graph,
                gateway.Id,
                destination.Id);

            var route = new MeshRoute
            {
                Id = Guid.NewGuid(),
                GatewayId = gatewayId,
                SourceNodeId = gateway.Id,
                DestinationNodeId = destination.Id,
                NodePath = result.NodePath,
                Algorithm = RoutingAlgorithm.Dijkstra,
                TotalCost = result.TotalCost,
                TotalLatency = result.TotalLatency,
                HopCount = result.HopCount,
                IsSuccessful = result.Success,
                CalculatedAt = DateTime.UtcNow
            };

            await _routeRepository.AddAsync(
                route,
                cancellationToken);
        }
    }

    private static void ValidateGateway(Guid gatewayId)
    {
        if (gatewayId == Guid.Empty)
            throw new ArgumentException(
                "A valid gateway ID is required.",
                nameof(gatewayId));
    }
}