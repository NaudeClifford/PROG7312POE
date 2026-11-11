using SmartX.Application.Models;
using SmartX.Domain.Entities;

namespace SmartX.Application.Interfaces.Sensors;

public interface ISensorProtocolAdapter
{
    bool Supports(Sensor sensor);

    Task<bool> TestConnectionAsync(
        Sensor sensor,
        CancellationToken cancellationToken = default);

    Task<SensorReading?> ReadAsync(
        Sensor sensor,
        CancellationToken cancellationToken = default);

    Task StopAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default);
}