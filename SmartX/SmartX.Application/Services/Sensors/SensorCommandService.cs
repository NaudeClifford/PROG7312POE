using SmartX.Application.Commands.Sensor;
using SmartX.Application.Requests.Sensor;
using SmartX.Domain.Interfaces;

namespace SmartX.Application.Services.Sensors;

public sealed class SensorCommandService :
    ISensorCommandService
{
    private readonly SendSensorCommandHandler _handler;
    private readonly AuditLogService _auditLog;
    private readonly ISensorRepository _sensorRepository;
    private readonly IGatewayRepository _gatewayRepository;

    public SensorCommandService(
        SendSensorCommandHandler handler,
        AuditLogService auditLog,
        ISensorRepository sensorRepository,
        IGatewayRepository gatewayRepository)
    {
        _handler = handler;
        _auditLog = auditLog;
        _sensorRepository = sensorRepository;
        _gatewayRepository = gatewayRepository;
    }

    public async Task<SensorCommandResult> SendCommandAsync(
        Guid sensorId,
        string command,
        CancellationToken cancellationToken = default)
    {
        var result =
            await _handler.HandleAsync(
                new SendSensorCommandRequest
                {
                    SensorId = sensorId,
                    Command = command
                },
                cancellationToken);

        if (!result.Success)
        {
            return SensorCommandResult.Fail(
                result.Error ??
                "The command could not be executed.");
        }

        var sensor =
            await _sensorRepository.GetByIdAsync(
                sensorId,
                cancellationToken);

        if (sensor is null)
        {
            return SensorCommandResult.Fail(
                "Sensor not found.");
        }

        if (!sensor.GatewayId.HasValue)
        {
            return SensorCommandResult.Fail(
                "Sensor is not associated with a gateway.");
        }

        var gateway =
            await _gatewayRepository.GetByIdAsync(
                sensor.GatewayId.Value,
                cancellationToken);

        if (gateway is null)
        {
            return SensorCommandResult.Fail(
                "Gateway not found.");
        }

        await _auditLog.LogAsync(
            entityType: "Sensor",
            entityId: sensor.Id,
            action: "CommandExecuted",
            companyId: gateway.CompanyId,
            details:
                $"Command: {command.Trim()}",
            cancellationToken: cancellationToken);

        return SensorCommandResult.Ok(
            result.Data?.Response,
            result.Data?.InverseCommand);
    }
}
