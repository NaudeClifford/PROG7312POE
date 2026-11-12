using SmartX.Application.Services.Telemetry.Structures;
using SmartX.Domain.Interfaces;
using SmartX.Shared.DTOs.Telemetry;
using SmartX.Shared.Mapping;
using SmartX.Shared.Models;
using System.Security.Claims;

namespace SmartX.Application.Queries.Telemetry;

public class GetTelemetryAvlHistoryHandler
{
    private readonly TelemetryAvlTreeService _avlTree;
    private readonly ISensorRepository _sensorRepository;
    private readonly IGatewayRepository _gatewayRepository;
    private readonly IMapper _mapper;

    public GetTelemetryAvlHistoryHandler(
        TelemetryAvlTreeService avlTree,
        ISensorRepository sensorRepository,
        IGatewayRepository gatewayRepository,
        IMapper mapper)
    {
        _avlTree = avlTree;
        _sensorRepository = sensorRepository;
        _gatewayRepository = gatewayRepository;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<TelemetryDto>>> HandleAsync(
        GetTelemetryByDateRangeQuery query,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (query.SensorId == Guid.Empty)
            return Result<IReadOnlyList<TelemetryDto>>.Fail(
                "Sensor ID is required.");

        if (query.From > query.To)
            return Result<IReadOnlyList<TelemetryDto>>.Fail(
                "Start date cannot be after end date.");

        cancellationToken.ThrowIfCancellationRequested();

        var companyClaim = user.FindFirst("CompanyId")?.Value;

        if (!Guid.TryParse(companyClaim, out var companyId) ||
            companyId == Guid.Empty)
        {
            return Result<IReadOnlyList<TelemetryDto>>.Fail(
                "You do not have access to this sensor.");
        }

        var sensor = await _sensorRepository.GetByIdAsync(
            query.SensorId,
            cancellationToken);

        if (sensor is null)
            return Result<IReadOnlyList<TelemetryDto>>.Fail(
                "Sensor not found.");

        if (!sensor.GatewayId.HasValue)
            return Result<IReadOnlyList<TelemetryDto>>.Fail(
                "Sensor is not associated with a gateway.");

        var gateway = await _gatewayRepository.GetByIdAsync(
            sensor.GatewayId.Value,
            cancellationToken);

        if (gateway is null)
            return Result<IReadOnlyList<TelemetryDto>>.Fail(
                "Gateway not found.");

        if (gateway.CompanyId != companyId)
            return Result<IReadOnlyList<TelemetryDto>>.Fail(
                "You do not have access to this sensor.");

        cancellationToken.ThrowIfCancellationRequested();

        var telemetry = _avlTree.GetBySensorAndRange(
            query.SensorId,
            query.From,
            query.To);

        var dtos = _mapper.Map<IReadOnlyList<TelemetryDto>>(
            telemetry);

        return Result<IReadOnlyList<TelemetryDto>>.Ok(dtos);
    }
}