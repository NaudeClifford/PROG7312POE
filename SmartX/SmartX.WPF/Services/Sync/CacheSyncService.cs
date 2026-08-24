using SmartX.Domain.Entities;
using SmartX.Domain.Enums;
using SmartX.WPF.Repositories.Local;
using SmartX.WPF.Services.Api;

namespace SmartX.WPF.Services.Sync;

public class CacheSyncService(
    ISmartXApiClient apiClient,
    ILocalSensorCache sensorCache,
    ILocalTelemetryCache telemetryCache,
    ILocalUserCache userCache,
    ILocalCompanyCache companyCache,
    ILocalGatewayCache gatewayCache) : ICacheSyncService
{
    private readonly ISmartXApiClient _apiClient = apiClient;

    private readonly ILocalSensorCache _sensorCache =
        sensorCache;

    private readonly ILocalTelemetryCache _telemetryCache =
        telemetryCache;

    private readonly ILocalUserCache _userCache =
        userCache;

    private readonly ILocalCompanyCache _companyCache =
        companyCache;

    private readonly ILocalGatewayCache _gatewayCache =
        gatewayCache;


    // =========================================================
    // SENSORS
    // =========================================================

    public async Task SyncSensorsAsync(
    CancellationToken cancellationToken = default)
    {
        var sensors = await _apiClient.GetSensorsAsync(
            cancellationToken);

        foreach (var dto in sensors)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var cachedSensor = await _sensorCache.GetByIdAsync(
                dto.Id,
                cancellationToken);

            if (cachedSensor is not null &&
                dto.UpdatedAt <= cachedSensor.UpdatedAt)
            {
                continue;
            }

            var sensor = new Sensor
            {
                Id = dto.Id,
                Name = dto.Name,
                DeviceIdentifier = dto.DeviceIdentifier,
                Category = (SensorCategory)dto.Category,
                Location = dto.Location,
                Description = dto.Description,
                IsActive = dto.IsActive,
                GatewayId = dto.GatewayId,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt
            };

            await _sensorCache.UpdateAsync(
                sensor,
                cancellationToken);
        }
    }

    // =========================================================
    // TELEMETRY
    // =========================================================

    public async Task SyncTelemetryAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default)
    {
        var telemetry =
            await _apiClient.GetTelemetryBySensorIdAsync(
                sensorId,
                cancellationToken);

        foreach (var dto in telemetry)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var cached =
                await _telemetryCache.GetByIdAsync(
                    dto.Id,
                    cancellationToken);

            if (cached is not null &&
                dto.UpdatedAt <= cached.UpdatedAt)
            {
                continue;
            }

            var entity = new Telemetry
            {
                Id = dto.Id,
                SensorId = dto.SensorId,
                Timestamp = dto.Timestamp,
                Voltage = dto.Voltage,
                Current = dto.Current,
                Power = dto.Power,
                Temperature = dto.Temperature,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt
            };

            await _telemetryCache.UpdateAsync(
                entity,
                cancellationToken);
        }
    }


    // =========================================================
    // USER
    // =========================================================

    public async Task SyncUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var dto =
            await _apiClient.GetUserByIdAsync(
                userId,
                cancellationToken);

        if (dto is null)
            return;

        cancellationToken.ThrowIfCancellationRequested();

        var cached =
            await _userCache.GetByIdAsync(
                dto.Id,
                cancellationToken);

        if (cached is not null &&
            dto.UpdatedAt <= cached.UpdatedAt)
        {
            return;
        }

        var user = new User
        {
            Id = dto.Id,
            FirebaseUid = dto.FirebaseUid,
            Email = dto.Email,
            CompanyId = dto.CompanyId,
            DisplayName = dto.DisplayName,
            Role = dto.Role,
            IsActive = dto.IsActive,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt
        };

        await _userCache.UpdateAsync(
            user,
            cancellationToken);
    }


    // =========================================================
    // COMPANIES
    // =========================================================

    public async Task SyncCompaniesAsync(
        CancellationToken cancellationToken = default)
    {
        var companies =
            await _apiClient.GetCompaniesAsync(
                cancellationToken);

        foreach (var dto in companies)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var cached =
                await _companyCache.GetByIdAsync(
                    dto.Id,
                    cancellationToken);

            if (cached is not null &&
                dto.UpdatedAt <= cached.UpdatedAt)
            {
                continue;
            }

            var company = new Company
            {
                Id = dto.Id,
                Name = dto.Name,
                Description = dto.Description,
                IsActive = dto.IsActive,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt
            };

            await _companyCache.UpdateAsync(
                company,
                cancellationToken);
        }
    }


    // =========================================================
    // GATEWAYS
    // =========================================================

    public async Task SyncGatewaysAsync(
        CancellationToken cancellationToken = default)
    {
        var gateways =
            await _apiClient.GetGatewaysAsync(
                cancellationToken);

        foreach (var dto in gateways)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var cached =
                await _gatewayCache.GetByIdAsync(
                    dto.Id,
                    cancellationToken);

            if (cached is not null &&
                dto.UpdatedAt <= cached.UpdatedAt)
            {
                continue;
            }

            var gateway = new Gateway
            {
                Id = dto.Id,
                CompanyId = dto.CompanyId,
                Name = dto.Name,
                Description = dto.Description,
                SerialNumber = dto.SerialNumber,
                IpAddress = dto.IpAddress,
                IsActive = dto.IsActive,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt
            };

            await _gatewayCache.UpdateAsync(
                gateway,
                cancellationToken);
        }
    }
}