using SmartX.Domain.Entities.Mesh;
using SmartX.Shared.Mapping;
using SmartX.WPF.Repositories.Local;
using SmartX.WPF.Services.Api;

namespace SmartX.WPF.Services.Sync;

public class MeshCacheSyncService(
    ISmartXApiClient apiClient,
    ILocalMeshNodeCache nodeCache,
    ILocalMeshLinkCache linkCache,
    ILocalMeshLogCache logCache,
    ILocalMeshRouteCache routeCache,
    ILocalMeshConnectionCache connectionCache,
    IMapper mapper) : IMeshCacheSyncService
{
    private readonly ISmartXApiClient _apiClient = apiClient;

    private readonly ILocalMeshNodeCache _nodeCache = nodeCache;
    private readonly ILocalMeshLinkCache _linkCache = linkCache;
    private readonly ILocalMeshLogCache _logCache = logCache;
    private readonly ILocalMeshRouteCache _routeCache = routeCache;
    private readonly ILocalMeshConnectionCache _connectionCache = connectionCache;

    private readonly IMapper _mapper = mapper;

    // NODES

    public async Task SyncNodesAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        if (gatewayId == Guid.Empty)
            return;

        var topology = await _apiClient.GetMeshTopologyAsync(
            gatewayId);

        cancellationToken.ThrowIfCancellationRequested();

        var serverNodes = topology.Nodes;

        var cachedNodes = await _nodeCache.GetByGatewayIdAsync(
            gatewayId,
            cancellationToken);

        var serverIds = serverNodes
            .Select(x => x.Id)
            .ToHashSet();

        // DELETE STALE NODES

        foreach (var cached in cachedNodes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!serverIds.Contains(cached.Id))
            {
                await _nodeCache.DeleteAsync(
                    cached.Id,
                    cancellationToken);
            }
        }

        // INSERT / UPDATE

        foreach (var dto in serverNodes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var cached = cachedNodes.FirstOrDefault(
                x => x.Id == dto.Id);

            if (cached is not null &&
                dto.UpdatedAt <= cached.UpdatedAt)
            {
                continue;
            }

            var node = _mapper.Map<MeshNode>(dto);

            await _nodeCache.UpdateAsync(
                node,
                cancellationToken);
        }
    }

    // LINKS

    public async Task SyncLinksAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        if (gatewayId == Guid.Empty)
            return;

        var topology = await _apiClient.GetMeshTopologyAsync(
            gatewayId);

        cancellationToken.ThrowIfCancellationRequested();

        var serverLinks = topology.Links;

        var cachedLinks = await _linkCache.GetByGatewayIdAsync(
            gatewayId,
            cancellationToken);

        var serverIds = serverLinks
            .Select(x => x.Id)
            .ToHashSet();

        // DELETE STALE LINKS

        foreach (var cached in cachedLinks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!serverIds.Contains(cached.Id))
            {
                await _linkCache.DeleteAsync(
                    cached.Id,
                    cancellationToken);
            }
        }

        // INSERT / UPDATE

        foreach (var dto in serverLinks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var cached = cachedLinks.FirstOrDefault(
                x => x.Id == dto.Id);

            if (cached is not null &&
                dto.UpdatedAt <= cached.UpdatedAt)
            {
                continue;
            }

            var link = _mapper.Map<MeshLink>(dto);

            await _linkCache.UpdateAsync(
                link,
                cancellationToken);
        }
    }

    // LOGS

    public async Task SyncLogsAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        if (gatewayId == Guid.Empty)
            return;

        var serverLogs = await _apiClient.GetMeshGatewayLogsAsync(
            gatewayId);

        cancellationToken.ThrowIfCancellationRequested();

        var cachedLogs = await _logCache.GetByGatewayIdAsync(
            gatewayId,
            cancellationToken);

        var cachedIds = cachedLogs
            .Select(x => x.Id)
            .ToHashSet();

        foreach (var dto in serverLogs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (cachedIds.Contains(dto.Id))
                continue;

            var log = _mapper.Map<MeshLog>(dto);

            await _logCache.AddAsync(
                log,
                cancellationToken);
        }
    }

    // ROUTES

    public async Task SyncRoutesAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        if (gatewayId == Guid.Empty)
            return;

        var serverRoutes =
            await _apiClient.GetMeshRoutesAsync(gatewayId);

        cancellationToken.ThrowIfCancellationRequested();

        var cachedRoutes =
            await _routeCache.GetByGatewayIdAsync(
                gatewayId,
                cancellationToken);

        var serverIds = serverRoutes
            .Select(x => x.Id)
            .ToHashSet();

        foreach (var dto in serverRoutes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var cached = cachedRoutes.FirstOrDefault(
                x => x.Id == dto.Id);

            if (cached is not null &&
                dto.CalculatedAt <= cached.CalculatedAt)
            {
                continue;
            }

            var route = _mapper.Map<MeshRoute>(dto);

            await _routeCache.UpdateAsync(
                route,
                cancellationToken);
        }

        // Remove routes no longer returned by the API.
        foreach (var cached in cachedRoutes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!serverIds.Contains(cached.Id))
            {
                await _routeCache.DeleteAsync(
                    cached.Id,
                    cancellationToken);
            }
        }
    }

    // CONNECTIONS

    public async Task SyncConnectionsAsync(
     Guid gatewayId,
     CancellationToken cancellationToken = default)
    {
        if (gatewayId == Guid.Empty)
            return;

        var serverConnections =
            await _apiClient.GetMeshConnectionsAsync(
                gatewayId,
                cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        var cachedConnections =
            await _connectionCache.GetByGatewayIdAsync(
                gatewayId,
                cancellationToken);

        var serverIds = serverConnections
            .Select(x => x.Id)
            .ToHashSet();

        // INSERT / UPDATE

        foreach (var dto in serverConnections)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var cached = cachedConnections.FirstOrDefault(
                x => x.Id == dto.Id);

            if (cached is not null &&
                dto.LastUpdated <= cached.LastUpdated)
            {
                continue;
            }

            var connection = _mapper.Map<MeshConnection>(dto);

            await _connectionCache.UpdateAsync(
                connection,
                cancellationToken);
        }

        // DELETE STALE CONNECTIONS

        foreach (var cached in cachedConnections)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!serverIds.Contains(cached.Id))
            {
                await _connectionCache.DeleteAsync(
                    cached.Id,
                    cancellationToken);
            }
        }
    }

    // COMPLETE MESH SYNCHRONIZATION

    public async Task SyncMeshAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        if (gatewayId == Guid.Empty)
            return;

        await SyncNodesAsync(gatewayId, cancellationToken);
        await SyncLinksAsync(gatewayId, cancellationToken);
        await SyncLogsAsync(gatewayId, cancellationToken);
        await SyncRoutesAsync(gatewayId, cancellationToken);
        await SyncConnectionsAsync(gatewayId, cancellationToken);
    }

    public async Task SyncMeshRoutesAsync(
    Guid gatewayId,
    CancellationToken cancellationToken = default)
    {
        if (gatewayId == Guid.Empty)
            return;

        var routes = await _apiClient.GetMeshRoutesAsync(
            gatewayId,
            cancellationToken);

        if (routes is null)
            return;

        var cachedRoutes = await _routeCache
            .GetByGatewayIdAsync(
                gatewayId,
                cancellationToken);

        var serverIds = routes
            .Select(x => x.Id)
            .ToHashSet();

        foreach (var cached in cachedRoutes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!serverIds.Contains(cached.Id))
            {
                await _routeCache.DeleteAsync(
                    cached.Id,
                    cancellationToken);
            }
        }

        foreach (var dto in routes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var entity = _mapper.Map<MeshRoute>(dto);

            await _routeCache.UpdateAsync(
                entity,
                cancellationToken);
        }
    }
}