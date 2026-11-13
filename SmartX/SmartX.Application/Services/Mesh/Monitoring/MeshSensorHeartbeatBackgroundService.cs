using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartX.Domain.Interfaces;
using SmartX.Domain.Interfaces.Mesh;

namespace SmartX.Application.Services.Mesh.Monitoring;

public class MeshSensorHeartbeatBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MeshSensorMonitoringOptions _options;
    private readonly ILogger<MeshSensorHeartbeatBackgroundService> _logger;

    public MeshSensorHeartbeatBackgroundService(
        IServiceScopeFactory scopeFactory,
        MeshSensorMonitoringOptions options,
        ILogger<MeshSensorHeartbeatBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Mesh sensor heartbeat monitoring started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EvaluateGatewaysAsync(stoppingToken);
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
                    "Mesh heartbeat monitoring cycle failed.");
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(
                        _options.MonitoringIntervalSeconds),
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation(
            "Mesh sensor heartbeat monitoring stopped.");
    }

    private async Task EvaluateGatewaysAsync(
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var sensorRepository =
            scope.ServiceProvider.GetRequiredService<ISensorRepository>();

        var linkMonitoringService =
            scope.ServiceProvider.GetRequiredService<IMeshLinkMonitoringService>();

        var heartbeatService =
            scope.ServiceProvider.GetRequiredService<
                IMeshSensorHeartbeatService>();

        var sensors = await sensorRepository.GetAllAsync(
            cancellationToken);

        var gatewayIds = sensors
            .Where(x => x.GatewayId.HasValue &&
                        x.GatewayId.Value != Guid.Empty)
            .Select(x => x.GatewayId!.Value)
            .Distinct()
            .ToList();

        foreach (var gatewayId in gatewayIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {

                await heartbeatService.EvaluateSensorConnectivityAsync(
                    gatewayId,
                    cancellationToken);

                await linkMonitoringService.EvaluateGatewayLinksAsync(
                    gatewayId,
                    cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to evaluate sensor connectivity for gateway {GatewayId}.",
                    gatewayId);
            }
        }
    }
}