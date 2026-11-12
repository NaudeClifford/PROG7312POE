using Microsoft.Data.Sqlite;
using SmartX.Domain.Entities.Mesh;
using SmartX.WPF.Data;

namespace SmartX.WPF.Repositories.Local;

public class SQLiteMeshConnectionCache(
    SmartXCacheDatabase database) : ILocalMeshConnectionCache
{
    private readonly SmartXCacheDatabase _database = database;

    public async Task<IReadOnlyList<MeshConnection>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        using var connection = _database.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT * FROM MeshConnections
            ORDER BY LastUpdated DESC;
            """;

        using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        var connections = new List<MeshConnection>();

        while (await reader.ReadAsync(cancellationToken))
            connections.Add(Map(reader));

        return connections;
    }

    public async Task<IReadOnlyList<MeshConnection>> GetByNodeIdAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default)
    {
        using var connection = _database.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT * FROM MeshConnections
            WHERE SourceNodeId = $nodeId
               OR DestinationNodeId = $nodeId
            ORDER BY LastUpdated DESC;
            """;

        command.Parameters.AddWithValue(
            "$nodeId", nodeId.ToString());

        using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        var connections = new List<MeshConnection>();

        while (await reader.ReadAsync(cancellationToken))
            connections.Add(Map(reader));

        return connections;
    }

    public async Task AddAsync(
        MeshConnection connection,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        using var dbConnection = _database.CreateConnection();
        await dbConnection.OpenAsync(cancellationToken);

        using var command = dbConnection.CreateCommand();

        command.CommandText = """
            INSERT INTO MeshConnections
            (
                Id, SourceNodeId, DestinationNodeId,
                SignalStrength, LinkQuality, Latency,
                EnergyCost, RoutingCost, IsActive,
                LastUpdated, CreatedAt
            )
            VALUES
            (
                $id, $sourceNodeId, $destinationNodeId,
                $signalStrength, $linkQuality, $latency,
                $energyCost, $routingCost, $isActive,
                $lastUpdated, $createdAt
            );
            """;

        AddParameters(command, connection);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        MeshConnection connection,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        using var dbConnection = _database.CreateConnection();
        await dbConnection.OpenAsync(cancellationToken);

        using var command = dbConnection.CreateCommand();

        command.CommandText = """
            INSERT INTO MeshConnections
            (
                Id, SourceNodeId, DestinationNodeId,
                SignalStrength, LinkQuality, Latency,
                EnergyCost, RoutingCost, IsActive,
                LastUpdated, CreatedAt
            )
            VALUES
            (
                $id, $sourceNodeId, $destinationNodeId,
                $signalStrength, $linkQuality, $latency,
                $energyCost, $routingCost, $isActive,
                $lastUpdated, $createdAt
            )
            ON CONFLICT(Id) DO UPDATE SET
                SourceNodeId = excluded.SourceNodeId,
                DestinationNodeId = excluded.DestinationNodeId,
                SignalStrength = excluded.SignalStrength,
                LinkQuality = excluded.LinkQuality,
                Latency = excluded.Latency,
                EnergyCost = excluded.EnergyCost,
                RoutingCost = excluded.RoutingCost,
                IsActive = excluded.IsActive,
                LastUpdated = excluded.LastUpdated,
                CreatedAt = excluded.CreatedAt;
            """;

        AddParameters(command, connection);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var connection = _database.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();

        command.CommandText = """
            DELETE FROM MeshConnections
            WHERE Id = $id;
            """;

        command.Parameters.AddWithValue("$id", id.ToString());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddParameters(
        SqliteCommand command,
        MeshConnection connection)
    {
        command.Parameters.AddWithValue("$id", connection.Id.ToString());

        command.Parameters.AddWithValue(
            "$sourceNodeId", connection.SourceNodeId.ToString());

        command.Parameters.AddWithValue(
            "$destinationNodeId", connection.DestinationNodeId.ToString());

        command.Parameters.AddWithValue(
            "$signalStrength",
            (object?)connection.SignalStrength ?? DBNull.Value);

        command.Parameters.AddWithValue(
            "$linkQuality", connection.LinkQuality);

        command.Parameters.AddWithValue(
            "$latency", connection.Latency);

        command.Parameters.AddWithValue(
            "$energyCost", connection.EnergyCost);

        command.Parameters.AddWithValue(
            "$routingCost", connection.RoutingCost);

        command.Parameters.AddWithValue(
            "$isActive", connection.IsActive ? 1 : 0);

        command.Parameters.AddWithValue(
            "$lastUpdated", connection.LastUpdated.ToString("O"));

        command.Parameters.AddWithValue(
            "$createdAt", connection.CreatedAt.ToString("O"));
    }

    private static MeshConnection Map(SqliteDataReader reader)
    {
        return new MeshConnection
        {
            Id = Guid.Parse(reader.GetString(0)),
            SourceNodeId = Guid.Parse(reader.GetString(1)),
            DestinationNodeId = Guid.Parse(reader.GetString(2)),
            SignalStrength = reader.IsDBNull(3) ? null : reader.GetDouble(3),
            LinkQuality = reader.GetDouble(4),
            Latency = reader.GetDouble(5),
            EnergyCost = reader.GetDouble(6),
            RoutingCost = reader.GetDouble(7),
            IsActive = reader.GetInt32(8) == 1,
            LastUpdated = DateTime.Parse(reader.GetString(9)),
            CreatedAt = DateTime.Parse(reader.GetString(10))
        };
    }

    public async Task<MeshConnection?> GetByIdAsync(
    Guid id,
    CancellationToken cancellationToken = default)
    {
        using var connection = _database.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();

        command.CommandText = """
        SELECT *
        FROM MeshConnections
        WHERE Id = $id;
        """;

        command.Parameters.AddWithValue("$id", id.ToString());

        using var reader = await command.ExecuteReaderAsync(
            cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return Map(reader);
    }

    public async Task<IReadOnlyList<MeshConnection>> GetByGatewayIdAsync(
    Guid gatewayId,
    CancellationToken cancellationToken = default)
    {
        if (gatewayId == Guid.Empty)
            return [];

        using var connection = _database.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();

        command.CommandText = """
        SELECT *
        FROM MeshConnections
        WHERE GatewayId = $gatewayId
        ORDER BY LastUpdated DESC;
        """;

        command.Parameters.AddWithValue(
            "$gatewayId",
            gatewayId.ToString());

        using var reader = await command.ExecuteReaderAsync(
            cancellationToken);

        var connections = new List<MeshConnection>();

        while (await reader.ReadAsync(cancellationToken))
        {
            connections.Add(Map(reader));
        }

        return connections;
    }
}