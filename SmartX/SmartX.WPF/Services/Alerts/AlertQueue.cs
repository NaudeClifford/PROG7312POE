namespace SmartX.Application.Services.Alerts;

public sealed class AlertQueue
{
    private readonly PriorityQueue<SensorAlert, int> _queue = new();

    public int Count => _queue.Count;

    public void Enqueue(SensorAlert alert)
    {
        ArgumentNullException.ThrowIfNull(alert);

        _queue.Enqueue(
            alert,
            (int)alert.Severity);
    }

    public bool TryDequeue(out SensorAlert? alert)
    {
        if (_queue.Count == 0)
        {
            alert = null;
            return false;
        }

        alert = _queue.Dequeue();

        return true;
    }

    public bool TryPeek(out SensorAlert? alert)
    {
        if (_queue.Count == 0)
        {
            alert = null;
            return false;
        }

        alert = _queue.Peek();

        return true;
    }

    public void Clear()
    {
        _queue.Clear();
    }
}