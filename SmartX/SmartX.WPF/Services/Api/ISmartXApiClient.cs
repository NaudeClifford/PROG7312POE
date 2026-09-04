using SmartX.Application.Requests.Company;
using SmartX.Application.Requests.Gateway;
using SmartX.Application.Requests.Sensor;
using SmartX.Application.Requests.Telemetry;
using SmartX.Application.Requests.User;
using SmartX.Application.Services.Registration;
using SmartX.Shared.DTOs;
using SmartX.Shared.DTOs.Sensors;
using SmartX.Shared.DTOs.Telemetry;
using System.IO;

namespace SmartX.WPF.Services.Api;

public interface ISmartXApiClient
{

    // AVAILABILITY
    Task<bool> IsAvailableAsync(
        CancellationToken cancellationToken = default);


    // SENSORS - CRUD
    Task<IReadOnlyList<SensorDto>> GetSensorsAsync(
        CancellationToken cancellationToken = default);

    Task<SensorDto?> GetSensorByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateSensorAsync(
        CreateSensorRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateSensorAsync(
        UpdateSensorRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteSensorAsync(
        Guid id,
        CancellationToken cancellationToken = default);


    // SENSOR LOG FILES
    Task<IReadOnlyList<SensorLogFileDto>> GetSensorLogFilesAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default);

    Task<SensorLogFileUploadResultDto> UploadSensorLogFileAsync(
        Guid sensorId,
        string fileName,
        Stream fileStream,
        string contentType,
        Guid userId,
        CancellationToken cancellationToken = default);


    // TELEMETRY - CQRS
    Task<TelemetryDto?> GetTelemetryByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TelemetryDto>> GetTelemetryBySensorIdAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default);

    Task<TelemetryDto?> GetLatestTelemetryBySensorIdAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TelemetryDto>> GetTelemetryByDateRangeAsync(
        Guid sensorId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateTelemetryAsync(
    CreateTelemetryRequest request,
    CancellationToken cancellationToken = default);
    // USERS - CRUD
    Task<UserDto?> GetUserByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserDto>> GetUsersByCompanyIdAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task<UserDto?> GetUserByFirebaseUidAsync(
        string firebaseUid, string idToken,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserDto>> GetUsersAsync(
        CancellationToken cancellationToken = default);

    Task<Guid> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateUserAsync(
        UpdateUserRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteUserAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<RegistrationResultDto> RegisterCompanyAsync(
        RegisterCompanyRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> CompleteCompanyOnboardingAsync(
    Guid companyId,
    CancellationToken cancellationToken = default);

    // COMPANIES - CRUD
    Task<IReadOnlyList<CompanyDto>> GetCompaniesAsync(
        CancellationToken cancellationToken = default);

    Task<CompanyDto?> GetCompanyByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateCompanyAsync(
        CreateCompanyRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateCompanyAsync(
        UpdateCompanyRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> RequestCompanyDeletionAsync(
    Guid companyId,
    CancellationToken cancellationToken = default);

    Task<bool> DeleteCompanyAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> CompleteOnboardingAsync(
    Guid companyId,
    CancellationToken cancellationToken = default);

    // COMPANY CONFIGURATION

    Task<CompanyConfigurationDto?> GetCompanyConfigurationAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateCompanyConfigurationAsync(
        UpdateCompanyConfigurationRequest request,
        CancellationToken cancellationToken = default);


    // GATEWAYS - CRUD
    Task<IReadOnlyList<GatewayDto>> GetGatewaysAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GatewayDto>> GetGatewaysByCompanyIdAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task<GatewayDto?> GetGatewayByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateGatewayAsync(
        CreateGatewayRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateGatewayAsync(
        UpdateGatewayRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteGatewayAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
