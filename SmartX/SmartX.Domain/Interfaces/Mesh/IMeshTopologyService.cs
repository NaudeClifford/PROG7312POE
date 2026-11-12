using SmartX.Domain.Entities.Mesh;

namespace SmartX.Application.Interfaces.Mesh;

public interface IMeshTopologyService
{
    Task RegisterNodeAsync(
        MeshNode node,
        CancellationToken cancellationToken = default);

    Task UpdateNodeAsync(
        MeshNode node,
        CancellationToken cancellationToken = default);

    Task RemoveNodeAsync(
        Guid gatewayId,
        Guid nodeId,
        CancellationToken cancellationToken = default);

    Task RegisterLinkAsync(
        MeshLink link,
        CancellationToken cancellationToken = default);

    Task UpdateLinkAsync(
        MeshLink link,
        CancellationToken cancellationToken = default);

    Task RemoveLinkAsync(
        Guid gatewayId,
        Guid linkId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MeshNode>> GetNodesAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MeshLink>> GetLinksAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default);
}