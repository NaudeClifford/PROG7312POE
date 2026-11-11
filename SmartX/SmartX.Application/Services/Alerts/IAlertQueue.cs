
using SmartX.Shared.DTOs;

namespace SmartX.Application.Services.Alerts;

public interface IAlertQueue
{
    event EventHandler<SensorAlert>? AlertEnqueued;

    int Count { get; }

    IEnumerable<SensorAlert> GetAlerts();

    void Enqueue(SensorAlert alert);

    bool TryDequeue(out SensorAlert? alert);

    bool TryPeek(out SensorAlert? alert);

    void Clear();
}
