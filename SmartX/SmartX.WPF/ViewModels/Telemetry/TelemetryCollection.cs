using DomainTelemetry = SmartX.Domain.Entities.Telemetry;

namespace SmartX.WPF.ViewModels.Telemetry;

public class TelemetryCollection
{
    private readonly List<DomainTelemetry> _items = [];

    public int Count =>
        _items.Count;

    public void Add(DomainTelemetry telemetry)
    {
        _items.Add(telemetry);
    }

    public void Clear()
    {
        _items.Clear();
    }

    public DomainTelemetry this[int index] =>
        _items[index];

    public IEnumerable<DomainTelemetry> Items =>
        _items;

    public DomainTelemetry[] ToArray()
    {
        return _items.ToArray();
    }
}