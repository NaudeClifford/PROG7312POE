namespace SmartX.WPF.Services.Sync;

public interface ICacheSyncService
{
    Task SyncCompaniesAsync(
       CancellationToken cancellationToken = default);

    Task SyncGatewaysAsync(
        CancellationToken cancellationToken = default);

    Task SyncSensorsAsync(
        CancellationToken cancellationToken = default);

    Task SyncTelemetryAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default);

    Task SyncUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}