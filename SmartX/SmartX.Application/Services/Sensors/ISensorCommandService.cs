namespace SmartX.Application.Services.Sensors;

public interface ISensorCommandService
{
    Task<SensorCommandResult> SendCommandAsync(
        Guid sensorId,
        string command,
        CancellationToken cancellationToken = default);
}
