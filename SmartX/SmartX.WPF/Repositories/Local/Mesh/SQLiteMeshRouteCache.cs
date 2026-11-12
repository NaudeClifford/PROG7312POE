using Microsoft.Data.Sqlite;
using SmartX.Domain.Entities.Mesh;
using SmartX.Domain.Enums.Mesh;
using SmartX.WPF.Data;
using System.Text.Json;

namespace SmartX.WPF.Repositories.Local;

public class SQLiteMeshRouteCache(
    SmartXCacheDatabase database) : ILocalMeshRouteCache
{
    private readonly SmartXCacheDatabase _database = database;

    public async Task<IReadOnlyList<MeshRoute>> GetByGatewayIdAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        using var connection = _database.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT * FROM MeshRoutes
            WHERE GatewayId = $gatewayId
            ORDER BY CalculatedAt DESC;
            """;

        command.Parameters.AddWithValue(
            "$gatewayId", gatewayId.ToString());

        using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        var routes = new List<MeshRoute>();

        while (await reader.ReadAsync(cancellationToken))
            routes.Add(Map(reader));

        return routes;
    }

    public async Task<IReadOnlyList<MeshRoute>> GetByNodeIdAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default)
    {
        using var connection = _database.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();

        command.CommandText = """
        SELECT *
        FROM MeshRoutes;
        """;

        using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        var routes = new List<MeshRoute>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var route = Map(reader);

            if (route.SourceNodeId == nodeId ||
                route.DestinationNodeId == nodeId ||
                route.NodePath.Contains(nodeId))
            {
                routes.Add(route);
            }
        }

        return routes;
    }
    public async Task<IReadOnlyList<MeshRoute>> GetAllAsync(
    CancellationToken cancellationToken = default)
    {
        using var connection = _database.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();

        command.CommandText = """
        SELECT *
        FROM MeshRoutes
        ORDER BY CalculatedAt DESC;
        """;

        using var reader = await command.ExecuteReaderAsync(
            cancellationToken);

        var routes = new List<MeshRoute>();

        while (await reader.ReadAsync(cancellationToken))
        {
            routes.Add(Map(reader));
        }

        return routes;
    }
    public async Task UpdateAsync(
    MeshRoute route,
    CancellationToken cancellationToken = default)
    {
        using var connection = _database.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();

        command.CommandText = """
        UPDATE MeshRoutes
        SET
            GatewayId = $gatewayId,
            SourceNodeId = $sourceNodeId,
            DestinationNodeId = $destinationNodeId,
            NodePath = $nodePath,
            Algorithm = $algorithm,
            TotalCost = $totalCost,
            TotalLatency = $totalLatency,
            HopCount = $hopCount,
            IsSuccessful = $isSuccessful,
            CalculatedAt = $calculatedAt
        WHERE Id = $id;
        """;

        AddParameters(command, route);

        var affected = await command.ExecuteNonQueryAsync(
            cancellationToken);

        if (affected > 0)
            return;

        await AddAsync(route, cancellationToken);
    }
    public async Task DeleteAsync(
    Guid id,
    CancellationToken cancellationToken = default)
    {
        using var connection = _database.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();

        command.CommandText = """
        DELETE FROM MeshRoutes
        WHERE Id = $id;
        """;

        command.Parameters.AddWithValue("$id", id.ToString());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
    public async Task AddAsync(
        MeshRoute route,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(route);

        using var connection = _database.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();

        command.CommandText = """
            INSERT INTO MeshRoutes
            (
                Id, GatewayId, SourceNodeId,
                DestinationNodeId, NodePath, Algorithm,
                TotalCost, TotalLatency, HopCount,
                IsSuccessful, CalculatedAt
            )
            VALUES
            (
                $id, $gatewayId, $sourceNodeId,
                $destinationNodeId, $nodePath, $algorithm,
                $totalCost, $totalLatency, $hopCount,
                $isSuccessful, $calculatedAt
            );
            """;

        command.Parameters.AddWithValue("$id", route.Id.ToString());
        command.Parameters.AddWithValue("$gatewayId", route.GatewayId.ToString());
        command.Parameters.AddWithValue("$sourceNodeId", route.SourceNodeId.ToString());
        command.Parameters.AddWithValue("$destinationNodeId", route.DestinationNodeId.ToString());

        command.Parameters.AddWithValue(
            "$nodePath",
            JsonSerializer.Serialize(route.NodePath));

        command.Parameters.AddWithValue(
            "$algorithm", (int)route.Algorithm);

        command.Parameters.AddWithValue("$totalCost", route.TotalCost);
        command.Parameters.AddWithValue("$totalLatency", route.TotalLatency);
        command.Parameters.AddWithValue("$hopCount", route.HopCount);
        command.Parameters.AddWithValue("$isSuccessful", route.IsSuccessful ? 1 : 0);

        command.Parameters.AddWithValue(
            "$calculatedAt", route.CalculatedAt.ToString("O"));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddParameters(
    SqliteCommand command,
    MeshRoute route)
    {
        command.Parameters.AddWithValue(
            "$id", route.Id.ToString());

        command.Parameters.AddWithValue(
            "$gatewayId", route.GatewayId.ToString());

        command.Parameters.AddWithValue(
            "$sourceNodeId", route.SourceNodeId.ToString());

        command.Parameters.AddWithValue(
            "$destinationNodeId", route.DestinationNodeId.ToString());

        command.Parameters.AddWithValue(
            "$nodePath", JsonSerializer.Serialize(route.NodePath));

        command.Parameters.AddWithValue(
            "$algorithm", (int)route.Algorithm);

        command.Parameters.AddWithValue(
            "$totalCost", route.TotalCost);

        command.Parameters.AddWithValue(
            "$totalLatency", route.TotalLatency);

        command.Parameters.AddWithValue(
            "$hopCount", route.HopCount);

        command.Parameters.AddWithValue(
            "$isSuccessful", route.IsSuccessful ? 1 : 0);

        command.Parameters.AddWithValue(
            "$calculatedAt", route.CalculatedAt.ToString("O"));
    }

    private static MeshRoute Map(SqliteDataReader reader)
    {
        return new MeshRoute
        {
            Id = Guid.Parse(reader.GetString(0)),
            GatewayId = Guid.Parse(reader.GetString(1)),
            SourceNodeId = Guid.Parse(reader.GetString(2)),
            DestinationNodeId = Guid.Parse(reader.GetString(3)),

            NodePath =
                JsonSerializer.Deserialize<List<Guid>>(
                    reader.GetString(4)) ?? [],

            Algorithm = (RoutingAlgorithm)reader.GetInt32(5),

            TotalCost = reader.GetDouble(6),
            TotalLatency = reader.GetDouble(7),
            HopCount = reader.GetInt32(8),
            IsSuccessful = reader.GetInt32(9) == 1,

            CalculatedAt = DateTime.Parse(reader.GetString(10))
        };
    }
}