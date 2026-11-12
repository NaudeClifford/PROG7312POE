using Microsoft.Extensions.Hosting;
using SmartX.Application.Services.Telemetry.Structures;
using SmartX.Domain.Interfaces;

namespace SmartX.Application.Services.Telemetry;

public sealed class TelemetryHistoryInitializer(
    ITelemetryRepository telemetryRepository,
    ISensorRepository sensorRepository,
    TelemetryHistoryStore historyStore,
    TelemetryAvlTreeService avlTreeService) : IHostedService
{
    public async Task StartAsync(
        CancellationToken cancellationToken)
    {
        var sensors = await sensorRepository.GetAllAsync(
            cancellationToken);

        foreach (var sensor in sensors)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var telemetry =
                await telemetryRepository.GetBySensorIdAsync(
                    sensor.Id,
                    cancellationToken);

            historyStore.AddRange(telemetry);
            avlTreeService.AddRange(telemetry);
        }
    }

    public Task StopAsync(
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}