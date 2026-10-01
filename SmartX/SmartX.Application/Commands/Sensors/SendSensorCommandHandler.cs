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

        var response =
            new SensorCommandResponse
            {
                Response = "OK",
                InverseCommand = GetInverseCommand(command)
            };

        return Result<SensorCommandResponse>.Ok(response);
    }

    private static string? GetInverseCommand(
        string command)
    {
        return command.ToUpperInvariant() switch
        {
            "POWER ON" => "POWER OFF",
            "POWER OFF" => "POWER ON",
            _ => null
        };
    }
}