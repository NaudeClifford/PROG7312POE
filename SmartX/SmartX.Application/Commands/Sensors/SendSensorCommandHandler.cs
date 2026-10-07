using SmartX.Application.Commands.Sensors;
using SmartX.Application.Requests.Sensor;
using SmartX.Shared.Models;

namespace SmartX.Application.Commands.Sensor;

public class SendSensorCommandHandler
{
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

        var command = request.Command.Trim();

        var inverseCommand = GetInverseCommand(command);

        if (!IsSupportedCommand(command))
        {
            return Result<SensorCommandResponse>.Fail(
                $"Unsupported sensor command: {command}");
        }
        var response =
            new SensorCommandResponse
            {
                Response = $"Command '{command}' executed successfully.",
                InverseCommand = inverseCommand
            };

        return Result<SensorCommandResponse>.Ok(response);
    }

    private static string? GetInverseCommand(string command)
    {
        return command.ToUpperInvariant() switch
        {
            "POWER ON" => "POWER OFF",
            "POWER OFF" => "POWER ON",

            "START" => "STOP",
            "STOP" => "START",

            "ENABLE" => "DISABLE",
            "DISABLE" => "ENABLE",

            "OPEN" => "CLOSE",
            "CLOSE" => "OPEN",

            "LOCK" => "UNLOCK",
            "UNLOCK" => "LOCK",

            "ACTIVATE" => "DEACTIVATE",
            "DEACTIVATE" => "ACTIVATE",

            "RESET" => null,

            _ => null
        };
    }

    private static bool IsSupportedCommand(string command)
    {
        return command.ToUpperInvariant() switch
        {
            "POWER ON" => true,
            "POWER OFF" => true,
            "START" => true,
            "STOP" => true,
            "ENABLE" => true,
            "DISABLE" => true,
            "OPEN" => true,
            "CLOSE" => true,
            "LOCK" => true,
            "UNLOCK" => true,
            "ACTIVATE" => true,
            "DEACTIVATE" => true,
            "RESET" => true,
            _ => false
        };
    }
}