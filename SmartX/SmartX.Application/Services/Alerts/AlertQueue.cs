
using SmartX.Shared.DTOs;

namespace SmartX.Application.Services.Alerts;

public sealed class AlertQueue : IAlertQueue
{
    private readonly List<SensorAlert> _queue = new();
    private readonly object _lock = new();

    public event EventHandler<SensorAlert>? AlertEnqueued;

    public int Count
    {
        get
        {
            lock (_lock)
                return _queue.Count;
        }
    }

    public IEnumerable<SensorAlert> GetAlerts()
    {
        lock (_lock)
        {
            // Return highest-priority alerts first.
            return _queue
                .OrderBy(x => x.Severity)
                .ThenBy(x => x.Timestamp)
                .ToArray();
        }
    }

    public void Enqueue(SensorAlert alert)
    {
        ArgumentNullException.ThrowIfNull(alert);

        lock (_lock)
        {
            _queue.Add(alert);
            SiftUp(_queue.Count - 1);
        }

        // Notify subscribers outside the lock.
        AlertEnqueued?.Invoke(this, alert);
    }

    public bool TryDequeue(out SensorAlert? alert)
    {
        lock (_lock)
        {
            if (_queue.Count == 0)
            {
                alert = null;
                return false;
            }

            alert = _queue[0];

            int lastIndex = _queue.Count - 1;

            if (lastIndex == 0)
            {
                _queue.RemoveAt(0);
                return true;
            }

            _queue[0] = _queue[lastIndex];
            _queue.RemoveAt(lastIndex);

            SiftDown(0);

            return true;
        }
    }

    public bool TryPeek(out SensorAlert? alert)
    {
        lock (_lock)
        {
            if (_queue.Count == 0)
            {
                alert = null;
                return false;
            }

            alert = _queue[0];
            return true;
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _queue.Clear();
        }
    }

    private void SiftUp(int index)
    {
        while (index > 0)
        {
            int parentIndex = (index - 1) / 2;

            if (_queue[index].Severity >=
                _queue[parentIndex].Severity)
                break;

            Swap(parentIndex, index);
            index = parentIndex;
        }
    }

    private void SiftDown(int index)
    {
        int count = _queue.Count;

        while (true)
        {
            int left = 2 * index + 1;
            int right = 2 * index + 2;

            int smallest = index;

            if (left < count &&
                _queue[left].Severity <
                _queue[smallest].Severity)
            {
                smallest = left;
            }

            if (right < count &&
                _queue[right].Severity <
                _queue[smallest].Severity)
            {
                smallest = right;
            }

            if (smallest == index)
                break;

            Swap(index, smallest);
            index = smallest;
        }
    }

    private void Swap(int first, int second)
    {
        (_queue[first], _queue[second]) =
            (_queue[second], _queue[first]);
    }
}
