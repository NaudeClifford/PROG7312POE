using AutoMapper;
using FluentValidation;
using SmartX.Application.Queries.Telemetry;
using SmartX.Application.Requests.Telemetry;
using SmartX.Domain.Entities;
using SmartX.Domain.Interfaces;
using SmartX.Shared.DTOs.Telemetry;
using SmartX.Shared.Models;

namespace SmartX.Application.Services;

public class TelemetryService
{
    private readonly ITelemetryRepository _repository;
    private readonly ISensorRepository _sensorRepository;
    private readonly IValidator<CreateTelemetryRequest> _validator;

    public TelemetryService(
        ITelemetryRepository repository,
        ISensorRepository sensorRepository,
        IValidator<CreateTelemetryRequest> validator)
    {
        _repository = repository;
        _sensorRepository = sensorRepository;
        _validator = validator;
    }

    public async Task<Result<Guid>> CreateAsync(
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

        var telemetry = new Telemetry
        {
            Id = Guid.NewGuid(),

            SensorId = request.SensorId,

            Timestamp = request.Timestamp,

            Voltage = request.Voltage,
            Current = request.Current,
            Power = request.Power,
            Temperature = request.Temperature,

            CreatedAt = now,
            UpdatedAt = now
        };

        await _repository.AddAsync(
            telemetry,
            cancellationToken);

        return Result<Guid>.Ok(
            telemetry.Id);
    }
}
