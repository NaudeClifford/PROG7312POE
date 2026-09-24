using DomainTelemetry = SmartX.Domain.Entities.Telemetry;

namespace SmartX.Application.Services.Telemetry;

public sealed class TelemetryHistoryStore
{
    private readonly SortedDictionary<
        DateTime,
        List<DomainTelemetry>> _history = new();

    public int Count =>
        _history.Values.Sum(x => x.Count);

    public void Add(DomainTelemetry telemetry)
    {
        ArgumentNullException.ThrowIfNull(telemetry);

        if (!_history.TryGetValue(
                telemetry.Timestamp,
                out var readings))
        {
            readings = [];

            _history.Add(
                telemetry.Timestamp,
                readings);
        }

        readings.Add(telemetry);
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

    public void Clear()
    {
        _history.Clear();
    }

    public IEnumerable<DomainTelemetry> GetAll()
    {
        foreach (var group in _history)
        {
            foreach (var telemetry in group.Value)
            {
                yield return telemetry;
            }
        }
    }

    public IEnumerable<DomainTelemetry> GetNewestFirst()
    {
        foreach (var group in _history.Reverse())
        {
            foreach (var telemetry in group.Value.AsEnumerable().Reverse())
            {
                yield return telemetry;
            }
        }
    }

    public IEnumerable<DomainTelemetry> GetRange(
        DateTime from,
        DateTime to)
    {
        foreach (var group in _history)
        {
            if (group.Key < from)
            {
                continue;
            }

            if (group.Key >= to)
            {
                break;
            }

            foreach (var telemetry in group.Value)
            {
                yield return telemetry;
            }
        }
    }

    public IEnumerable<DomainTelemetry> GetBySensor(
        Guid sensorId)
    {
        foreach (var telemetry in GetAll())
        {
            if (telemetry.SensorId == sensorId)
            {
                yield return telemetry;
            }
        }
    }

    public IEnumerable<DomainTelemetry> GetBySensorAndRange(
        Guid sensorId,
        DateTime from,
        DateTime to)
    {
        foreach (var telemetry in GetRange(from, to))
        {
            if (telemetry.SensorId == sensorId)
            {
                yield return telemetry;
            }
        }
    }
}
