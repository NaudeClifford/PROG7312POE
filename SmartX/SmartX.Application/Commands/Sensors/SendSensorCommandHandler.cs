using SmartX.Application.Commands.Sensors;
using SmartX.Application.Requests.Sensor;
using SmartX.Domain.Interfaces;
using SmartX.Shared.Models;

namespace SmartX.Application.Commands.Sensor;

public class SendSensorCommandHandler
{
    private readonly ISensorRepository _sensorRepository;

    public SendSensorCommandHandler(
        ISensorRepository sensorRepository)
    {
        _sensorRepository = sensorRepository;
    }

    public async Task<Result<SensorCommandResponse>> HandleAsync(
        SendSensorCommandRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.SensorId == Guid.Empty)
        {
            return Result<SensorCommandResponse>.Fail(
                "Sensor ID is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Command))
        {
            return Result<SensorCommandResponse>.Fail(
                "Command is required.");
        }

        var sensor =
            await _sensorRepository.GetByIdAsync(
                request.SensorId,
                cancellationToken);

        if (sensor is null)
        {
            return Result<SensorCommandResponse>.Fail(
                "Sensor not found.");
        }

        var command =
            request.Command.Trim();

        // TODO:
        // Actual sensor/gateway command execution
        // will go here.

        var response =
            new SensorCommandResponse
            {
                Response = "OK",

                InverseCommand =
                    GetInverseCommand(command)
            };

        return Result<SensorCommandResponse>.Ok(
            response);
    }

    private static string? GetInverseCommand(
        string command)
    {
        return command.ToUpperInvariant() switch
        {
            "POWER ON" =>
                "POWER OFF",

            "POWER OFF" =>
                "POWER ON",

            _ =>
                null
        };
    }
}
