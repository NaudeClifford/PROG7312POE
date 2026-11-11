
using SmartX.Application.Interfaces.Sensors;
using SmartX.Application.Models;
using SmartX.Domain.Entities;
using SmartX.Domain.Enums;

namespace SmartX.Infrastructure.Sensors;

public class SimulationSensorAdapter : ISensorProtocolAdapter
{
    public bool Supports(Sensor sensor)
    {
        return sensor.IsSimulation ||
               sensor.Protocol == SensorProtocol.Simulation;
    }

    public Task<bool> TestConnectionAsync(
        Sensor sensor,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(true);
    }

    public Task<SensorReading?> ReadAsync(
        Sensor sensor,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        double voltage = Math.Round(
            220 + Random.Shared.NextDouble() * 10, 2);

        double current = Math.Round(
            1 + Random.Shared.NextDouble() * 4, 2);

        var reading = new SensorReading
        {
            DeviceIdentifier = sensor.DeviceIdentifier,
            Timestamp = DateTime.UtcNow,

            Voltage = voltage,
            Current = current,
            Power = Math.Round(voltage * current, 2),

            Temperature = Math.Round(
                20 + Random.Shared.NextDouble() * 10, 2)
        };

        return Task.FromResult<SensorReading?>(reading);
    }

    public Task StopAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.CompletedTask;
    }
}
