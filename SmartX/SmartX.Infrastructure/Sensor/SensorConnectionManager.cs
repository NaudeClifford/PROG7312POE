
using SmartX.Application.Interfaces.Sensors;
using SmartX.Application.Models;
using SmartX.Domain.Entities;

namespace SmartX.Application.Services.Sensors;

public class SensorConnectionManager : ISensorConnectionService
{
    private readonly IEnumerable<ISensorProtocolAdapter> _adapters;

    public SensorConnectionManager(
        IEnumerable<ISensorProtocolAdapter> adapters)
    {
        _adapters = adapters;
    }

    private ISensorProtocolAdapter GetAdapter(Sensor sensor)
    {
        return _adapters.FirstOrDefault(x => x.Supports(sensor))
            ?? throw new NotSupportedException(
                $"No adapter registered for {sensor.Protocol}.");
    }

    public async Task<bool> TestConnectionAsync(
        Sensor sensor,
        CancellationToken cancellationToken = default)
    {
        return await GetAdapter(sensor)
            .TestConnectionAsync(sensor, cancellationToken);
    }

    public async Task StartAsync(
        Sensor sensor,
        CancellationToken cancellationToken = default)
    {
        var connected = await TestConnectionAsync(
            sensor, cancellationToken);

        if (!connected)
            throw new InvalidOperationException(
                $"Could not connect to sensor {sensor.Name}.");
    }

    public async Task<SensorReading?> ReadAsync(
        Sensor sensor,
        CancellationToken cancellationToken = default)
    {
        return await GetAdapter(sensor)
            .ReadAsync(sensor, cancellationToken);
    }

    public async Task StopAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default)
    {
        foreach (var adapter in _adapters)
        {
            await adapter.StopAsync(
                sensorId, cancellationToken);
        }
    }
}
