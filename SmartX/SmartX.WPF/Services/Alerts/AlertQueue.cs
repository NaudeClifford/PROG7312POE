namespace SmartX.Application.Services.Alerts;

public sealed class AlertQueue
{
    private readonly List<SensorAlert> _queue = new();

    public int Count => _queue.Count;

    public IEnumerable<SensorAlert> GetAlerts()
    {
        return _queue.ToList();
    }

    public void Enqueue(SensorAlert alert)
    {
        ArgumentNullException.ThrowIfNull(alert);
        //Added alert to end of heap
        _queue.Add(alert);

        //Restore heap property by bubbling up the newly added alert
        SiftUp(_queue.Count - 1);
    }

    public bool TryDequeue(out SensorAlert? alert)
    {
        if (_queue.Count == 0)
        {
            alert = null;
            return false;
        }

        alert = _queue[0];

        //remove root and restore heap
        int lastIndex = _queue.Count - 1;

        if (lastIndex == 0)
        {
            _queue.RemoveAt(0);
            return true;
        }
        else
        {
            _queue[0] = _queue[lastIndex];
            _queue.RemoveAt(lastIndex);
            SiftDown(0);
        }

        return true;
    }

    public bool TryPeek(out SensorAlert? alert)
    {
        if (_queue.Count == 0)
        {
            alert = null;
            return false;
        }

        alert = _queue[0];

        return true;
    }

    public void Clear()
    {
        _queue.Clear();
    }

    private void SiftUp(int index)
    {
        while (index > 0)
        {
            int parentIndex = (index - 1) / 2;

            if (_queue[index].Severity >= _queue[parentIndex].Severity) break;

            Swap(parentIndex, index);
            index = parentIndex;
        }
    }

    private void SiftDown(int index)
    {
        int count = _queue.Count;

        while (true)
        {
            int leftChildIndex = 2 * index + 1;
            int rightChildIndex = 2 * index + 2;
            int smallestIndex = index;

            if (leftChildIndex < count && _queue[leftChildIndex].Severity < _queue[smallestIndex].Severity)
                smallestIndex = leftChildIndex;

            if (smallestIndex == index) break;

            Swap(index, smallestIndex);
            index = smallestIndex;
        }
    }

    private void Swap(int first, int second)
    {
        (_queue[first], _queue[second]) = (_queue[second], _queue[first]);
    }
}