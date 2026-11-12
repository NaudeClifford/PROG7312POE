using Microsoft.Data.Sqlite;
using SmartX.Domain.Entities.Mesh;
using SmartX.Domain.Enums.Mesh;

namespace SmartX.WPF.Data.Mappers.Mesh;

public static class MeshLinkMapper
{
    public static MeshLink Map(SqliteDataReader reader)
    {
        var id = reader.GetOrdinal("Id");
        var gatewayId = reader.GetOrdinal("GatewayId");
        var sourceNodeId = reader.GetOrdinal("SourceNodeId");
        var sourceNodeType = reader.GetOrdinal("SourceNodeType");
        var targetNodeId = reader.GetOrdinal("TargetNodeId");
        var targetNodeType = reader.GetOrdinal("TargetNodeType");
        var signalStrength = reader.GetOrdinal("SignalStrengthDbm");
        var latency = reader.GetOrdinal("LatencyMilliseconds");
        var energyCost = reader.GetOrdinal("EnergyCost");
        var isAvailable = reader.GetOrdinal("IsAvailable");
        var lastSeen = reader.GetOrdinal("LastSeenAt");
        var createdAt = reader.GetOrdinal("CreatedAt");
        var updatedAt = reader.GetOrdinal("UpdatedAt");

        return new MeshLink
        {
            Id = Guid.Parse(reader.GetString(id)),
            GatewayId = Guid.Parse(reader.GetString(gatewayId)),

            SourceNodeId = Guid.Parse(reader.GetString(sourceNodeId)),
            SourceNodeType = (MeshNodeType)reader.GetInt32(sourceNodeType),

            TargetNodeId = Guid.Parse(reader.GetString(targetNodeId)),
            TargetNodeType = (MeshNodeType)reader.GetInt32(targetNodeType),

            SignalStrengthDbm = reader.IsDBNull(signalStrength)
                ? null
                : reader.GetDouble(signalStrength),

            LatencyMilliseconds = reader.IsDBNull(latency)
                ? null
                : reader.GetDouble(latency),

            EnergyCost = reader.IsDBNull(energyCost)
                ? null
                : reader.GetDouble(energyCost),

            IsAvailable = reader.GetBoolean(isAvailable),

            LastSeenAt = DateTime.Parse(reader.GetString(lastSeen)),
            CreatedAt = DateTime.Parse(reader.GetString(createdAt)),
            UpdatedAt = DateTime.Parse(reader.GetString(updatedAt))
        };
    }
}