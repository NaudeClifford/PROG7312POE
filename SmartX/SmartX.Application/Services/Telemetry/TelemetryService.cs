using System.Security.Claims;
using SmartX.Shared.Mapping;
using FluentValidation;
using SmartX.Application.Requests.Telemetry;
using DomainTelemetry = SmartX.Domain.Entities.Telemetry;
using SmartX.Domain.Interfaces;
using SmartX.Shared.DTOs.Telemetry;
using SmartX.Shared.Models;

namespace SmartX.Application.Services.Telemetry;

public class TelemetryService
{
    private readonly ITelemetryRepository _repository;
    private readonly ISensorRepository _sensorRepository;
    private readonly IGatewayRepository _gatewayRepository;
    private readonly ITelemetryStream _telemetryStream;


    private readonly IValidator<CreateTelemetryRequest>
        _validator;

    public TelemetryService(
        ITelemetryRepository repository,
        ISensorRepository sensorRepository,
        IGatewayRepository gatewayRepository,
        IValidator<CreateTelemetryRequest> validator,
        ITelemetryStream telemetryStream)
    {
        _repository = repository;
        _sensorRepository = sensorRepository;
        _gatewayRepository = gatewayRepository;
        _validator = validator;
        _telemetryStream = telemetryStream;
    }

    // CREATE
    public async Task<Result<Guid>> CreateAsync(
        CreateTelemetryRequest request,
        ClaimsPrincipal user,
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

        if (!sensor.GatewayId.HasValue)
        {
            return Result<Guid>.Fail(
                "Sensor is not associated with a gateway.");
        }

        var gateway =
            await _gatewayRepository.GetByIdAsync(
                sensor.GatewayId.Value,
                cancellationToken);

        if (gateway is null)
        {
            return Result<Guid>.Fail(
                "Gateway not found.");
        }

        if (!CanAccessCompany(
                user,
                gateway.CompanyId))
        {
            return Result<Guid>.Fail(
                "You do not have access to this telemetry.");
        }

        var now = DateTime.UtcNow;

        var telemetry = new DomainTelemetry
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


        _telemetryStream.Enqueue(telemetry);

        return Result<Guid>.Ok(
            telemetry.Id);
    }

    // COMPANY ACCESS
    private static bool CanAccessCompany(
        ClaimsPrincipal user,
        Guid companyId)
    {
        if (companyId == Guid.Empty)
            return false;

        if (!user.IsInRole("Administrator") &&
            !user.IsInRole("Technician"))
        {
            return false;
        }

        var claim =
            user.FindFirst("CompanyId")?.Value;

        if (!Guid.TryParse(
                claim,
                out var userCompanyId))
        {
            return false;
        }

        return userCompanyId == companyId;
    }
}
