
using Microsoft.AspNetCore.Mvc;
using SmartX.Application.Interfaces.Mesh;
using SmartX.Application.Services.Mesh.Graph;
using SmartX.Application.Services.Mesh.Routing;
using SmartX.Application.Services.Mesh.SpanningTree;
using SmartX.Application.Services.Mesh.Traversal;
using SmartX.Domain.Entities.Mesh;
using SmartX.Shared.DTOs.Mesh;
using SmartX.Shared.Mapping;

namespace SmartX.API.Controllers.Mesh;

[ApiController]
[Route("api/mesh")]
public class MeshController : ControllerBase
{
    private readonly IMeshDiscoveryService _discoveryService;
    private readonly IMeshTopologyService _topologyService;
    private readonly IMeshHealthService _healthService;
    private readonly IMeshMonitoringService _monitoringService;
    private readonly IMeshLinkQualityService _linkQualityService;
    private readonly IMeshLogService _logService;

    private readonly MeshGraphBuilder _graphBuilder;
    private readonly MeshRoutingService _routingService;
    private readonly MeshTraversalService _traversalService;
    private readonly MinimumSpanningTreeService _spanningTreeService;

    private readonly IMapper _mapper;

    public MeshController(
        IMeshDiscoveryService discoveryService,
        IMeshTopologyService topologyService,
        IMeshHealthService healthService,
        IMeshMonitoringService monitoringService,
        IMeshLinkQualityService linkQualityService,
        IMeshLogService logService,
        MeshGraphBuilder graphBuilder,
        MeshRoutingService routingService,
        MeshTraversalService traversalService,
        MinimumSpanningTreeService spanningTreeService,
        IMapper mapper)
    {
        _discoveryService = discoveryService;
        _topologyService = topologyService;
        _healthService = healthService;
        _monitoringService = monitoringService;
        _linkQualityService = linkQualityService;
        _logService = logService;

        _graphBuilder = graphBuilder;
        _routingService = routingService;
        _traversalService = traversalService;
        _spanningTreeService = spanningTreeService;

        _mapper = mapper;
    }

    // DISCOVERY

    [HttpPost("discovery/{gatewayId:guid}")]
    public async Task<IActionResult> Discover(
        Guid gatewayId,
        CancellationToken cancellationToken)
    {
        var result = await _discoveryService.DiscoverAsync(
            gatewayId, cancellationToken);

        var dto = _mapper.Map<MeshDiscoveryDto>(result);

        return Ok(dto);
    }

    [HttpPost("discovery/{gatewayId:guid}/synchronize")]
    public async Task<ActionResult<MeshOperationResultDto>> Synchronize(
        Guid gatewayId,
        CancellationToken cancellationToken)
    {
        var changes = await _discoveryService.SynchronizeAsync(
            gatewayId,
            cancellationToken);

        return Ok(new MeshOperationResultDto
        {
            Success = true,
            GatewayId = gatewayId,
            Message = "Mesh synchronization completed.",
            Changes = changes,
            Timestamp = DateTime.UtcNow
        });
    }

    // TOPOLOGY

    [HttpGet("{gatewayId:guid}/topology")]
    public async Task<IActionResult> GetTopology(
        Guid gatewayId,
        CancellationToken cancellationToken)
    {
        var nodes = await _topologyService.GetNodesAsync(
            gatewayId, cancellationToken);

        var links = await _topologyService.GetLinksAsync(
            gatewayId, cancellationToken);

        var dto = new MeshTopologyDto
        {
            GatewayId = gatewayId,
            Nodes = _mapper.Map<List<MeshNodeDto>>(nodes),
            Links = _mapper.Map<List<MeshLinkDto>>(links)
        };

        return Ok(dto);
    }

    [HttpPost("nodes")]
    public async Task<IActionResult> RegisterNode(
        [FromBody] MeshNodeDto request,
        CancellationToken cancellationToken)
    {
        var node = _mapper.Map<MeshNode>(request);

        await _topologyService.RegisterNodeAsync(
            node, cancellationToken);

        var dto = _mapper.Map<MeshNodeDto>(node);

        return CreatedAtAction(
            nameof(GetTopology),
            new { gatewayId = node.GatewayId },
            dto);
    }

    [HttpPut("nodes")]
    public async Task<IActionResult> UpdateNode(
        [FromBody] MeshNodeDto request,
        CancellationToken cancellationToken)
    {
        var node = _mapper.Map<MeshNode>(request);

        await _topologyService.UpdateNodeAsync(
            node, cancellationToken);

        return Ok(_mapper.Map<MeshNodeDto>(node));
    }

    [HttpDelete("{gatewayId:guid}/nodes/{nodeId:guid}")]
    public async Task<IActionResult> RemoveNode(
        Guid gatewayId,
        Guid nodeId,
        CancellationToken cancellationToken)
    {
        await _topologyService.RemoveNodeAsync(
            gatewayId, nodeId, cancellationToken);

        return NoContent();
    }

