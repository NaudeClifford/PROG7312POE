using SmartX.Application.Commands.Sensors;
using SmartX.Application.Queries.Sensors;
using SmartX.Shared.DTOs.Sensors;
using SmartX.Shared.Models;

namespace SmartX.Application.Services.CRUD;

public class SensorCrudService :
    ICrudService<
        SensorDto,
        CreateSensorCommand,
        UpdateSensorCommand>
{
    private readonly GetSensorsHandler _getSensors;
    private readonly GetSensorByIdHandler _getSensorById;
    private readonly CreateSensorHandler _createSensor;
    private readonly UpdateSensorHandler _updateSensor;
    private readonly DeleteSensorHandler _deleteSensor;

    public SensorCrudService(
        GetSensorsHandler getSensors,
        GetSensorByIdHandler getSensorById,
        CreateSensorHandler createSensor,
        UpdateSensorHandler updateSensor,
        DeleteSensorHandler deleteSensor)
    {
        _getSensors = getSensors;
        _getSensorById = getSensorById;
        _createSensor = createSensor;
        _updateSensor = updateSensor;
        _deleteSensor = deleteSensor;
    }

    public Task<Result<IReadOnlyList<SensorDto>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return _getSensors.HandleAsync(
            new GetSensorsQuery(),
            cancellationToken);
    }

    public Task<Result<SensorDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _getSensorById.HandleAsync(
            new GetSensorByIdQuery
            {
                SensorId = id
            },
            cancellationToken);
    }

    public Task<Result<Guid>> CreateAsync(
        CreateSensorCommand command,
        CancellationToken cancellationToken = default)
    {
        return _createSensor.HandleAsync(
            command,
            cancellationToken);
    }

    public Task<Result<bool>> UpdateAsync(
        UpdateSensorCommand command,
        CancellationToken cancellationToken = default)
    {
        return _updateSensor.HandleAsync(
            command,
            cancellationToken);
    }

    public Task<Result<bool>> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _deleteSensor.HandleAsync(
            new DeleteSensorCommand
            {
                Id = id
            },
            cancellationToken);
    }
}