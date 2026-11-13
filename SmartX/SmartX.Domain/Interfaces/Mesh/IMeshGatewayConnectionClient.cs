
using SmartX.Domain.Entities.Mesh;

namespace SmartX.Domain.Interfaces.Mesh;

public interface IMeshGatewayConnectionClient
{
    Task<IReadOnlyList<MeshLink>> GetConnectionsAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default);
}