    [HttpPost("links")]
    public async Task<IActionResult> RegisterLink(
        [FromBody] MeshLinkDto request,
        CancellationToken cancellationToken)
    {
        var link = _mapper.Map<MeshLink>(request);

        await _topologyService.RegisterLinkAsync(
            link, cancellationToken);

        var dto = _mapper.Map<MeshLinkDto>(link);

        return Created(
            $"api/mesh/links/{link.Id}",
            dto);
    }

    [HttpPut("links")]
    public async Task<IActionResult> UpdateLink(
        [FromBody] MeshLinkDto request,
        CancellationToken cancellationToken)
    {
        var link = _mapper.Map<MeshLink>(request);

        await _topologyService.UpdateLinkAsync(
            link, cancellationToken);

        return Ok(_mapper.Map<MeshLinkDto>(link));
    }

    [HttpDelete("{gatewayId:guid}/links/{linkId:guid}")]
    public async Task<IActionResult> RemoveLink(
        Guid gatewayId,
        Guid linkId,
        CancellationToken cancellationToken)
    {
        await _topologyService.RemoveLinkAsync(
            gatewayId, linkId, cancellationToken);

        return NoContent();
    }

    // HEALTH

    [HttpGet("{gatewayId:guid}/health")]
    public async Task<ActionResult<MeshHealthReportDto>> GetHealth(
        Guid gatewayId,
        CancellationToken cancellationToken)
    {
        var report = await _healthService.GetHealthAsync(
            gatewayId, cancellationToken);

        return Ok(_mapper.Map<MeshHealthReportDto>(report));
    }

    // MONITORING

    [HttpPost("{gatewayId:guid}/monitoring/refresh")]
    public async Task<IActionResult> RefreshMonitoring(
        Guid gatewayId,
        CancellationToken cancellationToken)
    {
        await _monitoringService.RefreshAsync(
            gatewayId, cancellationToken);

        return Ok(new MeshOperationResultDto
        {
            GatewayId = gatewayId,
            Message = "Mesh monitoring refreshed.",
            Timestamp = DateTime.UtcNow
        });
    }

    [HttpPost("{gatewayId:guid}/monitoring/nodes")]
    public async Task<IActionResult> CheckNodeStatus(
        Guid gatewayId,
        CancellationToken cancellationToken)
    {
        await _monitoringService.CheckNodeStatusAsync(
            gatewayId, cancellationToken);

        return Ok(new MeshOperationResultDto
        {
            GatewayId = gatewayId,
            Message = "Node statuses checked.",
            Timestamp = DateTime.UtcNow
        });
    }

    [HttpPost("{gatewayId:guid}/monitoring/routes")]
    public async Task<IActionResult> RecalculateRoutes(
        Guid gatewayId,
        CancellationToken cancellationToken)
    {
        await _monitoringService.RecalculateRoutesAsync(
            gatewayId, cancellationToken);

        return Ok(new MeshOperationResultDto
        {
            GatewayId = gatewayId,
            Message = "Mesh routes recalculated.",
            Timestamp = DateTime.UtcNow
        });
    }

    // LINK QUALITY

    [HttpPost("{gatewayId:guid}/links/evaluate")]
    public async Task<IActionResult> EvaluateGatewayLinks(
        Guid gatewayId,
        CancellationToken cancellationToken)
    {
        await _linkQualityService.EvaluateGatewayLinksAsync(
            gatewayId, cancellationToken);

        return Ok(new MeshOperationResultDto
        {
            GatewayId = gatewayId,
            Message = "Gateway mesh links evaluated.",
            Timestamp = DateTime.UtcNow
        });
    }

    [HttpPost("links/{linkId:guid}/evaluate")]
    public async Task<IActionResult> EvaluateLink(
        Guid linkId,
        CancellationToken cancellationToken)
    {
        await _linkQualityService.EvaluateLinkAsync(
            linkId, cancellationToken);

        return Ok(new MeshOperationResultDto
        {
            LinkId = linkId,
            Message = "Mesh link evaluated.",
            Timestamp = DateTime.UtcNow
        });
    }

    // ROUTING

    [HttpGet("{gatewayId:guid}/routing/shortest-path")]
    public async Task<IActionResult> FindShortestPath(
        Guid gatewayId,
        [FromQuery] Guid sourceNodeId,
        [FromQuery] Guid destinationNodeId,
        CancellationToken cancellationToken)
    {
        var nodes = await _topologyService.GetNodesAsync(
            gatewayId, cancellationToken);

        var links = await _topologyService.GetLinksAsync(
            gatewayId, cancellationToken);

        var graph = _graphBuilder.Build(nodes, links);

        var result = _routingService.FindShortestPath(
            graph, sourceNodeId, destinationNodeId);

        if (!result.Success)
            return NotFound(new { Message = "No route found." });

        return Ok(_mapper.Map<MeshRouteDto>(result));
    }

