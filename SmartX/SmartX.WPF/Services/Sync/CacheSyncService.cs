using SmartX.Shared.Mapping;
using SmartX.Domain.Entities;
using SmartX.Domain.Enums;
using SmartX.Shared.DTOs;
using SmartX.WPF.Repositories.Local;
using SmartX.WPF.Services.Api;
using System.Net.Http;

namespace SmartX.WPF.Services.Sync;

public class CacheSyncService(
    ISmartXApiClient apiClient,
    ILocalSensorCache sensorCache,
    ILocalTelemetryCache telemetryCache,
    ILocalUserCache userCache,
    ILocalCompanyCache companyCache,
    ILocalGatewayCache gatewayCache,
    IMapper mapper) : ICacheSyncService
{
    private readonly ISmartXApiClient _apiClient = apiClient;
    private readonly ILocalSensorCache _sensorCache = sensorCache;
    private readonly ILocalTelemetryCache _telemetryCache = telemetryCache;
    private readonly ILocalUserCache _userCache = userCache;
    private readonly ILocalCompanyCache _companyCache = companyCache;
    private readonly ILocalGatewayCache _gatewayCache = gatewayCache;
    private readonly IMapper _mapper = mapper;


    // SENSORS

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
            var sensor = _mapper.Map<Sensor>(dto);

            await _sensorCache.UpdateAsync(
                sensor,
                cancellationToken);
        }
    }

    // TELEMETRY

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
            var entity =
            _mapper.Map<Telemetry>(dto);

            await _telemetryCache.UpdateAsync(
                entity,
                cancellationToken);
        }
    }


    // USER

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

        await _userCache.UpdateAsync(
    dto,
    cancellationToken);
    }

    // COMPANIES

    public async Task SyncCompanyAsync(
    Guid companyId,
    CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            return;

        var dto =
            await _apiClient.GetCompanyByIdAsync(
                companyId,
                cancellationToken);

        if (dto is null)
            return;

        cancellationToken.ThrowIfCancellationRequested();

        var cached =
            await _companyCache.GetByIdAsync(
                companyId,
                cancellationToken);

        if (cached is not null &&
            dto.UpdatedAt <= cached.UpdatedAt)
        {
            return;
        }

        var company =
            _mapper.Map<Company>(dto);

        await _companyCache.UpdateAsync(
            company,
            cancellationToken);
    }


    // GATEWAYS

    public async Task SyncGatewaysAsync(
    Guid companyId,
    CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            return;

        var gateways =
            await _apiClient.GetGatewaysByCompanyIdAsync(
                companyId,
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
            var gateway = _mapper.Map<Gateway>(dto);

            await _gatewayCache.UpdateAsync(
                gateway,
                cancellationToken);
        }
    }

    public async Task SyncUsersAsync(
    Guid companyId,
    CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            return;

        var users =
            await _apiClient.GetUsersByCompanyIdAsync(
                companyId,
                cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        var cachedUsers =
            await _userCache.GetByCompanyIdAsync(
                companyId,
                cancellationToken);

        // UPDATE / INSERT
        foreach (var dto in users)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var cached =
                await _userCache.GetByIdAsync(
                    dto.Id,
                    cancellationToken);

            if (cached is not null &&
                dto.UpdatedAt <= cached.UpdatedAt)
            {
                continue;
            }

            await _userCache.UpdateAsync(
                dto,
                cancellationToken);
        }

        // DELETE STALE USERS
        var apiUserIds =
            users
                .Select(x => x.Id)
                .ToHashSet();

        foreach (var cachedUser in cachedUsers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!apiUserIds.Contains(cachedUser.Id))
            {
                await _userCache.DeleteAsync(
                    cachedUser.Id,
                    cancellationToken);
            }
        }
    }

    public async Task<CompanyDto?> GetCompanyAsync(
    Guid companyId,
    CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            return null;

        var cached =
            await _companyCache.GetByIdAsync(
                companyId,
                cancellationToken);

        if (cached is not null)
        {
            return _mapper.Map<CompanyDto>(cached);
        }

        var dto =
            await _apiClient.GetCompanyByIdAsync(
                companyId,
                cancellationToken);

        if (dto is null)
            return null;

        cancellationToken.ThrowIfCancellationRequested();

        var company =
            _mapper.Map<Company>(dto);

        await _companyCache.UpdateAsync(
            company,
            cancellationToken);

        return dto;
    }



}
