using SmartX.Application.Commands.Sensor;
using SmartX.Application.Requests.Sensor;
using SmartX.Application.Services.Sensors;
using SmartX.Shared.DTOs;
using SmartX.WPF.Repositories.Local;
using SmartX.WPF.Services.Session;

namespace SmartX.WPF.Services;

public sealed class SensorCommandService : ISensorCommandService
{
    private readonly SendSensorCommandHandler _handler;
    private readonly ILocalSensorCache _sensorCache;
    private readonly ILocalGatewayCache _gatewayCache;
    private readonly ILocalUserBehaviorCache _behaviorCache;
    private readonly SmartXSession _session;

    public SensorCommandService(
        SendSensorCommandHandler handler,
        ILocalSensorCache sensorCache,
        ILocalGatewayCache gatewayCache,
        ILocalUserBehaviorCache behaviorCache,
        SmartXSession session)
    {
        _handler = handler;
        _sensorCache = sensorCache;
        _gatewayCache = gatewayCache;
        _behaviorCache = behaviorCache;
        _session = session;
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

        var succeeded = result.Success;

        await _behaviorCache.AddAsync(
            new UserBehaviorDto
            {
                UserId = _session.UserId,
                SensorId = sensorId,
                Command = command.Trim(),
                Timestamp = DateTime.UtcNow,
                Succeeded = succeeded,
                Outcome = succeeded
                    ? result.Data?.Response
                    : result.Error ?? "Command execution failed."
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