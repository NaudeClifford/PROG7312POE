namespace SmartX.Application.Services.Sensors
{
    public sealed class ManualStack<T>
    {
        private readonly List<T> _items = new();

        public int Count => _items.Count;

        public void Push(T item)
        {
            _items.Add(item);
        }

        public T? Pop()
        {
            if (_items.Count == 0) return default;

            var topIndex = _items.Count - 1;

            T item = _items[topIndex];

            _items.RemoveAt(topIndex);

            return item;
        }

        public T? Peek()
        {
            if (_items.Count == 0) return default;

            return _items[^1];
        }

        public bool TryPop(out T? item)
        {
            if (_items.Count == 0)
            {
                item = default;
                return false;
            }
            int topIndex = _items.Count - 1;

            item = _items[topIndex];
            _items.RemoveAt(topIndex);

            return true;
        }

        public bool TryPeek(out T? item)
        {
            if (_items.Count == 0)
            {
                item = default;
                return false;
            }

            item = _items[^1];
            return true;
        }
        public T? PeekLast(Func<T, bool> predicate)
        {
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                if (predicate(_items[i]))
                    return _items[i];
            }

            return default;
        }

        public bool Remove(T item)
        {
            return _items.Remove(item);
        }
        public void Clear()
        {
            _items.Clear();
        }
    }
}
