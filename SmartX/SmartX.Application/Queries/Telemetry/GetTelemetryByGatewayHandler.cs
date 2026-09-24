using SmartX.Shared.Mapping;
using SmartX.Domain.Interfaces;
using SmartX.Shared.DTOs.Telemetry;
using SmartX.Shared.Models;
using System.Security.Claims;

namespace SmartX.Application.Queries.Telemetry;

public class GetTelemetryByGatewayHandler
{
    private readonly ITelemetryRepository _telemetryRepository;
    private readonly IGatewayRepository _gatewayRepository;
    private readonly IMapper _mapper;

    public GetTelemetryByGatewayHandler(
        ITelemetryRepository telemetryRepository,
        IGatewayRepository gatewayRepository,
        IMapper mapper)
    {
        _telemetryRepository = telemetryRepository;
        _gatewayRepository = gatewayRepository;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<TelemetryDto>>> HandleAsync(
        GetTelemetryByGatewayQuery query,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (query.GatewayId == Guid.Empty)
        {
            return Result<IReadOnlyList<TelemetryDto>>.Fail(
                "Gateway ID is required.");
        }

        var userCompanyId =
            GetUserCompanyId(user);

        if (userCompanyId is null)
        {
            return Result<IReadOnlyList<TelemetryDto>>.Fail(
                "You do not have access to this gateway.");
        }

        var gateway =
            await _gatewayRepository.GetByIdAsync(
                query.GatewayId,
                cancellationToken);

        if (gateway is null)
        {
            return Result<IReadOnlyList<TelemetryDto>>.Fail(
                "Gateway not found.");
        }

        if (gateway.CompanyId != userCompanyId.Value)
        {
            return Result<IReadOnlyList<TelemetryDto>>.Fail(
                "You do not have access to this gateway.");
        }

        var telemetry =
            await _telemetryRepository.GetByGatewayIdAsync(
                query.GatewayId,
                cancellationToken);

        var dtos =
            _mapper.Map<IReadOnlyList<TelemetryDto>>(
                telemetry);

        return Result<IReadOnlyList<TelemetryDto>>.Ok(
            dtos);
    }

    private static Guid? GetUserCompanyId(
        ClaimsPrincipal user)
    {
        var claim =
            user.FindFirst("CompanyId")?.Value;

        if (!Guid.TryParse(
                claim,
                out var companyId) ||
            companyId == Guid.Empty)
        {
            return null;
        }

        return companyId;
    }
}
