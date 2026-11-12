using Microsoft.Data.Sqlite;
using SmartX.Domain.Entities.Mesh;
using SmartX.Domain.Enums.Mesh;
using SmartX.WPF.Data;

namespace SmartX.WPF.Repositories.Local;

public class SQLiteMeshLinkCache(
    SmartXCacheDatabase database) : ILocalMeshLinkCache
{
    private readonly SmartXCacheDatabase _database = database;

    public async Task<MeshLink?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var connection = _database.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT * FROM MeshLinks WHERE Id = $id;
            """;

        command.Parameters.AddWithValue("$id", id.ToString());

        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        return await reader.ReadAsync(cancellationToken)
            ? Map(reader)
            : null;
    }

    public async Task<IReadOnlyList<MeshLink>> GetByGatewayIdAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        using var connection = _database.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT * FROM MeshLinks
            WHERE GatewayId = $gatewayId;
            """;

        command.Parameters.AddWithValue("$gatewayId", gatewayId.ToString());

        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var links = new List<MeshLink>();

        while (await reader.ReadAsync(cancellationToken))
            links.Add(Map(reader));

        return links;
    }

    public async Task<IReadOnlyList<MeshLink>> GetByNodeIdAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default)
    {
        using var connection = _database.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT * FROM MeshLinks
            WHERE SourceNodeId = $nodeId
               OR TargetNodeId = $nodeId;
            """;

        command.Parameters.AddWithValue("$nodeId", nodeId.ToString());

        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var links = new List<MeshLink>();

        while (await reader.ReadAsync(cancellationToken))
            links.Add(Map(reader));

        return links;
    }

    public async Task UpdateAsync(
        MeshLink link,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(link);

        using var connection = _database.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();

        command.CommandText = """
            INSERT INTO MeshLinks
            (
                Id, GatewayId, SourceNodeId, SourceNodeType,
                TargetNodeId, TargetNodeType, SignalStrengthDbm,
                LatencyMilliseconds, EnergyCost, IsAvailable,
                LastSeenAt, CreatedAt, UpdatedAt
            )
            VALUES
            (
                $id, $gatewayId, $sourceNodeId, $sourceNodeType,
                $targetNodeId, $targetNodeType, $signalStrength,
                $latency, $energyCost, $isAvailable,
                $lastSeenAt, $createdAt, $updatedAt
            )
            ON CONFLICT(Id) DO UPDATE SET
                GatewayId = excluded.GatewayId,
                SourceNodeId = excluded.SourceNodeId,
                SourceNodeType = excluded.SourceNodeType,
                TargetNodeId = excluded.TargetNodeId,
                TargetNodeType = excluded.TargetNodeType,
                SignalStrengthDbm = excluded.SignalStrengthDbm,
                LatencyMilliseconds = excluded.LatencyMilliseconds,
                EnergyCost = excluded.EnergyCost,
                IsAvailable = excluded.IsAvailable,
                LastSeenAt = excluded.LastSeenAt,
                CreatedAt = excluded.CreatedAt,
                UpdatedAt = excluded.UpdatedAt;
            """;

        command.Parameters.AddWithValue("$id", link.Id.ToString());
        command.Parameters.AddWithValue("$gatewayId", link.GatewayId.ToString());
        command.Parameters.AddWithValue("$sourceNodeId", link.SourceNodeId.ToString());
        command.Parameters.AddWithValue("$sourceNodeType", (int)link.SourceNodeType);
        command.Parameters.AddWithValue("$targetNodeId", link.TargetNodeId.ToString());
        command.Parameters.AddWithValue("$targetNodeType", (int)link.TargetNodeType);
        command.Parameters.AddWithValue("$signalStrength", (object?)link.SignalStrengthDbm ?? DBNull.Value);
        command.Parameters.AddWithValue("$latency", (object?)link.LatencyMilliseconds ?? DBNull.Value);
        command.Parameters.AddWithValue("$energyCost", (object?)link.EnergyCost ?? DBNull.Value);
        command.Parameters.AddWithValue("$isAvailable", link.IsAvailable ? 1 : 0);
        command.Parameters.AddWithValue("$lastSeenAt", link.LastSeenAt.ToString("O"));
        command.Parameters.AddWithValue("$createdAt", link.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("$updatedAt", link.UpdatedAt.ToString("O"));

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
            DELETE FROM MeshLinks WHERE Id = $id;
            """;

        command.Parameters.AddWithValue("$id", id.ToString());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static MeshLink Map(SqliteDataReader reader)
    {
        return new MeshLink
        {
            Id = Guid.Parse(reader.GetString(0)),
            GatewayId = Guid.Parse(reader.GetString(1)),
            SourceNodeId = Guid.Parse(reader.GetString(2)),
            SourceNodeType = (MeshNodeType)reader.GetInt32(3),
            TargetNodeId = Guid.Parse(reader.GetString(4)),
            TargetNodeType = (MeshNodeType)reader.GetInt32(5),
            SignalStrengthDbm = reader.IsDBNull(6) ? null : reader.GetDouble(6),
            LatencyMilliseconds = reader.IsDBNull(7) ? null : reader.GetDouble(7),
            EnergyCost = reader.IsDBNull(8) ? null : reader.GetDouble(8),
            IsAvailable = reader.GetInt32(9) == 1,
            LastSeenAt = DateTime.Parse(reader.GetString(10)),
            CreatedAt = DateTime.Parse(reader.GetString(11)),
            UpdatedAt = DateTime.Parse(reader.GetString(12))
        };
    }
}