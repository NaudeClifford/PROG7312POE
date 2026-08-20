using AutoMapper;
using SmartX.Shared.Models;
using SmartX.Domain.Entities;
using SmartX.Domain.Interfaces;
using SmartX.Shared.DTOs;
using SmartX.Application.Queries.Gateway;

namespace SmartX.Application.Queries.Gateway
{
    public class GetGatewayByIdHandler
    {
        private readonly IMapper _mapper;
        private readonly IGatewayRepository _gatewayRepository;

        public GetGatewayByIdHandler(IGatewayRepository gatewayRepository,
            IMapper mapper)
        {
            _gatewayRepository = gatewayRepository;
            _mapper = mapper;
        }

        public async Task<Result<GatewayDto>> HandleAsync(
            GetGatewayByIdQuery query,
            CancellationToken cancellationToken = default)
        {
            var sensor = await _gatewayRepository.GetByIdAsync(query.Id,
                cancellationToken);

            if (sensor is null)
            {
                return Result<GatewayDto>.Fail("Gateway not found.");
            }

            var dto = _mapper.Map<GatewayDto>(sensor);

            return Result<GatewayDto>.Ok(dto);
        }
    }
}
