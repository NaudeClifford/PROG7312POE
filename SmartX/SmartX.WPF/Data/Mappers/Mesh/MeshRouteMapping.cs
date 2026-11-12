using Microsoft.Data.Sqlite;
using SmartX.Domain.Entities.Mesh;
using SmartX.Domain.Enums.Mesh;
using System.Text.Json;

namespace SmartX.WPF.Data.Mappers.Mesh;

public static class MeshRouteMapper
{
    public static MeshRoute Map(SqliteDataReader reader)
    {
        var id = reader.GetOrdinal("Id");
        var sourceNodeId = reader.GetOrdinal("SourceNodeId");
        var destinationNodeId = reader.GetOrdinal("DestinationNodeId");
        var nodePath = reader.GetOrdinal("NodePath");
        var algorithm = reader.GetOrdinal("Algorithm");
        var totalCost = reader.GetOrdinal("TotalCost");
        var totalLatency = reader.GetOrdinal("TotalLatency");
        var hopCount = reader.GetOrdinal("HopCount");
        var isSuccessful = reader.GetOrdinal("IsSuccessful");
        var calculatedAt = reader.GetOrdinal("CalculatedAt");

        return new MeshRoute
        {
            Id = Guid.Parse(reader.GetString(id)),

            SourceNodeId = Guid.Parse(reader.GetString(sourceNodeId)),
            DestinationNodeId = Guid.Parse(reader.GetString(destinationNodeId)),

            NodePath = JsonSerializer.Deserialize<List<Guid>>(
                reader.GetString(nodePath)) ?? [],

            Algorithm = (RoutingAlgorithm)reader.GetInt32(algorithm),

            TotalCost = reader.GetDouble(totalCost),
            TotalLatency = reader.GetDouble(totalLatency),
            HopCount = reader.GetInt32(hopCount),

            IsSuccessful = reader.GetBoolean(isSuccessful),

            CalculatedAt = DateTime.Parse(reader.GetString(calculatedAt))
        };
    }
}