    // TRAVERSAL

    [HttpGet("{gatewayId:guid}/traversal/bfs/{startNodeId:guid}")]
    public async Task<IActionResult> BreadthFirstSearch(
        Guid gatewayId,
        Guid startNodeId,
        CancellationToken cancellationToken)
    {
        var graph = await BuildGraphAsync(
            gatewayId, cancellationToken);

        var result = _traversalService.BreadthFirstSearch(
            graph, startNodeId);

        return Ok(_mapper.Map<MeshTraversalResultDto>(result));
    }

    [HttpGet("{gatewayId:guid}/traversal/dfs/{startNodeId:guid}")]
    public async Task<IActionResult> DepthFirstSearch(
        Guid gatewayId,
        Guid startNodeId,
        CancellationToken cancellationToken)
    {
        var graph = await BuildGraphAsync(
            gatewayId, cancellationToken);

        var result = _traversalService.DepthFirstSearch(
            graph, startNodeId);

        return Ok(_mapper.Map<MeshTraversalResultDto>(result));
    }

    [HttpGet("{gatewayId:guid}/traversal/bfs-route")]
    public async Task<IActionResult> FindBfsRoute(
        Guid gatewayId,
        [FromQuery] Guid sourceNodeId,
        [FromQuery] Guid destinationNodeId,
        CancellationToken cancellationToken)
    {
        var graph = await BuildGraphAsync(
            gatewayId, cancellationToken);

        var result = _traversalService.FindBfsRoute(
            graph, sourceNodeId, destinationNodeId);

        if (!result.Success)
            return NotFound(new { Message = "No BFS route found." });

        return Ok(_mapper.Map<MeshTraversalResultDto>(result));
    }

    [HttpGet("{gatewayId:guid}/traversal/dfs-route")]
    public async Task<IActionResult> FindDfsRoute(
        Guid gatewayId,
        [FromQuery] Guid sourceNodeId,
        [FromQuery] Guid destinationNodeId,
        CancellationToken cancellationToken)
    {
        var graph = await BuildGraphAsync(
            gatewayId, cancellationToken);

        var result = _traversalService.FindDfsRoute(
            graph, sourceNodeId, destinationNodeId);

        if (!result.Success)
            return NotFound(new { Message = "No DFS route found." });

        return Ok(_mapper.Map<MeshTraversalResultDto>(result));
    }

    // MINIMUM SPANNING TREE

    [HttpGet("{gatewayId:guid}/spanning-tree/{startNodeId:guid}")]
    public async Task<ActionResult<MeshSpanningTreeDto>>
        GetMinimumSpanningTree(
            Guid gatewayId,
            Guid startNodeId,
            CancellationToken cancellationToken)
    {
        var nodes = await _topologyService.GetNodesAsync(
            gatewayId,
            cancellationToken);

        var links = await _topologyService.GetLinksAsync(
            gatewayId,
            cancellationToken);

        var graph = _graphBuilder.Build(nodes, links);

        var result = _spanningTreeService.Calculate(
            graph,
            startNodeId);

        var dto = new MeshSpanningTreeDto
        {
            GatewayId = gatewayId,
            StartNodeId = startNodeId,
            Connections = _mapper.Map<List<MeshSpanningTreeConnectionDto>>(
                result),
            TotalConnections = result.Count,
            TotalCost = result.Sum(x => x.Cost),
            TotalEnergyCost = result.Sum(x => x.EnergyCost)
        };

        return Ok(dto);
    }

    // LOGGING

    [HttpGet("{gatewayId:guid}/logs")]
    public async Task<IActionResult> GetGatewayLogs(
        Guid gatewayId,
        CancellationToken cancellationToken)
    {
        var logs = await _logService.GetByGatewayIdAsync(
            gatewayId, cancellationToken);

        return Ok(_mapper.Map<List<MeshLogDto>>(logs));
    }

    [HttpGet("nodes/{nodeId:guid}/logs")]
    public async Task<IActionResult> GetNodeLogs(
        Guid nodeId,
        CancellationToken cancellationToken)
    {
        var logs = await _logService.GetByNodeIdAsync(
            nodeId, cancellationToken);

        return Ok(_mapper.Map<List<MeshLogDto>>(logs));
    }

    // GRAPH HELPER

    private async Task< /* Your graph type */ dynamic> BuildGraphAsync(
        Guid gatewayId,
        CancellationToken cancellationToken)
    {
        var nodes = await _topologyService.GetNodesAsync(
            gatewayId, cancellationToken);

        var links = await _topologyService.GetLinksAsync(
            gatewayId, cancellationToken);

        return _graphBuilder.Build(nodes, links);
    }
}
