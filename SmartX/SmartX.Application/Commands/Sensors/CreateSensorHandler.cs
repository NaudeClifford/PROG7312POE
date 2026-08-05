using SmartX.Application.Common;
using SmartX.Domain.Entities;
using SmartX.Domain.Interfaces;

namespace SmartX.Application.Commands.Sensors;

public class CreateSensorHandler
{
    private readonly ISensorRepository _sensorRepository;

    public CreateSensorHandler(ISensorRepository sensorRepository)
    {
        _sensorRepository = sensorRepository;
    }

    public async Task<Result<Guid>> HandleAsync(
        CreateSensorCommand command,
        CancellationToken cancellationToken = default)
    {
        var sensor = new Sensor
        {
            Id = Guid.NewGuid(),
            Name = command.Name,
            Location = command.Location,
            Description = command.Description,
            GatewayId = command.GatewayId,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _sensorRepository.AddAsync(
            sensor,
            cancellationToken);

        return Result<Guid>.Ok(sensor.Id);
    }
}