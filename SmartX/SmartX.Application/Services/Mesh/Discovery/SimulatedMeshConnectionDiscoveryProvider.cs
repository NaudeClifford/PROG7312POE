using SmartX.Domain.Entities.Mesh;
using SmartX.Domain.Enums.Mesh;
using SmartX.Domain.Interfaces.Mesh;

namespace SmartX.Application.Services.Mesh.Discovery;

public class SimulatedMeshConnectionDiscoveryProvider
    : IMeshConnectionDiscoveryProvider
{
    private readonly IMeshNodeRepository _nodeRepository;

    public SimulatedMeshConnectionDiscoveryProvider(
        IMeshNodeRepository nodeRepository)
    {
        _nodeRepository = nodeRepository;
    }

    public async Task<IReadOnlyList<MeshLink>> DiscoverAsync(
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

        cancellationToken.ThrowIfCancellationRequested();

        var sensors = nodes
            .Where(x =>
                !x.IsGateway &&
                x.NodeType == MeshNodeType.Sensor &&
                x.Status == MeshNodeStatus.Online)
            .OrderBy(x => x.Id)
            .ToList();

        var links = new List<MeshLink>();

        for (int i = 0; i < sensors.Count - 1; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var source = sensors[i];
            var target = sensors[i + 1];

            links.Add(new MeshLink
            {
                Id = Guid.NewGuid(),

                GatewayId = gatewayId,

                SourceNodeId = source.Id,
                SourceNodeType = source.NodeType,

                TargetNodeId = target.Id,
                TargetNodeType = target.NodeType,

                SignalStrengthDbm = -45,
                LatencyMilliseconds = 20,
                EnergyCost = 0.2,

                IsAvailable = true,

                LastSeenAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        return links;
    }
}