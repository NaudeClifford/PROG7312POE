using Microsoft.Data.Sqlite;
using SmartX.Domain.Entities;

namespace SmartX.WPF.Data.Mappers;

public static class CompanyMapper
{
    public static Company Map(SqliteDataReader reader)
    {
        var id = reader.GetOrdinal("Id");
        var name = reader.GetOrdinal("Name");
        var description = reader.GetOrdinal("Description");
        var isActive = reader.GetOrdinal("IsActive");
        var createdAt = reader.GetOrdinal("CreatedAt");
        var updatedAt = reader.GetOrdinal("UpdatedAt");

        return new Company
        {
            Id = Guid.Parse(reader.GetString(id)),
            Name = reader.GetString(name),
            Description = reader.GetString(description),
            IsActive = reader.GetInt32(isActive) == 1,
            CreatedAt = DateTime.Parse(reader.GetString(createdAt)),
            UpdatedAt = DateTime.Parse(reader.GetString(updatedAt))
        };
    }
}