
using Microsoft.Extensions.Logging;
using SmartX.Application.Interfaces.Sensors;
using SmartX.Application.Requests.Telemetry;
using SmartX.Application.Services.Telemetry;
using SmartX.Domain.Interfaces;
using System.Security.Claims;

namespace SmartX.Application.Services.Sensors;

public class SensorProcessingService
{
    private readonly ISensorRepository _sensorRepository;
    private readonly IGatewayRepository _gatewayRepository;
    private readonly ISensorConnectionService _connectionService;
    private readonly TelemetryService _telemetryService;
    private readonly ILogger<SensorProcessingService> _logger;

    public SensorProcessingService(
        ISensorRepository sensorRepository,
        IGatewayRepository gatewayRepository,
        ISensorConnectionService connectionService,
        TelemetryService telemetryService,
        ILogger<SensorProcessingService> logger)
    {
        _sensorRepository = sensorRepository;
        _gatewayRepository = gatewayRepository;
        _connectionService = connectionService;
        _telemetryService = telemetryService;
        _logger = logger;
    }

    public async Task ProcessAsync(
        CancellationToken cancellationToken = default)
    {
        var sensors = await _sensorRepository.GetAllAsync(
            cancellationToken);

        var enabledSensors = sensors.Where(x =>
            x.IsActive &&
            x.IsConnectionEnabled).ToList();

        foreach (var sensor in enabledSensors)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (!sensor.GatewayId.HasValue)
                {
                    _logger.LogWarning(
                        "Sensor {SensorId} has no gateway.",
                        sensor.Id);

                    continue;
                }

                var gateway = await _gatewayRepository.GetByIdAsync(
                    sensor.GatewayId.Value,
                    cancellationToken);

                if (gateway is null ||
                    gateway.CompanyId == Guid.Empty)
                {
                    _logger.LogWarning(
                        "Gateway or company not found for sensor {SensorId}.",
                        sensor.Id);

                    continue;
                }

                // Start and test connection.
                await _connectionService.StartAsync(
                    sensor,
                    cancellationToken);

                // Collect sensor reading.
                var reading = await _connectionService.ReadAsync(
                    sensor,
                    cancellationToken);

                if (reading is null)
                {
                    _logger.LogWarning(
                        "No reading received from sensor {SensorId}.",
                        sensor.Id);

                    continue;
                }

                // Use the existing telemetry request.
                var request = new CreateTelemetryRequest
                {
                    SensorId = sensor.Id,

                    DeviceIdentifier =
                        string.IsNullOrWhiteSpace(reading.DeviceIdentifier)
                            ? sensor.DeviceIdentifier
                            : reading.DeviceIdentifier,

                    Timestamp = reading.Timestamp,

                    Voltage = reading.Voltage,
                    Current = reading.Current,
                    Power = reading.Power,
                    Temperature = reading.Temperature
                };

                // Trusted background identity.
                var claims = new[]
                {
                    new Claim(
                        ClaimTypes.Role,
                        "Technician"),

                    new Claim(
                        "CompanyId",
                        gateway.CompanyId.ToString())
                };

                var identity = new ClaimsIdentity(
                    claims,
                    "SmartXBackgroundService");

                var principal = new ClaimsPrincipal(identity);

                // Reuse the existing telemetry pipeline.
                var result = await _telemetryService.CreateAsync(
                    request,
                    principal,
                    cancellationToken);

                if (!result.Success)
                {
                    _logger.LogWarning(
                        "Telemetry failed for sensor {SensorId}: {Error}",
                        sensor.Id,
                        result.Error);

                    continue;
                }

                _logger.LogInformation(
                    "Telemetry successfully collected from sensor {SensorId}.",
                    sensor.Id);
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
                    "Failed to process sensor {SensorId}.",
                    sensor.Id);
            }
        }
    }
}
