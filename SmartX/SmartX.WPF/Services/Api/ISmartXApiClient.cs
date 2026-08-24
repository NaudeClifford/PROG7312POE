using SmartX.Application.Commands.Company;
using SmartX.Application.Commands.Gateway;
using SmartX.Application.Commands.Sensors;
using SmartX.Application.Commands.Users;
using SmartX.Shared.DTOs;
using SmartX.Shared.DTOs.SensorLog;
using SmartX.Shared.DTOs.Sensors;
using SmartX.Shared.DTOs.Telemetry;
using System.IO;

namespace SmartX.WPF.Services.Api;

public interface ISmartXApiClient
{

    Task<bool> IsAvailableAsync(
    CancellationToken cancellationToken = default);

    // Sensors
    Task<IReadOnlyList<SensorDto>> GetSensorsAsync(
        CancellationToken cancellationToken = default);

    Task<SensorDto?> GetSensorByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateSensorAsync(
        CreateSensorCommand command,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateSensorAsync(
        UpdateSensorCommand command,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteSensorAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SensorLogFileDto>>
        GetSensorLogFilesAsync(
            Guid sensorId,
            CancellationToken cancellationToken = default);

    Task<SensorLogFileUploadResultDto>
        UploadSensorLogFileAsync(
            Guid sensorId,
            string fileName,
            Stream fileStream,
            string contentType,
            Guid userId,
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

    Task<IReadOnlyList<UserDto>> GetUsersAsync(
        CancellationToken cancellationToken = default);

    Task<Guid> CreateUserAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateUserAsync(
        UpdateUserCommand command,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteUserAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    // Companies
    Task<IReadOnlyList<CompanyDto>> GetCompaniesAsync(
        CancellationToken cancellationToken = default);

    Task<CompanyDto?> GetCompanyByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateCompanyAsync(
        CreateCompanyCommand command,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateCompanyAsync(
        UpdateCompanyCommand command,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteCompanyAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    // Gateways

    Task<IReadOnlyList<GatewayDto>> GetGatewaysAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GatewayDto>> GetGatewaysByCompanyIdAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task<GatewayDto?> GetGatewayByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateGatewayAsync(
        CreateGatewayCommand command,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateGatewayAsync(
        UpdateGatewayCommand command,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteGatewayAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}