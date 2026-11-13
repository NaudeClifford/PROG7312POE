using SmartX.Domain.Enums.Mesh;
using SmartX.Domain.Interfaces;
using SmartX.Domain.Interfaces.Mesh;

namespace SmartX.Application.Services.Mesh.Monitoring;

public class MeshSensorHeartbeatService : IMeshSensorHeartbeatService
{
    private readonly ISensorRepository _sensorRepository;
    private readonly IMeshNodeRepository _nodeRepository;
    private readonly MeshSensorMonitoringOptions _options;

    public MeshSensorHeartbeatService(
        ISensorRepository sensorRepository,
        IMeshNodeRepository nodeRepository,
        MeshSensorMonitoringOptions options)
    {
        _sensorRepository = sensorRepository;
        _nodeRepository = nodeRepository;
        _options = options;
    }

    public async Task ProcessHeartbeatAsync(
        Guid gatewayId,
        Guid sensorId,
        CancellationToken cancellationToken = default)
    {
        ValidateIds(gatewayId, sensorId);

        var sensors = await _sensorRepository.GetAllAsync(
            cancellationToken);

        var sensor = sensors.FirstOrDefault(x =>
            x.Id == sensorId &&
            x.GatewayId == gatewayId &&
            x.IsActive);

        if (sensor is null)
            throw new InvalidOperationException(
                "Active sensor was not found for this gateway.");

        var nodes = await _nodeRepository.GetByGatewayIdAsync(
            gatewayId,
            cancellationToken);

        var node = nodes.FirstOrDefault(x =>
            x.DeviceId == sensorId &&
            x.NodeType == MeshNodeType.Sensor);

        if (node is null)
            throw new InvalidOperationException(
                "Mesh node has not been registered for this sensor.");

        var now = DateTime.UtcNow;

        node.Status = MeshNodeStatus.Online;
        node.LastSeen = now;
        node.UpdatedAt = now;

        await _nodeRepository.UpdateAsync(
            node,
            cancellationToken);
    }

    public async Task EvaluateSensorConnectivityAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        if (gatewayId == Guid.Empty)
            throw new ArgumentException(
                "A valid gateway ID is required.",
                nameof(gatewayId));

        var sensors = await _sensorRepository.GetAllAsync(
            cancellationToken);

        var gatewaySensors = sensors
            .Where(x =>
                x.GatewayId == gatewayId &&
                x.IsActive)
            .ToList();

        var nodes = await _nodeRepository.GetByGatewayIdAsync(
            gatewayId,
            cancellationToken);

        var now = DateTime.UtcNow;

        var heartbeatTimeout = TimeSpan.FromSeconds(
            _options.HeartbeatTimeoutSeconds);

        foreach (var sensor in gatewaySensors)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var node = nodes.FirstOrDefault(x =>
                x.DeviceId == sensor.Id &&
                x.NodeType == MeshNodeType.Sensor);

            if (node is null)
                continue;

            var isOnline =
                node.LastSeen.HasValue &&
                node.LastSeen.Value >= now - heartbeatTimeout;

            var newStatus = isOnline
                ? MeshNodeStatus.Online
                : MeshNodeStatus.Offline;

            if (node.Status == newStatus)
                continue;

            node.Status = newStatus;
            node.UpdatedAt = now;

            await _nodeRepository.UpdateAsync(
                node,
                cancellationToken);
        }
    }

    private static void ValidateIds(
        Guid gatewayId,
        Guid sensorId)
    {
        if (gatewayId == Guid.Empty)
            throw new ArgumentException(
                "A valid gateway ID is required.",
                nameof(gatewayId));

        if (sensorId == Guid.Empty)
            throw new ArgumentException(
                "A valid sensor ID is required.",
                nameof(sensorId));
    }
}
