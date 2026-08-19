using FluentValidation;
using SmartX.Domain.Entities;
using SmartX.Domain.Interfaces;
using SmartX.Shared.Models;

namespace SmartX.Application.Commands.Sensors;

public class CreateGatewayHandler
{
    private readonly ISensorRepository _sensorRepository;
    private readonly IValidator<CreateCompanyCommand> _validator;

    public CreateGatewayHandler(
        ISensorRepository sensorRepository,
        IValidator<CreateCompanyCommand> validator)
    {
        _sensorRepository = sensorRepository;
        _validator = validator;
    }

    public async Task<Result<Guid>> HandleAsync(
        CreateCompanyCommand command,
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
        var now = DateTime.UtcNow;

        var sensor = new Sensor
        {
            Id = Guid.NewGuid(),
            Name = command.Name,
            Location = command.Location,
            DeviceIdentifier = command.DeviceIdentifier,
            Description = command.Description,
            Category = command.Category,
            GatewayId = command.GatewayId,
            CreatedAt = now,
            UpdatedAt = now,
            IsActive = true
        };

        await _sensorRepository.AddAsync(
            sensor,
            cancellationToken);

        return Result<Guid>.Ok(sensor.Id);
    }
}