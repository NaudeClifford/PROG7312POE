using AutoMapper;
using SmartX.Application.Common;
using SmartX.Domain.Interfaces;
using SmartX.Shared.DTOs.Telemetry;

namespace SmartX.Application.Queries.Telemetry;

public class GetTelemetryByDateRangeHandler
{
    private readonly ITelemetryRepository _telemetryRepository;
    private readonly IMapper _mapper;

    public GetTelemetryByDateRangeHandler(
        ITelemetryRepository telemetryRepository,
        IMapper mapper)
    {
        _telemetryRepository = telemetryRepository;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<TelemetryDto>>> HandleAsync(
        GetTelemetryByDateRangeQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.From > query.To) return 
                Result<IReadOnlyList<TelemetryDto>>.Fail(
                "The start date cannot be after the end date.");

        var telemetry = await _telemetryRepository
            .GetBySensorAndDateAsync(
                query.SensorId,
                query.From,
                query.To,
                cancellationToken);

        var dtos = _mapper.Map<IReadOnlyList<TelemetryDto>>(
            telemetry);

        return Result<IReadOnlyList<TelemetryDto>>.Ok(dtos);
    }
}