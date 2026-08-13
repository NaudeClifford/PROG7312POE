using AutoMapper;
using SmartX.Shared.Models;
using SmartX.Domain.Entities;
using SmartX.Domain.Interfaces;
using SmartX.Shared.DTOs.Sensors;

namespace SmartX.Application.Queries.Sensors;

public class GetSensorsHandler
{
    private readonly ISensorRepository _sensorRepository;
    private readonly IMapper _mapper;
    public GetSensorsHandler(ISensorRepository sensorRepository, IMapper mapper)
    {
        _sensorRepository = sensorRepository;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<SensorDto>>> HandleAsync(
        GetSensorsQuery query,
        CancellationToken cancellationToken = default)
    {
        var sensors = await _sensorRepository.GetAllAsync(
            cancellationToken);

        var dtos = _mapper.Map<List<SensorDto>>(sensors);


        return Result<IReadOnlyList<SensorDto>>.Ok(dtos);
    }

}