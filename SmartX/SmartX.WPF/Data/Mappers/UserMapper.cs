using Microsoft.Data.Sqlite;
using SmartX.Domain.Entities;
using SmartX.Domain.Enums;

namespace SmartX.WPF.Data.Mappers;

public static class UserMapper
{
    public static User Map(SqliteDataReader reader)
    {
        var id = reader.GetOrdinal("Id");
        var firebaseUid = reader.GetOrdinal("FirebaseUid");
        var email = reader.GetOrdinal("Email");
        var displayName = reader.GetOrdinal("DisplayName");
        var role = reader.GetOrdinal("Role");
        var createdAt = reader.GetOrdinal("CreatedAt");

        return new User
        {
            Id = Guid.Parse(reader.GetString(id)),

            FirebaseUid = reader.GetString(firebaseUid),

            Email = reader.GetString(email),

            DisplayName = reader.GetString(displayName),

            Role = (UserRole)reader.GetInt32(role),

            CreatedAt = DateTime.Parse(
                reader.GetString(createdAt))
        };
    }
}