using Microsoft.Data.Sqlite;
using SmartX.Domain.Entities;
using SmartX.Domain.Enums;

namespace SmartX.WPF.Data.Mappers;

public static class SensorMapper
{
    public static Sensor Map(SqliteDataReader reader)
    {
        var id = reader.GetOrdinal("Id");
        var name = reader.GetOrdinal("Name");
        var deviceIdentifier = reader.GetOrdinal("DeviceIdentifier");
        var category = reader.GetOrdinal("Category");
        var location = reader.GetOrdinal("Location");
        var description = reader.GetOrdinal("Description");
        var isActive = reader.GetOrdinal("IsActive");
        var gatewayId = reader.GetOrdinal("GatewayId");
        var createdAt = reader.GetOrdinal("CreatedAt");
        var updatedAt = reader.GetOrdinal("UpdatedAt");

        return new Sensor
        {
            Id = Guid.Parse(reader.GetString(id)),

            Name = reader.GetString(name),

            DeviceIdentifier = reader.GetString(deviceIdentifier),

            Category = (SensorCategory)reader.GetInt32(category),

            Location = reader.GetString(location),

            Description = reader.GetString(description),

            IsActive = reader.GetInt32(isActive) == 1,

            GatewayId = reader.IsDBNull(gatewayId)
                ? null
                : Guid.Parse(reader.GetString(gatewayId)),

            CreatedAt = DateTime.Parse(
                reader.GetString(createdAt)),

            UpdatedAt = DateTime.Parse(
                reader.GetString(updatedAt))
        };
    }
}