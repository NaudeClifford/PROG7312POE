using Microsoft.Data.Sqlite;
using SmartX.Domain.Entities.Mesh;
using SmartX.Domain.Enums.Mesh;

namespace SmartX.WPF.Data.Mappers.Mesh;

public static class MeshLogMapper
{
    public static MeshLog Map(SqliteDataReader reader)
    {
        var id = reader.GetOrdinal("Id");
        var nodeId = reader.GetOrdinal("NodeId");
        var connectionId = reader.GetOrdinal("ConnectionId");
        var eventType = reader.GetOrdinal("EventType");
        var message = reader.GetOrdinal("Message");
        var severity = reader.GetOrdinal("Severity");
        var details = reader.GetOrdinal("Details");
        var timestamp = reader.GetOrdinal("Timestamp");

        return new MeshLog
        {
            Id = Guid.Parse(reader.GetString(id)),

            NodeId = reader.IsDBNull(nodeId)
                ? null
                : Guid.Parse(reader.GetString(nodeId)),

            ConnectionId = reader.IsDBNull(connectionId)
                ? null
                : Guid.Parse(reader.GetString(connectionId)),

            EventType = reader.GetString(eventType),
            Message = reader.GetString(message),

            Severity = (MeshLogSeverity)reader.GetInt32(severity),

            Details = reader.IsDBNull(details)
                ? null
                : reader.GetString(details),

            Timestamp = DateTime.Parse(reader.GetString(timestamp))
        };
    }
}