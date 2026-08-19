using Microsoft.Data.Sqlite;
using SmartX.Domain.Entities;

namespace SmartX.WPF.Data.Mappers;

public static class GatewayMapper
{
    public static Gateway Map(SqliteDataReader reader)
    {
        var id = reader.GetOrdinal("Id");
        var companyId = reader.GetOrdinal("CompanyId");
        var name = reader.GetOrdinal("Name");
        var description = reader.GetOrdinal("Description");
        var serialNumber = reader.GetOrdinal("SerialNumber");
        var ipAddress = reader.GetOrdinal("IpAddress");
        var isActive = reader.GetOrdinal("IsActive");
        var createdAt = reader.GetOrdinal("CreatedAt");
        var updatedAt = reader.GetOrdinal("UpdatedAt");

        return new Gateway
        {
            Id = Guid.Parse(reader.GetString(id)),
            CompanyId = Guid.Parse(reader.GetString(companyId)),
            Name = reader.GetString(name),
            Description = reader.GetString(description),

            SerialNumber = reader.IsDBNull(serialNumber)
                ? null
                : reader.GetString(serialNumber),

            IpAddress = reader.IsDBNull(ipAddress)
                ? null
                : reader.GetString(ipAddress),

            IsActive = reader.GetInt32(isActive) == 1,

            CreatedAt = DateTime.Parse(
                reader.GetString(createdAt)),

            UpdatedAt = DateTime.Parse(
                reader.GetString(updatedAt))
        };
    }
}