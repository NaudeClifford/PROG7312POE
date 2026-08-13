using FluentValidation;
using SmartX.Shared.Models;
using SmartX.Domain.Interfaces;

namespace SmartX.Application.Commands.Sensors;

public class UpdateSensorHandler
{
    private readonly ISensorRepository _sensorRepository;
    private readonly IValidator<UpdateSensorCommand> _validator;

    public UpdateSensorHandler(
        ISensorRepository sensorRepository,
        IValidator<UpdateSensorCommand> validator)
    {
        _sensorRepository = sensorRepository;
        _validator = validator;
    }

    public async Task<Result<bool>> HandleAsync(
        UpdateSensorCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(
            command,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            var errors = string.Join(
                "; ",
                validationResult.Errors.Select(x => x.ErrorMessage));

            return Result<bool>.Fail(errors);
        }

        var sensor = await _sensorRepository.GetByIdAsync(
            command.Id,
            cancellationToken);

        if (sensor is null)
        {
            return Result<bool>.Fail("Sensor not found.");
        }

        sensor.Name = command.Name;
        sensor.Location = command.Location;
        sensor.Category = command.Category;
        sensor.DeviceIdentifier = command.DeviceIdentifier;
        sensor.Description = command.Description;
        sensor.GatewayId = command.GatewayId;
        sensor.IsActive = command.IsActive;

        await _sensorRepository.UpdateAsync(
            sensor,
            cancellationToken);

        return Result<bool>.Ok(true);
    }
}