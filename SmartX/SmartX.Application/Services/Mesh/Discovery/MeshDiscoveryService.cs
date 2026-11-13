
using SmartX.Application.Interfaces.Mesh;
using SmartX.Domain.Entities.Mesh;
using SmartX.Domain.Enums.Mesh;
using SmartX.Domain.Interfaces;
using SmartX.Domain.Interfaces.Mesh;

namespace SmartX.Application.Services.Mesh.Discovery;

public class MeshDiscoveryService(
    IMeshDiscoveryProvider discoveryProvider,
    IMeshTopologyService topologyService,
    IMeshNodeRepository nodeRepository,
    IMeshLinkRepository linkRepository,
    IMeshLogRepository logRepository,
    ISensorRepository sensorRepository,
    IMeshConnectionDiscoveryService connectionDiscoveryService)
    : IMeshDiscoveryService
{
    private readonly IMeshDiscoveryProvider _discoveryProvider =
        discoveryProvider;

    private readonly IMeshTopologyService _topologyService =
        topologyService;

    private readonly IMeshConnectionDiscoveryService _connectionDiscoveryService = connectionDiscoveryService;
    private readonly IMeshNodeRepository _nodeRepository =
        nodeRepository;

    private readonly IMeshLinkRepository _linkRepository =
        linkRepository;

    private readonly IMeshLogRepository _logRepository =
        logRepository;

    private readonly ISensorRepository _sensorRepository =
        sensorRepository;

    public async Task<MeshDiscoveryResult> DiscoverAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        ValidateGateway(gatewayId);

        var result = await _discoveryProvider.DiscoverAsync(
      gatewayId,
      cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        if (result is null)
        {
            throw new InvalidOperationException(
                "Mesh discovery returned no result.");
        }

        result.DiscoveredNodes ??= new List<MeshNode>();

        var discoveredConnections =
            await _connectionDiscoveryService.DiscoverConnectionsAsync(
                gatewayId,
                cancellationToken);

        result.DiscoveredLinks = discoveredConnections.ToList();

        // Validate gateway discovery results.
        foreach (var node in result.DiscoveredNodes)
        {
            if (node.GatewayId != gatewayId)
            {
                throw new InvalidOperationException(
                    "A discovered node belongs to another gateway.");
            }
        }

        foreach (var link in result.DiscoveredLinks)
        {
            if (link.GatewayId != gatewayId)
            {
                throw new InvalidOperationException(
                    "A discovered link belongs to another gateway.");
            }
        }

        // Retrieve actual registered sensors.
        var sensors = await _sensorRepository.GetAllAsync(
            cancellationToken);

        var gatewaySensors = sensors
            .Where(sensor =>
                sensor.GatewayId == gatewayId &&
                sensor.IsActive)
            .ToList();

        // Convert registered sensors into mesh nodes.
        foreach (var sensor in gatewaySensors)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var existing = result.DiscoveredNodes.FirstOrDefault(
                node =>
                    node.DeviceId == sensor.Id &&
                    node.NodeType == MeshNodeType.Sensor);

            if (existing is not null)
            {
                existing.Name = sensor.Name;
                existing.DeviceIdentifier =
                    sensor.DeviceIdentifier;

                continue;
            }

            result.DiscoveredNodes.Add(
                new MeshNode
                {
                    Id = Guid.NewGuid(),

                    GatewayId = gatewayId,

                    // The sensor's actual database ID.
                    DeviceId = sensor.Id,

                    DeviceIdentifier =
                        sensor.DeviceIdentifier,

                    Name = sensor.Name,

                    NodeType = MeshNodeType.Sensor,

                    IsGateway = false,

                    // Remains Offline until actual mesh
                    // connectivity has been established.
                    LastSeen = null,

                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
        }

        return result;
    }

    public async Task<int> SynchronizeAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        ValidateGateway(gatewayId);
        await SynchronizeSensorsAsync(
            gatewayId,
            cancellationToken);

        var discovery = await DiscoverAsync(
            gatewayId,
            cancellationToken);

        var existingNodes =
            await _nodeRepository.GetByGatewayIdAsync(
                gatewayId,
                cancellationToken);

        var existingLinks =
            await _linkRepository.GetByGatewayIdAsync(
                gatewayId,
                cancellationToken);

        var nodesByDevice = existingNodes
            .GroupBy(x => (x.DeviceId, x.NodeType))
            .ToDictionary(x => x.Key, x => x.First());

        var linksByEndpoints = existingLinks
            .GroupBy(x => NormalizeEndpoints(
                x.SourceNodeId,
                x.TargetNodeId))
            .ToDictionary(
                x => x.Key,
                x => x.First());

        int changes = 0;

        // Synchronize discovered nodes.
        foreach (var discovered in discovery.DiscoveredNodes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (nodesByDevice.TryGetValue(
                (discovered.DeviceId, discovered.NodeType),
                out var existing))
            {
                discovered.Id = existing.Id;
                discovered.CreatedAt = existing.CreatedAt;

                await _topologyService.UpdateNodeAsync(
                    discovered,
                    cancellationToken);
            }
            else
            {
                await _topologyService.RegisterNodeAsync(
                    discovered,
                    cancellationToken);

                nodesByDevice[
                    (discovered.DeviceId, discovered.NodeType)
                ] = discovered;
            }

            changes++;
        }

        // Refresh nodes before synchronizing links.
        var synchronizedNodes =
            await _nodeRepository.GetByGatewayIdAsync(
                gatewayId,
                cancellationToken);

        var validNodeIds = synchronizedNodes
            .Select(x => x.Id)
            .ToHashSet();

        // Synchronize actual discovered links.
        foreach (var discovered in discovery.DiscoveredLinks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!validNodeIds.Contains(discovered.SourceNodeId) ||
                !validNodeIds.Contains(discovered.TargetNodeId))
            {
                continue;
            }

            var key = NormalizeEndpoints(
                discovered.SourceNodeId,
                discovered.TargetNodeId);

            if (linksByEndpoints.TryGetValue(key, out var existing))
            {
                discovered.Id = existing.Id;
                discovered.CreatedAt = existing.CreatedAt;

                await _topologyService.UpdateLinkAsync(
                    discovered,
                    cancellationToken);
            }
            else
            {
                await _topologyService.RegisterLinkAsync(
                    discovered,
                    cancellationToken);

                linksByEndpoints[key] = discovered;
            }

            changes++;
        }

        await _logRepository.AddAsync(
            new SmartX.Domain.Entities.Mesh.MeshLog
            {
                Id = Guid.NewGuid(),
                GatewayId = gatewayId,
                EventType = "MeshDiscoveryCompleted",
                Message =
                    $"Mesh discovery completed. " +
                    $"Nodes discovered: {discovery.DiscoveredNodes.Count}. " +
                    $"Links discovered: {discovery.DiscoveredLinks.Count}.",
                Severity =
                    SmartX.Domain.Enums.Mesh.MeshLogSeverity.Information,
                Timestamp = DateTime.UtcNow
            },
            cancellationToken);

        return changes;
    }

    private async Task SynchronizeSensorsAsync(
    Guid gatewayId,
    CancellationToken cancellationToken)
    {
        var sensors = await _sensorRepository.GetAllAsync(
            cancellationToken);

        var gatewaySensors = sensors
            .Where(x =>
                x.GatewayId == gatewayId &&
                x.IsActive)
            .ToList();

        var existingNodes =
            await _nodeRepository.GetByGatewayIdAsync(
                gatewayId,
                cancellationToken);

        foreach (var sensor in gatewaySensors)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var existing = existingNodes.FirstOrDefault(x =>
                x.DeviceId == sensor.Id &&
                x.NodeType == MeshNodeType.Sensor);

            var node = new MeshNode
            {
                Id = existing?.Id ?? Guid.NewGuid(),

                GatewayId = gatewayId,
                DeviceId = sensor.Id,

                DeviceIdentifier = sensor.DeviceIdentifier,
                Name = sensor.Name,

                NodeType = MeshNodeType.Sensor,
                Status = existing?.Status ?? MeshNodeStatus.Offline,

                IsGateway = false,

                BatteryLevel = existing?.BatteryLevel,
                LastSeen = existing?.LastSeen,

                CreatedAt = existing?.CreatedAt ?? DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            if (existing is null)
            {
                await _topologyService.RegisterNodeAsync(
                    node,
                    cancellationToken);
            }
            else
            {
                await _topologyService.UpdateNodeAsync(
                    node,
                    cancellationToken);
            }
        }
    }
    private static (Guid First, Guid Second) NormalizeEndpoints(
    Guid source,
    Guid target)
    {
        return source.CompareTo(target) <= 0
            ? (source, target)
            : (target, source);
    }

    private static void ValidateGateway(Guid gatewayId)
    {
        if (gatewayId == Guid.Empty)
        {
            throw new ArgumentException(
                "A valid gateway ID is required.",
                nameof(gatewayId));
        }
    }
}
