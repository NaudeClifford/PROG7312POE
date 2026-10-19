using SmartX.Application.Commands.Sensors;
using SmartX.Application.Requests.Sensor;
using SmartX.Domain.shared;
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

        var inverseCommand = SensorCommandCatalog.GetInverseCommand(command);

        if (!SensorCommandCatalog.IsSupported(command))
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

}