
using DomainTelemetry = SmartX.Domain.Entities.Telemetry;

namespace SmartX.Application.Services.Telemetry;

public sealed class TelemetryHistoryStore
{
    private readonly SortedDictionary<
        DateTime, List<DomainTelemetry>> _history = new();

    private readonly object _lock = new();

    public int Count
    {
        get
        {
            lock (_lock)
                return _history.Values.Sum(x => x.Count);
        }
    }

    public void Add(DomainTelemetry telemetry)
    {
        ArgumentNullException.ThrowIfNull(telemetry);

        lock (_lock)
        {
            if (!_history.TryGetValue(
                telemetry.Timestamp, out var readings))
            {
                readings = [];
                _history.Add(telemetry.Timestamp, readings);
            }

            readings.Add(telemetry);
        }
    }

    public void AddRange(IEnumerable<DomainTelemetry> telemetry)
    {
        ArgumentNullException.ThrowIfNull(telemetry);

        foreach (var item in telemetry)
            Add(item);
    }

    public void Clear()
    {
        lock (_lock)
            _history.Clear();
    }

    public IEnumerable<DomainTelemetry> GetAll()
    {
        lock (_lock)
            return _history.Values
                .SelectMany(x => x)
                .ToList();
    }

    public IEnumerable<DomainTelemetry> GetNewestFirst()
    {
        lock (_lock)
            return _history.Reverse()
                .SelectMany(x => x.Value.AsEnumerable().Reverse())
                .ToList();
    }

    public IEnumerable<DomainTelemetry> GetRange(
        DateTime from,
        DateTime to)
    {
        if (from > to)
            throw new ArgumentException("Invalid date range.");

        lock (_lock)
        {
            return _history
                .Where(x => x.Key >= from && x.Key < to)
                .SelectMany(x => x.Value)
                .ToList();
        }
    }

    public IEnumerable<DomainTelemetry> GetBySensor(
        Guid sensorId)
    {
        lock (_lock)
        {
            return _history.Values
                .SelectMany(x => x)
                .Where(x => x.SensorId == sensorId)
                .ToList();
        }
    }

    public IEnumerable<DomainTelemetry> GetBySensorAndRange(
        Guid sensorId,
        DateTime from,
        DateTime to)
    {
        lock (_lock)
        {
            return _history
                .Where(x => x.Key >= from && x.Key < to)
                .SelectMany(x => x.Value)
                .Where(x => x.SensorId == sensorId)
                .ToList();
        }
    }
}
