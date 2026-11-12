using SmartX.Domain.Entities.Mesh;
using SmartX.Domain.Interfaces.Mesh;
using System.Text.Json;

namespace SmartX.Infrastructure.Repositories.Mesh;

public class JsonMeshConnectionRepository
    : IMeshConnectionRepository
{
    private readonly string _filePath;

    private static readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    public JsonMeshConnectionRepository()
    {
        _filePath = Path.Combine(
            AppContext.BaseDirectory,
            "Data", "Local", "mesh-connections.json");
    }

    public async Task<IReadOnlyList<MeshConnection>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            return await ReadUnsafeAsync(cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyList<MeshConnection>> GetByNodeIdAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default)
    {
        var connections = await GetAllAsync(cancellationToken);

        return connections.Where(x =>
            x.SourceNodeId == nodeId ||
            x.DestinationNodeId == nodeId).ToList();
    }

    public async Task AddAsync(
        MeshConnection connection,
        CancellationToken cancellationToken = default)
    {
        await ModifyAsync(connections =>
        {
            if (connections.Any(x => x.Id == connection.Id))
                throw new InvalidOperationException(
                    "Mesh connection already exists.");

            connections.Add(connection);
        }, cancellationToken);
    }

    public async Task UpdateAsync(
        MeshConnection connection,
        CancellationToken cancellationToken = default)
    {
        await ModifyAsync(connections =>
        {
            var index = connections.FindIndex(
                x => x.Id == connection.Id);

            if (index < 0)
                throw new InvalidOperationException(
                    "Mesh connection not found.");

            connections[index] = connection;
        }, cancellationToken);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await ModifyAsync(
            connections => connections.RemoveAll(x => x.Id == id),
            cancellationToken);
    }

    private async Task ModifyAsync(
        Action<List<MeshConnection>> action,
        CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            var connections = await ReadUnsafeAsync(cancellationToken);

            action(connections);

            await WriteUnsafeAsync(connections, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }
    public async Task<IReadOnlyList<MeshConnection>> GetByGatewayIdAsync(
    Guid gatewayId,
    CancellationToken cancellationToken = default)
    {
        if (gatewayId == Guid.Empty)
            return [];

        var connections = await GetAllAsync(cancellationToken);

        return connections
            .Where(x => x.GatewayId == gatewayId)
            .ToList();
    }

    private async Task<List<MeshConnection>> ReadUnsafeAsync(
        CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
            return [];

        var json = await File.ReadAllTextAsync(
            _filePath, cancellationToken);

        return JsonSerializer.Deserialize<List<MeshConnection>>(json)
            ?? [];
    }

    private async Task WriteUnsafeAsync(
        List<MeshConnection> connections,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(
            Path.GetDirectoryName(_filePath)!);

        var json = JsonSerializer.Serialize(connections, Options);

        await File.WriteAllTextAsync(
            _filePath, json, cancellationToken);
    }
}