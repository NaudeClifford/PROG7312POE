using SmartX.Application.Interfaces.Mesh;
using SmartX.Domain.Entities.Mesh;
using SmartX.Domain.Enums.Mesh;
using SmartX.Domain.Interfaces.Mesh;

namespace SmartX.Application.Services.Mesh.Monitoring;

public class MeshHealthService : IMeshHealthService
{
    private readonly IMeshNodeRepository _nodeRepository;
    private readonly IMeshLinkRepository _linkRepository;
    private readonly IMeshRouteRepository _routeRepository;

    public MeshHealthService(
        IMeshNodeRepository nodeRepository,
        IMeshLinkRepository linkRepository,
        IMeshRouteRepository routeRepository)
    {
        _nodeRepository = nodeRepository;
        _linkRepository = linkRepository;
        _routeRepository = routeRepository;
    }

    public async Task<MeshHealthReport> GetHealthAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        if (gatewayId == Guid.Empty)
            throw new ArgumentException(
                "A valid gateway ID is required.",
                nameof(gatewayId));

        var nodes = await _nodeRepository.GetByGatewayIdAsync(
            gatewayId, cancellationToken);

        var links = await _linkRepository.GetByGatewayIdAsync(
            gatewayId, cancellationToken);

        var routes = await _routeRepository.GetByGatewayIdAsync(
            gatewayId, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        var availableLinks = links
            .Where(x => x.IsAvailable)
            .ToList();

        var successfulRoutes = routes
            .Where(x => x.IsSuccessful)
            .ToList();

        var report = new MeshHealthReport
        {
            GatewayId = gatewayId,
            GeneratedAt = DateTime.UtcNow,

            TotalNodes = nodes.Count,

            OnlineNodes = nodes.Count(x =>
                x.Status == MeshNodeStatus.Online),

            OfflineNodes = nodes.Count(x =>
                x.Status == MeshNodeStatus.Offline),

            DegradedNodes = nodes.Count(x =>
                x.Status == MeshNodeStatus.Degraded),

            UnreachableNodes = nodes.Count(x =>
                x.Status == MeshNodeStatus.Unreachable),

            TotalLinks = links.Count,

            AvailableLinks = availableLinks.Count,

            UnavailableLinks = links.Count -
                               availableLinks.Count,

            TotalRoutes = routes.Count,

            SuccessfulRoutes = successfulRoutes.Count,

            FailedRoutes = routes.Count(x =>
                !x.IsSuccessful),

            AverageLatency = availableLinks.Count == 0
                ? 0
                : availableLinks.Average(x =>
                    x.LatencyMilliseconds ?? 0),

            NetworkAvailability = nodes.Count == 0
                ? 0
                : (double)nodes.Count(x =>
                    x.Status == MeshNodeStatus.Online) /
                  nodes.Count * 100
        };

        GenerateWarnings(report);

        report.Status = CalculateHealthStatus(report);

        return report;
    }

    private static void GenerateWarnings(
        MeshHealthReport report)
    {
        if (report.TotalNodes == 0)
            report.Warnings.Add("No mesh nodes found.");

        if (report.OfflineNodes > 0)
            report.Warnings.Add(
                $"{report.OfflineNodes} node(s) offline.");

        if (report.DegradedNodes > 0)
            report.Warnings.Add(
                $"{report.DegradedNodes} node(s) degraded.");

        if (report.UnreachableNodes > 0)
            report.Warnings.Add(
                $"{report.UnreachableNodes} node(s) unreachable.");

        if (report.UnavailableLinks > 0)
            report.Warnings.Add(
                $"{report.UnavailableLinks} link(s) unavailable.");

        if (report.FailedRoutes > 0)
            report.Warnings.Add(
                $"{report.FailedRoutes} historical route(s) failed.");

        if (report.AvailableLinks == 0 &&
            report.TotalLinks > 0)
        {
            report.Warnings.Add(
                "No available mesh connections.");
        }
    }

    private static MeshHealthStatus CalculateHealthStatus(
        MeshHealthReport report)
    {
        if (report.TotalNodes == 0 ||
            report.OnlineNodes == 0)
            return MeshHealthStatus.Offline;

        if (report.UnreachableNodes > 0 ||
            report.NetworkAvailability < 50 ||
            report.AvailableLinks == 0 &&
            report.TotalLinks > 0)
            return MeshHealthStatus.Critical;

        if (report.OfflineNodes > 0 ||
            report.DegradedNodes > 0 ||
            report.NetworkAvailability < 90 ||
            report.UnavailableLinks > 0)
            return MeshHealthStatus.Degraded;

        return MeshHealthStatus.Healthy;
    }
}