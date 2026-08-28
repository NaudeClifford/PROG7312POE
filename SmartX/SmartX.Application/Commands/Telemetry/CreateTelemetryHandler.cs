using FluentValidation;
using SmartX.Domain.Interfaces;
using SmartX.Shared.Models;

namespace SmartX.Application.Commands.Telemetry;

public class CreateTelemetryHandler
{
    private readonly ITelemetryRepository _telemetryRepository;
    private readonly ISensorRepository _sensorRepository;
    private readonly IValidator<CreateTelemetryCommand> _validator;

    public CreateTelemetryHandler(
        ITelemetryRepository telemetryRepository,
        ISensorRepository sensorRepository,
        IValidator<CreateTelemetryCommand> validator)
    {
        _telemetryRepository = telemetryRepository;
        _sensorRepository = sensorRepository;
        _validator = validator;
    }

    public async Task<Result<Guid>> HandleAsync(
        CreateTelemetryCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult =
            await _validator.ValidateAsync(
                command,
                cancellationToken);

        if (!validationResult.IsValid)
        {
            var errors = string.Join(
                "; ",
                validationResult.Errors
                    .Select(x => x.ErrorMessage));

            return Result<Guid>.Fail(errors);
        }

        var sensor =
            await _sensorRepository.GetByIdAsync(
                command.SensorId,
                cancellationToken);

        if (sensor is null)
        {
            return Result<Guid>.Fail(
                "Sensor not found.");
        }

        var now = DateTime.UtcNow;

        var telemetry = new Domain.Entities.Telemetry
        {
            Id = Guid.NewGuid(),
            SensorId = command.SensorId,
            Timestamp = command.TimeStamp,
            Voltage = command.Voltage,
            Current = command.Current,
            Power = command.Power,
            Temperature = command.Temperature,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _telemetryRepository.AddAsync(
            telemetry,
            cancellationToken);

        return Result<Guid>.Ok(
            telemetry.Id);
    }
}
