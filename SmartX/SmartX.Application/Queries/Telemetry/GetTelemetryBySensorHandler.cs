using AutoMapper;
using SmartX.Shared.Models;
using SmartX.Domain.Interfaces;
using SmartX.Shared.DTOs.Telemetry;

namespace SmartX.Application.Queries.Telemetry;

public class GetTelemetryBySensorHandler
{
    private readonly ITelemetryRepository _telemetryRepository;
    private readonly IMapper _mapper;

    public GetTelemetryBySensorHandler(
        ITelemetryRepository telemetryRepository,
        IMapper mapper)
    {
        _telemetryRepository = telemetryRepository;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<TelemetryDto>>> HandleAsync(
        GetTelemetryBySensorQuery query,
        CancellationToken cancellationToken = default)
    {
        var telemetry = await _telemetryRepository.GetBySensorIdAsync(
            query.SensorId,
            cancellationToken);

        var dtos = _mapper.Map<IReadOnlyList<TelemetryDto>>(
            telemetry);

        return Result<IReadOnlyList<TelemetryDto>>.Ok(dtos);
    }
}