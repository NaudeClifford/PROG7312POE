using SmartX.Domain.Entities;

namespace SmartX.WPF.Services.Api;

public interface ISmartXApiClient
{
    // Sensors
    Task<IReadOnlyList<Sensor>> GetSensorsAsync(
        CancellationToken cancellationToken = default);

    Task<Sensor?> GetSensorByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    // Telemetry
    Task<IReadOnlyList<Telemetry>> GetTelemetryBySensorIdAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default);

    Task<Telemetry?> GetLatestTelemetryBySensorIdAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default);

    // Users
    Task<User?> GetUserByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<User?> GetUserByFirebaseUidAsync(
        string firebaseUid,
        CancellationToken cancellationToken = default);
}