using SmartX.Domain.Entities.Mesh;
using SmartX.Domain.Interfaces.Mesh;
using System.Text.Json;

namespace SmartX.Infrastructure.Repositories.Mesh;

public class JsonMeshNodeRepository : IMeshNodeRepository
{
    private readonly string _filePath;

    private static readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    public JsonMeshNodeRepository()
    {
        _filePath = Path.Combine(
            AppContext.BaseDirectory,
            "Data", "Local", "mesh-nodes.json");
    }

    public async Task<IReadOnlyList<MeshNode>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            if (!File.Exists(_filePath))
                return [];

            var json = await File.ReadAllTextAsync(
                _filePath, cancellationToken);

            return JsonSerializer.Deserialize<List<MeshNode>>(json)
                ?? [];
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyList<MeshNode>> GetByGatewayIdAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        if (gatewayId == Guid.Empty)
            return [];

        var nodes = await GetAllAsync(cancellationToken);

        return nodes
            .Where(x => x.GatewayId == gatewayId)
            .ToList();
    }

    public async Task<MeshNode?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var nodes = await GetAllAsync(cancellationToken);

        return nodes.FirstOrDefault(x => x.Id == id);
    }

    public async Task AddAsync(
        MeshNode node,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(node);

        await ModifyAsync(nodes =>
        {
            if (nodes.Any(x => x.Id == node.Id))
                throw new InvalidOperationException(
                    "Mesh node already exists.");

            nodes.Add(node);
        }, cancellationToken);
    }

    public async Task UpdateAsync(
        MeshNode node,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(node);

        await ModifyAsync(nodes =>
        {
            var index = nodes.FindIndex(x => x.Id == node.Id);

            if (index < 0)
                throw new InvalidOperationException(
                    "Mesh node not found.");

            nodes[index] = node;
        }, cancellationToken);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await ModifyAsync(
            nodes => nodes.RemoveAll(x => x.Id == id),
            cancellationToken);
    }

    private async Task ModifyAsync(
        Action<List<MeshNode>> action,
        CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            var nodes = await ReadUnsafeAsync(cancellationToken);

            action(nodes);

            await WriteUnsafeAsync(nodes, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<List<MeshNode>> ReadUnsafeAsync(
        CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
            return [];

        var json = await File.ReadAllTextAsync(
            _filePath, cancellationToken);

        return JsonSerializer.Deserialize<List<MeshNode>>(json)
            ?? [];
    }

    private async Task WriteUnsafeAsync(
        List<MeshNode> nodes,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_filePath)!;

        Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(nodes, Options);

        await File.WriteAllTextAsync(
            _filePath, json, cancellationToken);
    }
}