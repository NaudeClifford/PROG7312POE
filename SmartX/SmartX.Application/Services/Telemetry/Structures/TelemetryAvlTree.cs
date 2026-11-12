
namespace SmartX.Application.Services.Telemetry.Structures;

using DomainTelemetry = SmartX.Domain.Entities.Telemetry;

public sealed class TelemetryAvlTree
{
    private sealed class Node
    {
        public DomainTelemetry Value { get; }
        public Node? Left { get; set; }
        public Node? Right { get; set; }
        public int Height { get; set; } = 1;

        public Node(DomainTelemetry value)
        {
            Value = value;
        }
    }

    private Node? _root;
    private readonly object _lock = new();

    public int Count { get; private set; }

    public void Add(DomainTelemetry telemetry)
    {
        ArgumentNullException.ThrowIfNull(telemetry);

        lock (_lock)
        {
            bool added = false;
            _root = Insert(_root, telemetry, ref added);

            if (added)
                Count++;
        }
    }

    public IReadOnlyList<DomainTelemetry> GetRange(
        DateTime from,
        DateTime to)
    {
        if (from > to)
            throw new ArgumentException(
                "Start date must be before end date.");

        lock (_lock)
        {
            var result = new List<DomainTelemetry>();

            CollectRange(_root, from, to, result);

            return result;
        }
    }

    public IReadOnlyList<DomainTelemetry> GetAll()
    {
        lock (_lock)
        {
            var result = new List<DomainTelemetry>();
            Traverse(_root, result);
            return result;
        }
    }

    private static Node Insert(
        Node? node,
        DomainTelemetry telemetry,
        ref bool added)
    {
        if (node is null)
        {
            added = true;
            return new Node(telemetry);
        }

        int comparison = Compare(telemetry, node.Value);

        if (comparison < 0)
        {
            node.Left = Insert(node.Left, telemetry, ref added);
        }
        else if (comparison > 0)
        {
            node.Right = Insert(node.Right, telemetry, ref added);
        }
        else
        {
            return node;
        }

        UpdateHeight(node);

        return Balance(node);
    }

    private static int Compare(
        DomainTelemetry first,
        DomainTelemetry second)
    {
        int timestamp = first.Timestamp.CompareTo(second.Timestamp);

        return timestamp != 0
            ? timestamp
            : first.Id.CompareTo(second.Id);
    }

    private static int Height(Node? node)
        => node?.Height ?? 0;

    private static void UpdateHeight(Node node)
    {
        node.Height = 1 + Math.Max(
            Height(node.Left),
            Height(node.Right));
    }

    private static int BalanceFactor(Node node)
        => Height(node.Left) - Height(node.Right);

    private static Node Balance(Node node)
    {
        int balance = BalanceFactor(node);

        if (balance > 1)
        {
            if (BalanceFactor(node.Left!) < 0)
                node.Left = RotateLeft(node.Left!);

            return RotateRight(node);
        }

        if (balance < -1)
        {
            if (BalanceFactor(node.Right!) > 0)
                node.Right = RotateRight(node.Right!);

            return RotateLeft(node);
        }

        return node;
    }

    private static Node RotateRight(Node y)
    {
        var x = y.Left!;
        var subtree = x.Right;

        x.Right = y;
        y.Left = subtree;

        UpdateHeight(y);
        UpdateHeight(x);

        return x;
    }

    private static Node RotateLeft(Node x)
    {
        var y = x.Right!;
        var subtree = y.Left;

        y.Left = x;
        x.Right = subtree;

        UpdateHeight(x);
        UpdateHeight(y);

        return y;
    }

    private static void CollectRange(
        Node? node,
        DateTime from,
        DateTime to,
        List<DomainTelemetry> result)
    {
        if (node is null)
            return;

        if (node.Value.Timestamp >= from)
            CollectRange(node.Left, from, to, result);

        if (node.Value.Timestamp >= from &&
            node.Value.Timestamp < to)
        {
            result.Add(node.Value);
        }

        if (node.Value.Timestamp < to)
            CollectRange(node.Right, from, to, result);
    }

    private static void Traverse(
        Node? node,
        List<DomainTelemetry> result)
    {
        if (node is null)
            return;

        Traverse(node.Left, result);
        result.Add(node.Value);
        Traverse(node.Right, result);
    }
}
