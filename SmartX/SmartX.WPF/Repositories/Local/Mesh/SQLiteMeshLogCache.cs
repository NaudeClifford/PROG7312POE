using Microsoft.Data.Sqlite;
using SmartX.Domain.Entities.Mesh;
using SmartX.Domain.Enums.Mesh;
using SmartX.WPF.Data;

namespace SmartX.WPF.Repositories.Local;

public class SQLiteMeshLogCache(
    SmartXCacheDatabase database) : ILocalMeshLogCache
{
    private readonly SmartXCacheDatabase _database = database;

    public async Task<IReadOnlyList<MeshLog>> GetByGatewayIdAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        using var connection = _database.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT * FROM MeshLogs
            WHERE GatewayId = $gatewayId
            ORDER BY Timestamp DESC;
            """;

        command.Parameters.AddWithValue("$gatewayId", gatewayId.ToString());

        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var logs = new List<MeshLog>();

        while (await reader.ReadAsync(cancellationToken))
            logs.Add(Map(reader));

        return logs;
    }

    public async Task<IReadOnlyList<MeshLog>> GetByNodeIdAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default)
    {
        using var connection = _database.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT * FROM MeshLogs
            WHERE NodeId = $nodeId
            ORDER BY Timestamp DESC;
            """;

        command.Parameters.AddWithValue("$nodeId", nodeId.ToString());

        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var logs = new List<MeshLog>();

        while (await reader.ReadAsync(cancellationToken))
            logs.Add(Map(reader));

        return logs;
    }

    public async Task AddAsync(
        MeshLog log,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(log);

        using var connection = _database.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();

        command.CommandText = """
            INSERT INTO MeshLogs
            (
                Id, GatewayId, NodeId, ConnectionId,
                EventType, Message, Severity, Details, Timestamp
            )
            VALUES
            (
                $id, $gatewayId, $nodeId, $connectionId,
                $eventType, $message, $severity, $details, $timestamp
            );
            """;

        command.Parameters.AddWithValue("$id", log.Id.ToString());
        command.Parameters.AddWithValue("$gatewayId", log.GatewayId.ToString());
        command.Parameters.AddWithValue("$nodeId", log.NodeId?.ToString() ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$connectionId", log.ConnectionId?.ToString() ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$eventType", log.EventType);
        command.Parameters.AddWithValue("$message", log.Message);
        command.Parameters.AddWithValue("$severity", (int)log.Severity);
        command.Parameters.AddWithValue("$details", (object?)log.Details ?? DBNull.Value);
        command.Parameters.AddWithValue("$timestamp", log.Timestamp.ToString("O"));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteOlderThanAsync(
        DateTime cutoff,
        CancellationToken cancellationToken = default)
    {
        using var connection = _database.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();

        command.CommandText = """
            DELETE FROM MeshLogs
            WHERE Timestamp < $cutoff;
            """;

        command.Parameters.AddWithValue("$cutoff", cutoff.ToString("O"));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static MeshLog Map(SqliteDataReader reader)
    {
        return new MeshLog
        {
            Id = Guid.Parse(reader.GetString(0)),
            GatewayId = Guid.Parse(reader.GetString(1)),
            NodeId = reader.IsDBNull(2) ? null : Guid.Parse(reader.GetString(2)),
            ConnectionId = reader.IsDBNull(3) ? null : Guid.Parse(reader.GetString(3)),
            EventType = reader.GetString(4),
            Message = reader.GetString(5),
            Severity = (MeshLogSeverity)reader.GetInt32(6),
            Details = reader.IsDBNull(7) ? null : reader.GetString(7),
            Timestamp = DateTime.Parse(reader.GetString(8))
        };
    }
}