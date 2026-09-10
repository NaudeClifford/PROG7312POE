using SmartX.Shared.Mapping;
using SmartX.Domain.Interfaces;
using SmartX.Shared.DTOs.Telemetry;
using SmartX.Shared.Models;
using System.Security.Claims;

namespace SmartX.Application.Queries.Telemetry;

public class GetTelemetryBySensorHandler
{
    private readonly ITelemetryRepository _telemetryRepository;
    private readonly ISensorRepository _sensorRepository;
    private readonly IGatewayRepository _gatewayRepository;
    private readonly IMapper _mapper;

    public GetTelemetryBySensorHandler(
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

    public async Task<Result<IReadOnlyList<TelemetryDto>>> HandleAsync(
        GetTelemetryBySensorQuery query,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (query.SensorId == Guid.Empty)
        {
            return Result<IReadOnlyList<TelemetryDto>>.Fail(
                "Sensor ID is required.");
        }

        var userCompanyId = GetUserCompanyId(user);

        if (userCompanyId is null)
        {
            return Result<IReadOnlyList<TelemetryDto>>.Fail(
                "You do not have access to this sensor.");
        }

        var sensor =
            await _sensorRepository.GetByIdAsync(
                query.SensorId,
                cancellationToken);

        if (sensor is null)
        {
            return Result<IReadOnlyList<TelemetryDto>>.Fail(
                "Sensor not found.");
        }

        if (!sensor.GatewayId.HasValue)
        {
            return Result<IReadOnlyList<TelemetryDto>>.Fail(
                "Sensor is not associated with a gateway.");
        }

        var gateway =
            await _gatewayRepository.GetByIdAsync(
                sensor.GatewayId.Value,
                cancellationToken);

        if (gateway is null)
        {
            return Result<IReadOnlyList<TelemetryDto>>.Fail(
                "Gateway not found.");
        }

        if (gateway.CompanyId != userCompanyId.Value)
        {
            return Result<IReadOnlyList<TelemetryDto>>.Fail(
                "You do not have access to this sensor.");
        }

        var telemetry =
            await _telemetryRepository.GetBySensorIdAsync(
                query.SensorId,
                cancellationToken);

        var dtos =
            _mapper.Map<IReadOnlyList<TelemetryDto>>(
                telemetry);

        return Result<IReadOnlyList<TelemetryDto>>.Ok(dtos);
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
