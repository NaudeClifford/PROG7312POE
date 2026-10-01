using SmartX.Application.Commands.Sensor;
using SmartX.Application.Requests.Sensor;
using SmartX.Application.Services.Sensors;
using SmartX.WPF.Repositories.Local;

namespace SmartX.WPF.Services;

public sealed class SensorCommandService : ISensorCommandService
{
    private readonly SendSensorCommandHandler _handler;
    private readonly ILocalSensorCache _sensorCache;
    private readonly ILocalGatewayCache _gatewayCache;

    public SensorCommandService(
        SendSensorCommandHandler handler,
        ILocalSensorCache sensorCache,
        ILocalGatewayCache gatewayCache)
    {
        _handler = handler;
        _sensorCache = sensorCache;
        _gatewayCache = gatewayCache;
    }

    public async Task<SensorCommandResult> SendCommandAsync(
        Guid sensorId,
        string command,
        CancellationToken cancellationToken = default)
    {
        if (sensorId == Guid.Empty)
        {
            return SensorCommandResult.Fail(
                "Sensor ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command))
        {
            return SensorCommandResult.Fail(
                "Command is required.");
        }

        var sensor =
            await _sensorCache.GetByIdAsync(
                sensorId,
                cancellationToken);

        if (sensor is null)
        {
            return SensorCommandResult.Fail(
                "Sensor not found.");
        }

        if (!sensor.GatewayId.HasValue)
        {
            return SensorCommandResult.Fail(
                "Sensor is not associated with a gateway.");
        }

        var gateway =
            await _gatewayCache.GetByIdAsync(
                sensor.GatewayId.Value,
                cancellationToken);

        if (gateway is null)
        {
            return SensorCommandResult.Fail(
                "Gateway not found.");
        }

        var result =
            await _handler.HandleAsync(
                new SendSensorCommandRequest
                {
                    SensorId = sensorId,
                    Command = command.Trim()
                },
                cancellationToken);

        if (!result.Success)
        {
            return SensorCommandResult.Fail(
                result.Error ??
                "The command could not be executed.");
        }

        return SensorCommandResult.Ok(
            result.Data?.Response,
            result.Data?.InverseCommand);
    }
}