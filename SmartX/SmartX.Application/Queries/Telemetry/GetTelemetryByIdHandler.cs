using SmartX.Shared.Mapping;
using SmartX.Domain.Interfaces;
using SmartX.Shared.DTOs.Telemetry;
using SmartX.Shared.Models;
using System.Security.Claims;

namespace SmartX.Application.Queries.Telemetry;

public class GetTelemetryByIdHandler
{
    private readonly ITelemetryRepository _telemetryRepository;
    private readonly ISensorRepository _sensorRepository;
    private readonly IGatewayRepository _gatewayRepository;
    private readonly IMapper _mapper;

    public GetTelemetryByIdHandler(
        ITelemetryRepository telemetryRepository,
        ISensorRepository sensorRepository,
        IGatewayRepository gatewayRepository,
        IMapper mapper)
    {
        _telemetryRepository = telemetryRepository;
        _sensorRepository = sensorRepository;
        _gatewayRepository = gatewayRepository;
        _mapper = mapper;
    }

    public async Task<Result<TelemetryDto?>> HandleAsync(
        GetTelemetryByIdQuery query,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (query.TelemetryId == Guid.Empty)
        {
            return Result<TelemetryDto?>.Fail(
                "Telemetry ID is required.");
        }

        var telemetry =
            await _telemetryRepository.GetByIdAsync(
                query.TelemetryId,
                cancellationToken);

        if (telemetry is null)
        {
            return Result<TelemetryDto?>.Fail(
                "Telemetry not found.");
        }

        var userCompanyId = GetUserCompanyId(user);

        if (userCompanyId is null)
        {
            return Result<TelemetryDto?>.Fail(
                "You do not have access to this telemetry.");
        }

        var sensor =
            await _sensorRepository.GetByIdAsync(
                telemetry.SensorId,
                cancellationToken);

        if (sensor is null)
        {
            return Result<TelemetryDto?>.Fail(
                "Sensor not found.");
        }

        if (!sensor.GatewayId.HasValue)
        {
            return Result<TelemetryDto?>.Fail(
                "Sensor is not associated with a gateway.");
        }

        var gateway =
            await _gatewayRepository.GetByIdAsync(
                sensor.GatewayId.Value,
                cancellationToken);

        if (gateway is null)
        {
            return Result<TelemetryDto?>.Fail(
                "Gateway not found.");
        }

        if (gateway.CompanyId != userCompanyId.Value)
        {
            return Result<TelemetryDto?>.Fail(
                "You do not have access to this telemetry.");
        }

        var dto =
            _mapper.Map<TelemetryDto>(
                telemetry);

        return Result<TelemetryDto?>.Ok(dto);
    }

    private static Guid? GetUserCompanyId(
        ClaimsPrincipal user)
    {
        var claim =
            user.FindFirst("CompanyId")?.Value;

        if (!Guid.TryParse(claim, out var companyId) ||
            companyId == Guid.Empty)
        {
            return null;
        }

        return companyId;
    }
}
