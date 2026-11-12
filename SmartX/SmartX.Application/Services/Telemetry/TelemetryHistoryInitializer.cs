
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartX.Application.Services.Telemetry.Structures;
using SmartX.Domain.Interfaces;

namespace SmartX.Application.Services.Telemetry;

public sealed class TelemetryHistoryInitializer(
    IServiceScopeFactory scopeFactory,
    TelemetryHistoryStore historyStore,
    TelemetryAvlTreeService avlTreeService) : IHostedService
{

    public async Task StartAsync(
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();

        var telemetryRepository =
            scope.ServiceProvider.GetRequiredService<ITelemetryRepository>();

        var sensorRepository =
            scope.ServiceProvider.GetRequiredService<ISensorRepository>();

        var sensors = await sensorRepository.GetAllAsync(
            cancellationToken);

        foreach (var sensor in sensors)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Skip invalid sensor IDs.
            if (sensor.Id == Guid.Empty)
                continue;

            try
            {
                var telemetry =
                    await telemetryRepository.GetBySensorIdAsync(
                        sensor.Id,
                        cancellationToken);

                if (telemetry.Count == 0)
                    continue;

                historyStore.AddRange(telemetry);
                avlTreeService.AddRange(telemetry);
            }
            catch (ArgumentException ex)
            {
                // Log invalid sensor data without stopping startup.
                System.Diagnostics.Debug.WriteLine(
                    $"Skipping sensor {sensor.Id}: {ex.Message}");
            }
        }
    }


    public Task StopAsync(
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
