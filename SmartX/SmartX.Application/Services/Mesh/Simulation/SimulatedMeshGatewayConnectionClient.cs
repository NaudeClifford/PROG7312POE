
using SmartX.Domain.Entities.Mesh;
using SmartX.Domain.Interfaces.Mesh;

namespace SmartX.Infrastructure.Mesh.Simulation;

public class SimulatedMeshGatewayConnectionClient(
    IMeshNodeRepository nodeRepository)
    : IMeshGatewayConnectionClient
{
    private readonly IMeshNodeRepository _nodeRepository =
        nodeRepository;

    public async Task<IReadOnlyList<MeshLink>> GetConnectionsAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        if (gatewayId == Guid.Empty)
            throw new ArgumentException(
                "Invalid gateway ID.",
                nameof(gatewayId));

        var nodes = await _nodeRepository.GetByGatewayIdAsync(
            gatewayId,
            cancellationToken);

        var sensors = nodes
            .Where(x => !x.IsGateway)
            .OrderBy(x => x.Name)
            .ToList();

        var connections = new List<MeshLink>();

        // Simulate gateway-mediated connectivity.
        // Each reachable sensor connects to the gateway.
        var gatewayNode = nodes.FirstOrDefault(x => x.IsGateway);

        if (gatewayNode is null)
        {
            throw new InvalidOperationException(
                "Gateway mesh node was not found.");
        }

        foreach (var sensor in sensors)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Simulate connectivity.
            var reachable = SimulateConnectivity(sensor);

            if (!reachable)
                continue;

            connections.Add(new MeshLink
            {
                Id = Guid.NewGuid(),
                GatewayId = gatewayId,

                SourceNodeId = gatewayNode.Id,
                TargetNodeId = sensor.Id,

                SignalStrengthDbm = -45,
                LatencyMilliseconds = 15,
                EnergyCost = 10,

                IsAvailable = true,

                LastSeenAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        return connections;
    }

    private static bool SimulateConnectivity(MeshNode sensor)
    {
        // Initially all registered simulated sensors
        // are reachable.
        return true;
    }
}
