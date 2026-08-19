using SmartX.Shared.DTOs;
using SmartX.Shared.DTOs.Sensors;
using SmartX.Shared.DTOs.Telemetry;

namespace SmartX.WPF.Services.Api;

public interface ISmartXApiClient
{
    // Sensors
    Task<IReadOnlyList<SensorDto>> GetSensorsAsync(
        CancellationToken cancellationToken = default);

    Task<SensorDto?> GetSensorByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    // Telemetry
    Task<IReadOnlyList<TelemetryDto>> GetTelemetryBySensorIdAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default);

    Task<TelemetryDto?> GetLatestTelemetryBySensorIdAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default);

    // Users
    Task<UserDto?> GetUserByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<UserDto?> GetUserByFirebaseUidAsync(
        string firebaseUid,
        CancellationToken cancellationToken = default);

    // Companies
    Task<IReadOnlyList<CompanyDto>> GetCompaniesAsync(
        CancellationToken cancellationToken = default);

    Task<CompanyDto?> GetCompanyByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    // Gateways
    Task<IReadOnlyList<GatewayDto>> GetGatewaysAsync(
        CancellationToken cancellationToken = default);

    Task<GatewayDto?> GetGatewayByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}