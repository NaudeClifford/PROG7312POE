using Microsoft.Data.Sqlite;
using SmartX.Domain.Entities.Mesh;
using SmartX.Domain.Enums.Mesh;

namespace SmartX.WPF.Data.Mappers.Mesh;

public static class MeshNodeMapper
{
    public static MeshNode Map(SqliteDataReader reader)
    {
        var id = reader.GetOrdinal("Id");
        var gatewayId = reader.GetOrdinal("GatewayId");
        var deviceId = reader.GetOrdinal("DeviceId");
        var nodeType = reader.GetOrdinal("NodeType");
        var deviceIdentifier = reader.GetOrdinal("DeviceIdentifier");
        var name = reader.GetOrdinal("Name");
        var status = reader.GetOrdinal("Status");
        var isGateway = reader.GetOrdinal("IsGateway");
        var batteryLevel = reader.GetOrdinal("BatteryLevel");
        var lastSeen = reader.GetOrdinal("LastSeen");
        var createdAt = reader.GetOrdinal("CreatedAt");
        var updatedAt = reader.GetOrdinal("UpdatedAt");

        return new MeshNode
        {
            Id = Guid.Parse(reader.GetString(id)),
            GatewayId = Guid.Parse(reader.GetString(gatewayId)),
            DeviceId = Guid.Parse(reader.GetString(deviceId)),
            NodeType = (MeshNodeType)reader.GetInt32(nodeType),
            DeviceIdentifier = reader.GetString(deviceIdentifier),
            Name = reader.GetString(name),
            Status = (MeshNodeStatus)reader.GetInt32(status),
            IsGateway = reader.GetBoolean(isGateway),

            BatteryLevel = reader.IsDBNull(batteryLevel)
                ? null
                : reader.GetDouble(batteryLevel),

            LastSeen = reader.IsDBNull(lastSeen)
                ? null
                : DateTime.Parse(reader.GetString(lastSeen)),

            CreatedAt = DateTime.Parse(reader.GetString(createdAt)),
            UpdatedAt = DateTime.Parse(reader.GetString(updatedAt))
        };
    }
}