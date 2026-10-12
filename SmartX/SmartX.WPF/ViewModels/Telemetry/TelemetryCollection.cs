using DomainTelemetry = SmartX.Domain.Entities.Telemetry;

namespace SmartX.WPF.ViewModels.Telemetry;

public class TelemetryCollection
{
    private readonly Dictionary<Guid, DomainTelemetry> _items = new();

    public int Count => _items.Count;

    public event Action? CollectionChanged;

    public void Add(DomainTelemetry telemetry)
    {
        if (telemetry is null) return;

        _items[telemetry.Id] = telemetry; CollectionChanged?.Invoke();
    }

    public void AddRange(IEnumerable<DomainTelemetry> telemetry)
    {
        foreach (var item in telemetry) { _items[item.Id] = item; }

        CollectionChanged?.Invoke();
    }

    public void Replace(IEnumerable<DomainTelemetry> telemetry)
    {
        _items.Clear();

        foreach (var item in telemetry) { _items[item.Id] = item; }

        CollectionChanged?.Invoke();
    }

    public void Clear() { _items.Clear(); CollectionChanged?.Invoke(); }

    public DomainTelemetry this[int index] => Items.ElementAt(index);

    public IEnumerable<DomainTelemetry> Items => _items.Values;

    public DomainTelemetry[] ToArray() => _items.Values.ToArray();

    public IEnumerable<DomainTelemetry> GetNewestFirst() => _items.Values.OrderByDescending(x => x.Timestamp);
}