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
            gatewayId,
            cancellationToken);

        var links = await _linkRepository.GetByGatewayIdAsync(
            gatewayId,
            cancellationToken);

        // Adapt this call if your route repository uses
        // a different method name.
        var routes = await _routeRepository.GetByGatewayIdAsync(
            gatewayId,
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

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

            AvailableLinks = links.Count(x =>
                x.IsAvailable),

            UnavailableLinks = links.Count(x =>
                !x.IsAvailable),

            TotalRoutes = routes.Count,

            SuccessfulRoutes = routes.Count(x =>
                x.IsSuccessful),

            FailedRoutes = routes.Count(x =>
                !x.IsSuccessful)
        };

        // Calculate network availability.
        report.NetworkAvailability =
            report.TotalNodes == 0
                ? 0
                : (double)report.OnlineNodes /
                  report.TotalNodes * 100;

        // Calculate average route latency.
        var successfulRoutes = routes
            .Where(x => x.IsSuccessful)
            .ToList();

        report.AverageLatency =
            successfulRoutes.Count == 0
                ? 0
                : successfulRoutes.Average(x =>
                    x.TotalLatency);

        // Generate health warnings.
        if (report.TotalNodes == 0)
        {
            report.Warnings.Add(
                "No mesh nodes were found.");
        }

        if (report.OfflineNodes > 0)
        {
            report.Warnings.Add(
                $"{report.OfflineNodes} mesh node(s) are offline.");
        }

        if (report.DegradedNodes > 0)
        {
            report.Warnings.Add(
                $"{report.DegradedNodes} mesh node(s) are degraded.");
        }

        if (report.UnavailableLinks > 0)
        {
            report.Warnings.Add(
                $"{report.UnavailableLinks} mesh link(s) are unavailable.");
        }

        if (report.FailedRoutes > 0)
        {
            report.Warnings.Add(
                $"{report.FailedRoutes} route(s) failed.");
        }

        // Determine overall health.
        report.Status = CalculateHealthStatus(report);

        return report;
    }

    private static MeshHealthStatus CalculateHealthStatus(
    MeshHealthReport report)
    {
        if (report.TotalNodes == 0 ||
            report.OnlineNodes == 0)
        {
            return MeshHealthStatus.Offline;
        }

        if (report.OfflineNodes > 0 ||
            report.UnreachableNodes > 0 ||
            report.FailedRoutes > 0 ||
            report.UnavailableLinks > 0)
        {
            return MeshHealthStatus.Critical;
        }

        if (report.DegradedNodes > 0)
            return MeshHealthStatus.Degraded;

        return MeshHealthStatus.Healthy;
    }
}