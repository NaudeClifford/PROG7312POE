using SmartX.Domain.Entities.Mesh;
using SmartX.Domain.Interfaces.Mesh;
using System.Text.Json;

namespace SmartX.Infrastructure.Repositories.Mesh;

public class JsonMeshRouteRepository : IMeshRouteRepository
{
    private readonly string _filePath;

    private static readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    public JsonMeshRouteRepository()
    {
        _filePath = Path.Combine(
            AppContext.BaseDirectory,
            "Data", "Local", "mesh-routes.json");
    }

    public async Task<IReadOnlyList<MeshRoute>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            if (!File.Exists(_filePath))
                return [];

            var json = await File.ReadAllTextAsync(
                _filePath, cancellationToken);

            return JsonSerializer.Deserialize<List<MeshRoute>>(json)
                ?? [];
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyList<MeshRoute>> GetByNodeAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default)
    {
        var routes = await GetAllAsync(cancellationToken);

        return routes.Where(x =>
            x.SourceNodeId == nodeId ||
            x.DestinationNodeId == nodeId ||
            x.NodePath.Contains(nodeId)).ToList();
    }

    public async Task<IReadOnlyList<MeshRoute>> GetByGatewayIdAsync(
    Guid gatewayId,
    CancellationToken cancellationToken = default)
    {
        var routes = await GetAllAsync(cancellationToken);

        return routes
            .Where(x => x.GatewayId == gatewayId)
            .OrderByDescending(x => x.CalculatedAt)
            .ToList();
    }

    public async Task AddAsync(
        MeshRoute route,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(route);

        await _lock.WaitAsync(cancellationToken);

        try
        {
            var routes = await ReadUnsafeAsync(cancellationToken);

            if (routes.Any(x => x.Id == route.Id))
                throw new InvalidOperationException(
                    "Mesh route already exists.");

            routes.Add(route);

            await WriteUnsafeAsync(routes, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<List<MeshRoute>> ReadUnsafeAsync(
        CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
            return [];

        var json = await File.ReadAllTextAsync(
            _filePath, cancellationToken);

        return JsonSerializer.Deserialize<List<MeshRoute>>(json)
            ?? [];
    }

    private async Task WriteUnsafeAsync(
        List<MeshRoute> routes,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(
            Path.GetDirectoryName(_filePath)!);

        var json = JsonSerializer.Serialize(routes, Options);

        await File.WriteAllTextAsync(
            _filePath, json, cancellationToken);


    }
}