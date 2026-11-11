using SmartX.Application.Services.Sensors.Command;

namespace SmartX.Application.Services.Sensors.Interfaces;

public interface ISensorCommandService
{
    Task<SensorCommandResult> SendCommandAsync(
        Guid sensorId,
        string command,
        CancellationToken cancellationToken = default);

    Task<SensorCommandResult> UndoLastCommandAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default);
}
