using FluentValidation;
using SmartX.Application.Common;
using SmartX.Domain.Entities;
using SmartX.Domain.Interfaces;

namespace SmartX.Application.Commands.Sensors;

public class CreateSensorHandler
{
    private readonly ISensorRepository _sensorRepository;
    private readonly IValidator<CreateSensorCommand> _validator;

    public CreateSensorHandler(
        ISensorRepository sensorRepository,
        IValidator<CreateSensorCommand> validator)
    {
        _sensorRepository = sensorRepository;
        _validator = validator;
    }

    public async Task<Result<Guid>> HandleAsync(
        CreateSensorCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(
            command, cancellationToken);

        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult
                .Errors.Select(x => x.ErrorMessage));

            return Result<Guid>.Fail(errors);
        }

        var sensor = new Sensor
        {
            Id = Guid.NewGuid(),
            Name = command.Name,
            Location = command.Location,
            DeviceIdentifier = command.DeviceIdentifier,
            Description = command.Description,
            Category = command.Category,
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