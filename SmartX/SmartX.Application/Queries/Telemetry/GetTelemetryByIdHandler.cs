using AutoMapper;
using SmartX.Domain.Interfaces;
using SmartX.Shared.DTOs.Telemetry;
using SmartX.Shared.Models;

namespace SmartX.Application.Queries.Telemetry;

public class GetTelemetryByIdHandler
{
    private readonly ITelemetryRepository _telemetryRepository;
    private readonly IMapper _mapper;

    public GetTelemetryByIdHandler(
        ITelemetryRepository telemetryRepository,
        IMapper mapper)
    {
        _telemetryRepository = telemetryRepository;
        _mapper = mapper;
    }

    public async Task<Result<TelemetryDto?>> HandleAsync(
        GetTelemetryByIdQuery query,
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

        var dto = _mapper.Map<TelemetryDto>(
            telemetry);

        return Result<TelemetryDto?>.Ok(dto);
    }
}
