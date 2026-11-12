using SmartX.Domain.Entities.Mesh;

namespace SmartX.Application.Interfaces.Mesh;

public interface IMeshLogService
{
    Task LogAsync(
        MeshLog log,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MeshLog>> GetByNodeIdAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MeshLog>> GetByGatewayIdAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default);

    Task DeleteOlderThanAsync(
        DateTime cutoff,
        CancellationToken cancellationToken = default);
}