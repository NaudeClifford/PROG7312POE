using SmartX.Application.Common;
using SmartX.Domain.Interfaces;

namespace SmartX.Application.Commands.Sensors;

public class UpdateSensorHandler
{
    private readonly ISensorRepository _sensorRepository;

    public UpdateSensorHandler(ISensorRepository sensorRepository)
    {
        _sensorRepository = sensorRepository;
    }

    public async Task<Result<bool>> HandleAsync(
        UpdateSensorCommand command,
        CancellationToken cancellationToken = default)
    {
        var sensor = await _sensorRepository.GetByIdAsync(
            command.Id,
            cancellationToken);

        if (sensor is null)
        {
            return Result<bool>.Fail("Sensor not found.");
        }

        sensor.Name = command.Name;
        sensor.Location = command.Location;
        sensor.Description = command.Description;
        sensor.GatewayId = command.GatewayId;
        sensor.IsActive = command.IsActive;

        await _sensorRepository.UpdateAsync(
            sensor,
            cancellationToken);

        return Result<bool>.Ok(true);
    }
}