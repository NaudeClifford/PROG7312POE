namespace SmartX.WPF.Services.Sync;

public interface IMeshCacheSyncService
{
    Task SyncNodesAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default);

    Task SyncLinksAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default);

    Task SyncLogsAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default);

    Task SyncRoutesAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default);

    Task SyncConnectionsAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default);

    Task SyncMeshAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default);
}