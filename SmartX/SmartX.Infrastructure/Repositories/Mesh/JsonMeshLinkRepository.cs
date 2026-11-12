using SmartX.Domain.Entities.Mesh;
using SmartX.Domain.Interfaces.Mesh;
using System.Text.Json;

namespace SmartX.Infrastructure.Repositories.Mesh;

public class JsonMeshLinkRepository : IMeshLinkRepository
{
    private readonly string _filePath;

    private static readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    public JsonMeshLinkRepository()
    {
        _filePath = Path.Combine(
            AppContext.BaseDirectory,
            "Data", "Local", "mesh-links.json");
    }

    public async Task<IReadOnlyList<MeshLink>> GetAllAsync(
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

    public async Task<IReadOnlyList<MeshLink>> GetByGatewayIdAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        if (gatewayId == Guid.Empty)
            return [];

        var links = await GetAllAsync(cancellationToken);

        return links
            .Where(x => x.GatewayId == gatewayId)
            .ToList();
    }

    public async Task<IReadOnlyList<MeshLink>> GetByNodeIdAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default)
    {
        var links = await GetAllAsync(cancellationToken);

        return links.Where(x =>
            x.SourceNodeId == nodeId ||
            x.TargetNodeId == nodeId).ToList();
    }

    public async Task AddAsync(
        MeshLink link,
        CancellationToken cancellationToken = default)
    {
        await ModifyAsync(links =>
        {
            if (links.Any(x => x.Id == link.Id))
                throw new InvalidOperationException(
                    "Mesh link already exists.");

            links.Add(link);
        }, cancellationToken);
    }

    public async Task UpdateAsync(
        MeshLink link,
        CancellationToken cancellationToken = default)
    {
        await ModifyAsync(links =>
        {
            var index = links.FindIndex(x => x.Id == link.Id);

            if (index < 0)
                throw new InvalidOperationException(
                    "Mesh link not found.");

            links[index] = link;
        }, cancellationToken);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await ModifyAsync(
            links => links.RemoveAll(x => x.Id == id),
            cancellationToken);
    }

    private async Task ModifyAsync(
        Action<List<MeshLink>> action,
        CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            var links = await ReadUnsafeAsync(cancellationToken);

            action(links);

            await WriteUnsafeAsync(links, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<List<MeshLink>> ReadUnsafeAsync(
        CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
            return [];

        var json = await File.ReadAllTextAsync(
            _filePath, cancellationToken);

        return JsonSerializer.Deserialize<List<MeshLink>>(json)
            ?? [];
    }

    private async Task WriteUnsafeAsync(
        List<MeshLink> links,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(
            Path.GetDirectoryName(_filePath)!);

        var json = JsonSerializer.Serialize(links, Options);

        await File.WriteAllTextAsync(
            _filePath, json, cancellationToken);
    }

    public async Task<MeshLink?> GetByIdAsync(
    Guid id,
    CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            return null;

        var links = await GetAllAsync(cancellationToken);

        return links.FirstOrDefault(x => x.Id == id);
    }
}