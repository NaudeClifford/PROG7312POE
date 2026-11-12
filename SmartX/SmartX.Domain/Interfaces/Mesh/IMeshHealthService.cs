using SmartX.Domain.Entities.Mesh;

namespace SmartX.Application.Interfaces.Mesh;

public interface IMeshHealthService
{
    Task<MeshHealthReport> GetHealthAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default);
}