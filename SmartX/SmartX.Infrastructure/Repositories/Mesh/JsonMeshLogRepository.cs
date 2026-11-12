using SmartX.Domain.Entities.Mesh;
using SmartX.Domain.Interfaces.Mesh;
using System.Text.Json;

namespace SmartX.Infrastructure.Repositories.Mesh;

public class JsonMeshLogRepository : IMeshLogRepository
{
    private readonly string _filePath;

    private static readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    public JsonMeshLogRepository()
    {
        _filePath = Path.Combine(
            AppContext.BaseDirectory,
            "Data", "Local", "mesh-logs.json");
    }

    public async Task AddAsync(
        MeshLog log,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            var logs = await ReadUnsafeAsync(cancellationToken);

            logs.Add(log);

            await WriteUnsafeAsync(logs, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyList<MeshLog>> GetByNodeIdAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default)
    {
        var logs = await GetAllAsync(cancellationToken);

        return logs
            .Where(x => x.NodeId == nodeId)
            .OrderByDescending(x => x.Timestamp)
            .ToList();
    }

    public async Task<IReadOnlyList<MeshLog>> GetByGatewayIdAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        var logs = await GetAllAsync(cancellationToken);

        return logs
            .Where(x => x.GatewayId == gatewayId)
            .OrderByDescending(x => x.Timestamp)
            .ToList();
    }

    public async Task DeleteOlderThanAsync(
        DateTime cutoff,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            var logs = await ReadUnsafeAsync(cancellationToken);

            logs.RemoveAll(x => x.Timestamp < cutoff);

            await WriteUnsafeAsync(logs, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<IReadOnlyList<MeshLog>> GetAllAsync(
        CancellationToken cancellationToken)
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

    private async Task<List<MeshLog>> ReadUnsafeAsync(
        CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
            return [];

        var json = await File.ReadAllTextAsync(
            _filePath, cancellationToken);

        return JsonSerializer.Deserialize<List<MeshLog>>(json)
            ?? [];
    }

    private async Task WriteUnsafeAsync(
        List<MeshLog> logs,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(
            Path.GetDirectoryName(_filePath)!);

        var json = JsonSerializer.Serialize(logs, Options);

        await File.WriteAllTextAsync(
            _filePath, json, cancellationToken);
    }
}