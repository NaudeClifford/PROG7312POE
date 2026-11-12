using Microsoft.Data.Sqlite;
using SmartX.Domain.Entities.Mesh;
using SmartX.Domain.Enums.Mesh;
using SmartX.WPF.Data;

namespace SmartX.WPF.Repositories.Local;

public class SQLiteMeshNodeCache(
    SmartXCacheDatabase database) : ILocalMeshNodeCache
{
    private readonly SmartXCacheDatabase _database = database;

    public async Task<MeshNode?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var connection = _database.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT * FROM MeshNodes WHERE Id = $id;
            """;

        command.Parameters.AddWithValue("$id", id.ToString());

        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        return await reader.ReadAsync(cancellationToken)
            ? Map(reader)
            : null;
    }

    public async Task<IReadOnlyList<MeshNode>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        using var connection = _database.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT * FROM MeshNodes ORDER BY Name;
            """;

        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var nodes = new List<MeshNode>();

        while (await reader.ReadAsync(cancellationToken))
            nodes.Add(Map(reader));

        return nodes;
    }

    public async Task<IReadOnlyList<MeshNode>> GetByGatewayIdAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        using var connection = _database.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT * FROM MeshNodes
            WHERE GatewayId = $gatewayId
            ORDER BY Name;
            """;

        command.Parameters.AddWithValue(
            "$gatewayId", gatewayId.ToString());

        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var nodes = new List<MeshNode>();

        while (await reader.ReadAsync(cancellationToken))
            nodes.Add(Map(reader));

        return nodes;
    }

    public async Task UpdateAsync(
        MeshNode node,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(node);

        using var connection = _database.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();

        command.CommandText = """
            INSERT INTO MeshNodes
            (
                Id, GatewayId, DeviceId, NodeType,
                DeviceIdentifier, Name, Status, IsGateway,
                BatteryLevel, LastSeen, CreatedAt, UpdatedAt
            )
            VALUES
            (
                $id, $gatewayId, $deviceId, $nodeType,
                $deviceIdentifier, $name, $status, $isGateway,
                $batteryLevel, $lastSeen, $createdAt, $updatedAt
            )
            ON CONFLICT(Id) DO UPDATE SET
                GatewayId = excluded.GatewayId,
                DeviceId = excluded.DeviceId,
                NodeType = excluded.NodeType,
                DeviceIdentifier = excluded.DeviceIdentifier,
                Name = excluded.Name,
                Status = excluded.Status,
                IsGateway = excluded.IsGateway,
                BatteryLevel = excluded.BatteryLevel,
                LastSeen = excluded.LastSeen,
                CreatedAt = excluded.CreatedAt,
                UpdatedAt = excluded.UpdatedAt;
            """;

        command.Parameters.AddWithValue("$id", node.Id.ToString());
        command.Parameters.AddWithValue("$gatewayId", node.GatewayId.ToString());
        command.Parameters.AddWithValue("$deviceId", node.DeviceId.ToString());
        command.Parameters.AddWithValue("$nodeType", (int)node.NodeType);
        command.Parameters.AddWithValue("$deviceIdentifier", node.DeviceIdentifier);
        command.Parameters.AddWithValue("$name", node.Name);
        command.Parameters.AddWithValue("$status", (int)node.Status);
        command.Parameters.AddWithValue("$isGateway", node.IsGateway ? 1 : 0);
        command.Parameters.AddWithValue("$batteryLevel", (object?)node.BatteryLevel ?? DBNull.Value);
        command.Parameters.AddWithValue("$lastSeen", node.LastSeen?.ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$createdAt", node.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("$updatedAt", node.UpdatedAt.ToString("O"));

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
            DELETE FROM MeshNodes WHERE Id = $id;
            """;

        command.Parameters.AddWithValue("$id", id.ToString());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static MeshNode Map(SqliteDataReader reader)
    {
        return new MeshNode
        {
            Id = Guid.Parse(reader.GetString(0)),
            GatewayId = Guid.Parse(reader.GetString(1)),
            DeviceId = Guid.Parse(reader.GetString(2)),
            NodeType = (MeshNodeType)reader.GetInt32(3),
            DeviceIdentifier = reader.GetString(4),
            Name = reader.GetString(5),
            Status = (MeshNodeStatus)reader.GetInt32(6),
            IsGateway = reader.GetInt32(7) == 1,
            BatteryLevel = reader.IsDBNull(8) ? null : reader.GetDouble(8),
            LastSeen = reader.IsDBNull(9)
                ? null
                : DateTime.Parse(reader.GetString(9)),
            CreatedAt = DateTime.Parse(reader.GetString(10)),
            UpdatedAt = DateTime.Parse(reader.GetString(11))
        };
    }
}