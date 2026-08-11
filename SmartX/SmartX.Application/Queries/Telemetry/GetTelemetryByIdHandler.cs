using AutoMapper;
using SmartX.Application.Common;
using SmartX.Domain.Interfaces;
using SmartX.Shared.DTOs.Telemetry;

namespace SmartX.Application.Queries.Telemetry;

public class GetTelemetryByIdHandler
{
    private readonly ITelemetryRepository _telemetryRepository;
    private readonly IMapper _mapper;

    public GetTelemetryByIdHandler(
        ITelemetryRepository telemetryRepository,
        IMapper mapper)
    {
        _mapper = mapper;
        _telemetryRepository = telemetryRepository;
    }

    public async Task<Result<TelemetryDto?>> HandleAsync(
        GetTelemetryByIdQuery query,
        CancellationToken cancellationToken)
    {
        var telemetry = await _telemetryRepository.GetByIdAsync(
            query.TelemetryId,
            cancellationToken);

        if (telemetry is null)
        {
            return Result<TelemetryDto?>.Fail(
                "Telemetry not found");
        }
        
        var dto = _mapper.Map<TelemetryDto>(telemetry);

        return Result<TelemetryDto?>.Ok(dto);
    }
}