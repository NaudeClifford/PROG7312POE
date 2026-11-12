using Microsoft.Data.Sqlite;
using SmartX.Domain.Entities.Mesh;

namespace SmartX.WPF.Data.Mappers.Mesh;

public static class MeshConnectionMapper
{
    public static MeshConnection Map(SqliteDataReader reader)
    {
        var id = reader.GetOrdinal("Id");
        var sourceNodeId = reader.GetOrdinal("SourceNodeId");
        var destinationNodeId = reader.GetOrdinal("DestinationNodeId");
        var signalStrength = reader.GetOrdinal("SignalStrength");
        var linkQuality = reader.GetOrdinal("LinkQuality");
        var latency = reader.GetOrdinal("Latency");
        var energyCost = reader.GetOrdinal("EnergyCost");
        var routingCost = reader.GetOrdinal("RoutingCost");
        var isActive = reader.GetOrdinal("IsActive");
        var lastUpdated = reader.GetOrdinal("LastUpdated");
        var createdAt = reader.GetOrdinal("CreatedAt");

        return new MeshConnection
        {
            Id = Guid.Parse(reader.GetString(id)),

            SourceNodeId = Guid.Parse(reader.GetString(sourceNodeId)),
            DestinationNodeId = Guid.Parse(reader.GetString(destinationNodeId)),

            SignalStrength = reader.IsDBNull(signalStrength)
                ? null
                : reader.GetDouble(signalStrength),

            LinkQuality = reader.GetDouble(linkQuality),
            Latency = reader.GetDouble(latency),
            EnergyCost = reader.GetDouble(energyCost),
            RoutingCost = reader.GetDouble(routingCost),

            IsActive = reader.GetBoolean(isActive),

            LastUpdated = DateTime.Parse(reader.GetString(lastUpdated)),
            CreatedAt = DateTime.Parse(reader.GetString(createdAt))
        };
    }
}