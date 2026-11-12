namespace SmartX.Domain.Interfaces;

public interface IPacketPriorityQueue<T>
{
    int Count { get; }

    void Enqueue(T item, double priority);

    bool TryDequeue(out T? item);

    bool TryPeek(out T? item);

    void Clear();
}