using System.Collections.Concurrent;

using DomainTelemetry = SmartX.Domain.Entities.Telemetry;

namespace SmartX.Application.Services.Telemetry;

public sealed class TelemetryStream : ITelemetryStream
{
    private readonly ConcurrentQueue<DomainTelemetry> _queue = new();

    public int Count => _queue.Count;

    public void Enqueue(
        DomainTelemetry telemetry)
    {
        ArgumentNullException.ThrowIfNull(telemetry);

        _queue.Enqueue(telemetry);
    }

    public bool TryDequeue(
        out DomainTelemetry? telemetry)
    {
        if (_queue.TryDequeue(out var result))
        {
            telemetry = result;

            return true;
        }

        telemetry = null;

        return false;
    }

    public void Clear()
    {
        while (_queue.TryDequeue(
                   out _))
        {
        }
    }
}
