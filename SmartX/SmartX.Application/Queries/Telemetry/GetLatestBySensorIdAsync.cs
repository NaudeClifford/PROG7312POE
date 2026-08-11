using AutoMapper;
using SmartX.Application.Common;
using SmartX.Domain.Interfaces;
using SmartX.Shared.DTOs.Telemetry;

namespace SmartX.Application.Queries.Telemetry;

public class GetLatestTelemetryBySensorHandler
{
    private readonly ITelemetryRepository _telemetryRepository;
    private readonly IMapper _mapper;

    public GetLatestTelemetryBySensorHandler(
        ITelemetryRepository telemetryRepository,
        IMapper mapper)
    {
        _telemetryRepository = telemetryRepository;
        _mapper = mapper;
    }

    public async Task<Result<TelemetryDto?>> HandleAsync(
        GetLatestTelemetryBySensorQuery query,
        CancellationToken cancellationToken = default)
    {
        var telemetry = await _telemetryRepository
            .GetLatestBySensorIdAsync(
                query.SensorId,
                cancellationToken);

        if (telemetry is null)
        {
            return Result<TelemetryDto?>.Fail(
                "Telemetry not found.");
        }

        var dto = _mapper.Map<TelemetryDto>(telemetry);

        return Result<TelemetryDto?>.Ok(dto);
    }
}