using AutoMapper;
using SmartX.Shared.Models;
using SmartX.Domain.Entities;
using SmartX.Domain.Interfaces;
using SmartX.Shared.DTOs;

namespace SmartX.Application.Queries.Gateway;

public class GetGatewaysHandler
{
    private readonly IGatewayRepository _gatewayRepository;
    private readonly IMapper _mapper;
    public GetGatewaysHandler(IGatewayRepository gatewayRepository, IMapper mapper)
    {
        _gatewayRepository = gatewayRepository;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<GatewayDto>>> HandleAsync(
        GetGatewaysQuery query,
        CancellationToken cancellationToken = default)
    {
        var gateways = await _gatewayRepository.GetAllAsync(
            cancellationToken);

        var dtos = _mapper.Map<List<GatewayDto>>(gateways);


        return Result<IReadOnlyList<GatewayDto>>.Ok(dtos);
    }

}