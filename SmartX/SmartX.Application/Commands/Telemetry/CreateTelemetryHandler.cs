using FluentValidation;
using SmartX.Application.Requests.Telemetry;
using SmartX.Domain.Interfaces;
using SmartX.Shared.Models;

namespace SmartX.Application.Commands.Telemetry;

public class CreateTelemetryHandler
{
    private readonly ITelemetryRepository _telemetryRepository;
    private readonly ISensorRepository _sensorRepository;
    private readonly IValidator<CreateTelemetryRequest> _validator;

    public CreateTelemetryHandler(
        ITelemetryRepository telemetryRepository,
        ISensorRepository sensorRepository,
        IValidator<CreateTelemetryRequest> validator)
    {
        _telemetryRepository = telemetryRepository;
        _sensorRepository = sensorRepository;
        _validator = validator;
    }

    public async Task<Result<Guid>> HandleAsync(
        CreateTelemetryRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult =
            await _validator.ValidateAsync(
                request,
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
                request.SensorId,
                cancellationToken);

        if (sensor is null)
        {
            return Result<Guid>.Fail(
                "Sensor not found.");
        }

        var now = DateTime.UtcNow;

        var telemetry =
            new SmartX.Domain.Entities.Telemetry
            {
                Id = Guid.NewGuid(),

                SensorId =
                    request.SensorId,

                Timestamp =
                    request.Timestamp,

                Voltage =
                    request.Voltage,

                Current =
                    request.Current,

                Power =
                    request.Power,

                Temperature =
                    request.Temperature,

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
