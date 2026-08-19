using FluentValidation;
using SmartX.Shared.Models;
using SmartX.Domain.Interfaces;

namespace SmartX.Application.Commands.Sensors;

public class UpdateGatewayHandler
{
    private readonly ISensorRepository _sensorRepository;
    private readonly IValidator<UpdateCompanyCommand> _validator;

    public UpdateGatewayHandler(
        ISensorRepository sensorRepository,
        IValidator<UpdateCompanyCommand> validator)
    {
        _sensorRepository = sensorRepository;
        _validator = validator;
    }

    public async Task<Result<bool>> HandleAsync(
        UpdateCompanyCommand command,
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

        sensor.UpdatedAt = DateTime.UtcNow;

        await _sensorRepository.UpdateAsync(
            sensor,
            cancellationToken);

        return Result<bool>.Ok(true);
    }
}