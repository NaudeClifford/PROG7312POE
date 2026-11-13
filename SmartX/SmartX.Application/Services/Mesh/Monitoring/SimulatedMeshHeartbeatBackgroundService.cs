
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartX.Domain.Interfaces;
using SmartX.Domain.Interfaces.Mesh;

namespace SmartX.Application.Services.Mesh.Monitoring;

public class SimulatedMeshHeartbeatBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SimulatedMeshHeartbeatBackgroundService> _logger;

    public SimulatedMeshHeartbeatBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<SimulatedMeshHeartbeatBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Simulated mesh heartbeat service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var sensorRepository =
                    scope.ServiceProvider.GetRequiredService<ISensorRepository>();

                var heartbeatService =
                    scope.ServiceProvider.GetRequiredService<IMeshSensorHeartbeatService>();

                var sensors = await sensorRepository.GetAllAsync(
                    stoppingToken);

                var activeSensors = sensors
                    .Where(x =>
                        x.IsActive &&
                        x.GatewayId.HasValue &&
                        x.GatewayId.Value != Guid.Empty)
                    .ToList();

                foreach (var sensor in activeSensors)
                {
                    stoppingToken.ThrowIfCancellationRequested();

                    try
                    {
                        await heartbeatService.ProcessHeartbeatAsync(
                            sensor.GatewayId!.Value,
                            sensor.Id,
                            stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Failed simulated heartbeat for sensor {SensorId}.",
                            sensor.Id);
                    }
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Simulated heartbeat cycle failed.");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(15),
                stoppingToken);
        }
    }
}
