using System.Collections.Concurrent;
using DomainTelemetry = SmartX.Domain.Entities.Telemetry;

namespace SmartX.Application.Services.Telemetry.Structures;

public sealed class TelemetryAvlTreeService
{
    private readonly ConcurrentDictionary<Guid, TelemetryAvlTree>
        _sensorTrees = new();

    public int SensorCount => _sensorTrees.Count;

    public void Add(DomainTelemetry telemetry)
    {
        ArgumentNullException.ThrowIfNull(telemetry);

        if (telemetry.SensorId == Guid.Empty)
            throw new ArgumentException("Sensor ID is required.");

        var tree = _sensorTrees.GetOrAdd(
            telemetry.SensorId,
            _ => new TelemetryAvlTree());

        lock (tree)
        {
            tree.Add(telemetry);
        }
    }

    public IReadOnlyList<DomainTelemetry> GetBySensor(
        Guid sensorId)
    {
        if (sensorId == Guid.Empty)
            return [];

        if (!_sensorTrees.TryGetValue(sensorId, out var tree))
            return [];

        lock (tree)
        {
            return tree.GetAll();
        }
    }

    public IReadOnlyList<DomainTelemetry> GetBySensorAndRange(
        Guid sensorId,
        DateTime from,
        DateTime to)
    {
        if (sensorId == Guid.Empty)
            return [];

        if (from > to)
            throw new ArgumentException(
                "Start date cannot be after end date.");

        if (!_sensorTrees.TryGetValue(sensorId, out var tree))
            return [];

        lock (tree)
        {
            return tree.GetRange(from, to);
        }
    }
    public void AddRange(
    IEnumerable<DomainTelemetry> telemetry)
    {
        ArgumentNullException.ThrowIfNull(telemetry);

        foreach (var item in telemetry)
        {
            Add(item);
        }
    }

    public void ClearSensor(Guid sensorId)
    {
        if (sensorId == Guid.Empty)
            return;

        _sensorTrees.TryRemove(sensorId, out _);
    }

    public void Clear()
    {
        _sensorTrees.Clear();
    }
}