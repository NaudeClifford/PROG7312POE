using SmartX.Domain.Entities.Mesh;

namespace SmartX.Domain.Interfaces.Mesh;

public interface IMeshLogRepository
{
    Task<IReadOnlyList<MeshLog>> GetByNodeIdAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MeshLog>> GetByGatewayIdAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        MeshLog log,
        CancellationToken cancellationToken = default);

    Task DeleteOlderThanAsync(
        DateTime cutoff,
        CancellationToken cancellationToken = default);
}