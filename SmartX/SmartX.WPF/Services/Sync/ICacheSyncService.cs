namespace SmartX.WPF.Services.Sync;

public interface ICacheSyncService
{
    Task SyncSensorsAsync(
        CancellationToken cancellationToken = default);

    Task SyncTelemetryAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default);

    Task SyncUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}