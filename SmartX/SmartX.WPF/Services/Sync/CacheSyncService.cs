using SmartX.WPF.Repositories.Local;
using SmartX.WPF.Services.Api;

namespace SmartX.WPF.Services.Sync;

public class CacheSyncService(
    ISmartXApiClient apiClient,
    ILocalSensorCache sensorCache) : ICacheSyncService
{
    private readonly ISmartXApiClient _apiClient = apiClient;
    private readonly ILocalSensorCache _sensorCache = sensorCache;

    public async Task SyncSensorsAsync(
        CancellationToken cancellationToken = default)
    {
        var sensors = await _apiClient.GetSensorsAsync(
            cancellationToken);

        foreach (var sensor in sensors)
        {
            await _sensorCache.UpdateAsync(
                sensor,
                cancellationToken);
        }
    }
}