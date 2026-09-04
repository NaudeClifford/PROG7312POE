using Microsoft.Data.Sqlite;
using SmartX.Domain.Entities;
using SmartX.Domain.Enums;
using SmartX.Shared.DTOs;

namespace SmartX.WPF.Data.Mappers;

public static class UserMapper
{
    public static UserDto Map(SqliteDataReader reader)
    {
        var id = reader.GetOrdinal("Id");
        var companyId = reader.GetOrdinal("CompanyId");
        var firebaseUid = reader.GetOrdinal("FirebaseUid");
        var email = reader.GetOrdinal("Email");
        var displayName = reader.GetOrdinal("DisplayName");
        var role = reader.GetOrdinal("Role");
        var isActive = reader.GetOrdinal("IsActive");
        var createdAt = reader.GetOrdinal("CreatedAt");

        return new UserDto
        {
            Id = Guid.Parse(
                reader.GetString(id)),

            CompanyId = Guid.Parse(
                reader.GetString(companyId)),

            FirebaseUid = reader.GetString(
                firebaseUid),

            Email = reader.GetString(
                email),

            DisplayName = reader.GetString(
                displayName),

            Role = (UserRole)reader.GetInt32(
                role),

            IsActive = reader.GetInt32(
                isActive) != 0,

            CreatedAt = DateTime.Parse(
                reader.GetString(createdAt))
        };
    }
}