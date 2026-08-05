using SmartX.Application.Common;
using SmartX.Domain.Entities;
using SmartX.Domain.Interfaces;

namespace SmartX.Application.Queries.Sensors;

public class GetSensorsHandler
{
    private readonly ISensorRepository _sensorRepository;

    public GetSensorsHandler(ISensorRepository sensorRepository)
    {
        _sensorRepository = sensorRepository;
    }

    public async Task<Result<IReadOnlyList<Sensor>>> HandleAsync(
        GetSensorsQuery query,
        CancellationToken cancellationToken = default)
    {
        var sensors = await _sensorRepository.GetAllAsync(
            cancellationToken);

        return Result<IReadOnlyList<Sensor>>.Ok(sensors);
    }
}