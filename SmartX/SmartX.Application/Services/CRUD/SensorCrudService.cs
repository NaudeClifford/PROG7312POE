using SmartX.Application.Commands.Sensors;
using SmartX.Application.Queries.Gateway;
using SmartX.Application.Queries.Sensors;
using SmartX.Domain.Entities;
using SmartX.Shared.DTOs.Sensors;
using SmartX.Shared.Models;
using System.Threading.Channels;

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
    private readonly GetGatewayByIdHandler _getGatewayById;
    private readonly AuditLogService _auditLog;

    public SensorCrudService(
        GetSensorsHandler getSensors,
        GetSensorByIdHandler getSensorById,
        CreateSensorHandler createSensor,
        UpdateSensorHandler updateSensor,
        DeleteSensorHandler deleteSensor,
        GetGatewayByIdHandler getGatewayById,
        AuditLogService auditLog)
    {
        _getSensors = getSensors;
        _getSensorById = getSensorById;
        _createSensor = createSensor;
        _updateSensor = updateSensor;
        _deleteSensor = deleteSensor;
        _getGatewayById = getGatewayById;
        _auditLog = auditLog;
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

    public async Task<Result<Guid>> CreateAsync(
        CreateSensorCommand command,
        CancellationToken cancellationToken = default)
    {
        // A sensor must have a gateway so that we can determine
        // the CompanyId for the audit record.
        if (!command.GatewayId.HasValue)
        {
            return Result<Guid>.Fail(
                "Sensor must be associated with a gateway.");
        }

        var gatewayId = command.GatewayId.Value;

        var gatewayResult = await _getGatewayById.HandleAsync(
            new GetGatewayByIdQuery
            {
                Id = gatewayId
            },
            cancellationToken);

        if (!gatewayResult.Success)
        {
            return Result<Guid>.Fail(
                gatewayResult.Error ?? "Unable to retrieve gateway.");
        }

        var result = await _createSensor.HandleAsync(
            command,
            cancellationToken);

        if (!result.Success)
            return result;

        await _auditLog.LogAsync(
            entityType: "Sensor",
            entityId: result.Data,
            action: "Created",
            companyId: gatewayResult.Data!.CompanyId,
            details: "Sensor created.",
            cancellationToken: cancellationToken);

        return result;
    }

    public async Task<Result<bool>> UpdateAsync(
        UpdateSensorCommand command,
        CancellationToken cancellationToken = default)
    {
        var sensorResult = await _getSensorById.HandleAsync(
            new GetSensorByIdQuery
            {
                SensorId = command.Id
            },
            cancellationToken);

        if (!sensorResult.Success)
        {
            return Result<bool>.Fail(
                sensorResult.Error ?? "Unable to retrieve sensor.");
        }

        var gatewayId = sensorResult.Data!.GatewayId;

        if (!gatewayId.HasValue)
        {
            return Result<bool>.Fail(
                "Sensor is not associated with a gateway.");
        }

        var gatewayResult = await _getGatewayById.HandleAsync(
            new GetGatewayByIdQuery
            {
                Id = gatewayId.Value
            },
            cancellationToken);

        if (!gatewayResult.Success)
        {
            return Result<bool>.Fail(
                gatewayResult.Error ?? "Unable to retrieve gateway.");
        }

        var result = await _updateSensor.HandleAsync(
            command,
            cancellationToken);

        if (!result.Success)
            return result;

        await _auditLog.LogAsync(
            entityType: "Sensor",
            entityId: command.Id,
            action: "Updated",
            companyId: gatewayResult.Data!.CompanyId,
            details: "Sensor updated.",
            cancellationToken: cancellationToken);

        return result;
    }

    public async Task<Result<bool>> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var sensorResult = await _getSensorById.HandleAsync(
            new GetSensorByIdQuery
            {
                SensorId = id
            },
            cancellationToken);

        if (!sensorResult.Success)
        {
            return Result<bool>.Fail(
                sensorResult.Error ?? "Unable to retrieve sensor.");
        }

        var gatewayId = sensorResult.Data!.GatewayId;

        if (!gatewayId.HasValue)
        {
            return Result<bool>.Fail(
                "Sensor is not associated with a gateway.");
        }

        var gatewayResult = await _getGatewayById.HandleAsync(
            new GetGatewayByIdQuery
            {
                Id = gatewayId.Value
            },
            cancellationToken);

        if (!gatewayResult.Success)
        {
            return Result<bool>.Fail(
                gatewayResult.Error ?? "Unable to retrieve gateway.");
        }

        var result = await _deleteSensor.HandleAsync(
            new DeleteSensorCommand
            {
                Id = id
            },
            cancellationToken);

        if (!result.Success)
            return result;

        await _auditLog.LogAsync(
            entityType: "Sensor",
            entityId: id,
            action: "Deleted",
            companyId: gatewayResult.Data!.CompanyId,
            details: "Sensor deleted.",
            cancellationToken: cancellationToken);

        return result;
    }
}