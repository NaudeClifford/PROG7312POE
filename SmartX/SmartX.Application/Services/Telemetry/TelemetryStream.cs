
using DomainTelemetry = SmartX.Domain.Entities.Telemetry;

namespace SmartX.Application.Services.Telemetry;

public sealed class TelemetryStream : ITelemetryStream
{
    private readonly Queue<DomainTelemetry> _criticalQueue = new();
    private readonly Queue<DomainTelemetry> _normalQueue = new();

    private readonly object _lock = new();

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _criticalQueue.Count + _normalQueue.Count;
            }
        }
    }

    public void Enqueue(
        DomainTelemetry telemetry,
        bool isCritical = false)
    {
        ArgumentNullException.ThrowIfNull(telemetry);

        lock (_lock)
        {
            if (isCritical)
                _criticalQueue.Enqueue(telemetry);
            else
                _normalQueue.Enqueue(telemetry);
        }
    }

    public bool TryDequeue(
        out DomainTelemetry? telemetry)
    {
        lock (_lock)
        {
            if (_criticalQueue.Count > 0)
            {
                telemetry = _criticalQueue.Dequeue();
                return true;
            }

            if (_normalQueue.Count > 0)
            {
                telemetry = _normalQueue.Dequeue();
                return true;
            }

            telemetry = null;
            return false;
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _criticalQueue.Clear();
            _normalQueue.Clear();
        }
    }
}
