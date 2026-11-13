namespace SmartX.Domain.Interfaces.Mesh;

public interface IMeshSensorHeartbeatService
{
    Task ProcessHeartbeatAsync(
        Guid gatewayId,
        Guid sensorId,
        CancellationToken cancellationToken = default);

    Task EvaluateSensorConnectivityAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default);
}