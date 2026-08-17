using SmartX.Domain.Entities;
using SmartX.Domain.Enums;
using SmartX.Shared;
using SmartX.WPF.Repositories.Local;
using SmartX.WPF.Services.Api;
using Sensor = SmartX.Domain.Entities.Sensor;

namespace SmartX.WPF.Services.Sync;

public class CacheSyncService(
    ISmartXApiClient apiClient,
    ILocalSensorCache sensorCache, ILocalTelemetryCache telemetryCache,
    ILocalUserCache userCache) : ICacheSyncService
{
    private readonly ISmartXApiClient _apiClient = apiClient;
    private readonly ILocalSensorCache _sensorCache = sensorCache;

    private readonly ILocalTelemetryCache _telemetryCache = telemetryCache;
    private readonly ILocalUserCache _userCache = userCache;


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

    public async Task SyncTelemetryAsync(
    Guid sensorId,
    CancellationToken cancellationToken = default)
    {
        var telemetrys =
            await _apiClient.GetTelemetryBySensorIdAsync(
                sensorId,
                cancellationToken);

        foreach (var dto in telemetrys)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var cachedTelemetry =
                await _telemetryCache.GetByIdAsync(
                    dto.Id,
                    cancellationToken);

            if (cachedTelemetry is not null &&
                dto.UpdatedAt <= cachedTelemetry.UpdatedAt)
            {
                continue;
            }

            var telemetry = new Telemetry
            {
                Id = dto.Id,
                SensorId = dto.SensorId,
                Timestamp = dto.Timestamp,
                Voltage = dto.Voltage,
                Current = dto.Current,
                Power = dto.Power,
                Temperature = dto.Temperature,
                UpdatedAt = dto.UpdatedAt
            };

            await _telemetryCache.UpdateAsync(
                telemetry,
                cancellationToken);
        }
    }

    public async Task SyncUserAsync(
    Guid userId,
    CancellationToken cancellationToken = default)
    {
        var dto = await _apiClient.GetUserByIdAsync(
            userId,
            cancellationToken);

        if (dto is null)
            return;

        cancellationToken.ThrowIfCancellationRequested();

        var cachedUser = await _userCache.GetByIdAsync(
            dto.Id,
            cancellationToken);

        if (cachedUser is not null &&
            dto.UpdatedAt <= cachedUser.UpdatedAt)
        {
            return;
        }

        var user = new User
        {
            Id = dto.Id
        };

        await _userCache.UpdateAsync(
            user,
            cancellationToken);
    }
}