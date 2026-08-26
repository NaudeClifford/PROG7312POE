namespace SmartX.WPF.Services.Sync;

public interface ICacheSyncService
{
    Task SyncCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task SyncGatewaysAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task SyncSensorsAsync(
        CancellationToken cancellationToken = default);

    Task SyncTelemetryAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default);

    Task SyncUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task SyncUsersAsync(
    Guid companyId,
    CancellationToken cancellationToken = default);
}