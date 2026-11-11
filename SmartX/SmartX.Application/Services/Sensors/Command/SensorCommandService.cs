using SmartX.Application.Commands.Sensor;
using SmartX.Application.Requests.Sensor;
using SmartX.Application.Services.Sensors.Interfaces;
using SmartX.Domain.Interfaces;

namespace SmartX.Application.Services.Sensors.Command;

public sealed class SensorCommandService : ISensorCommandService
{
    private readonly SendSensorCommandHandler _handler;
    private readonly ISensorRepository _sensorRepository;
    private readonly IGatewayRepository _gatewayRepository;
    private readonly ManualStack<SensorCommand> _commandStack;

    private readonly object _stackLock = new();

    // Serialises access to the shared in-memory stack.
    private static readonly SemaphoreSlim CommandGate = new(1, 1);

    public SensorCommandService(
        SendSensorCommandHandler handler,
        ISensorRepository sensorRepository,
        IGatewayRepository gatewayRepository,
        ManualStack<SensorCommand> commandStack)
    {
        _handler = handler;
        _sensorRepository = sensorRepository;
        _gatewayRepository = gatewayRepository;
        _commandStack = commandStack;
    }

    public async Task<SensorCommandResult> SendCommandAsync(
        Guid sensorId,
        string command,
        CancellationToken cancellationToken = default)
    {
        await CommandGate.WaitAsync(cancellationToken);

        try
        {
            if (sensorId == Guid.Empty)
                return SensorCommandResult.Fail("Sensor ID is required.");

            if (string.IsNullOrWhiteSpace(command))
                return SensorCommandResult.Fail("Command is required.");

            var sensor = await _sensorRepository.GetByIdAsync(
                sensorId, cancellationToken);

            if (sensor is null)
                return SensorCommandResult.Fail("Sensor not found.");

            if (!sensor.GatewayId.HasValue)
                return SensorCommandResult.Fail(
                    "Sensor is not associated with a gateway.");

            var gateway = await _gatewayRepository.GetByIdAsync(
                sensor.GatewayId.Value, cancellationToken);

            if (gateway is null)
                return SensorCommandResult.Fail("Gateway not found.");

            var commandText = command.Trim();

            var result = await _handler.HandleAsync(
                new SendSensorCommandRequest
                {
                    SensorId = sensorId,
                    Command = commandText
                },
                cancellationToken);

            if (!result.Success)
                return SensorCommandResult.Fail(
                    result.Error ?? "The command could not be executed.");

            var inverseCommand = result.Data?.InverseCommand;

            // Only successful, reversible commands enter the stack.
            if (!string.IsNullOrWhiteSpace(inverseCommand))
            {
                lock (_stackLock)
                {
                    _commandStack.Push(new SensorCommand
                    {
                        SensorId = sensorId,
                        Command = commandText,
                        InverseCommand = inverseCommand,
                        Response = result.Data?.Response,
                        Timestamp = DateTime.UtcNow
                    });
                }
            }

            return SensorCommandResult.Ok(
                result.Data?.Response,
                inverseCommand);
        }
        finally
        {
            CommandGate.Release();
        }
    }

    public async Task<SensorCommandResult> UndoLastCommandAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default)
    {
        await CommandGate.WaitAsync(cancellationToken);

        try
        {
            if (sensorId == Guid.Empty)
                return SensorCommandResult.Fail("Sensor ID is required.");

            SensorCommand? command;

            // Find the latest reversible command for this sensor.
            lock (_stackLock)
            {
                command = _commandStack.PeekLast(
                    x => x.SensorId == sensorId &&
                         !string.IsNullOrWhiteSpace(x.InverseCommand));
            }

            if (command is null)
            {
                return SensorCommandResult.Fail(
                    "No reversible command is available for this sensor.");
            }

            var sensor = await _sensorRepository.GetByIdAsync(
                sensorId, cancellationToken);

            if (sensor is null)
                return SensorCommandResult.Fail("Sensor not found.");

            if (!sensor.GatewayId.HasValue)
                return SensorCommandResult.Fail(
                    "Sensor is not associated with a gateway.");

            var gateway = await _gatewayRepository.GetByIdAsync(
                sensor.GatewayId.Value, cancellationToken);

            if (gateway is null)
                return SensorCommandResult.Fail("Gateway not found.");

            // Execute the inverse command.
            var result = await _handler.HandleAsync(
                new SendSensorCommandRequest
                {
                    SensorId = sensorId,
                    Command = command.InverseCommand!
                },
                cancellationToken);

            if (!result.Success)
            {
                return SensorCommandResult.Fail(
                    result.Error ?? "Undo command failed.");
            }

            // Remove the original command only after successful execution.
            lock (_stackLock)
            {
                _commandStack.Remove(command);
            }

            return SensorCommandResult.Ok(
                result.Data?.Response,
                command.Command);
        }
        finally
        {
            CommandGate.Release();
        }
    }

}
