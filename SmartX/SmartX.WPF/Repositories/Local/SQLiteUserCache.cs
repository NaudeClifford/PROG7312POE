using Microsoft.Data.Sqlite;
using SmartX.Domain.Entities;
using SmartX.WPF.Data;
using SmartX.WPF.Data.Mappers;

namespace SmartX.WPF.Repositories.Local;

public class SQLiteUserCache(
    SmartXCacheDatabase database) : ILocalUserCache
{
    private readonly SmartXCacheDatabase _database = database;

    public async Task<User?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var connection = _database.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT
                Id,
                FirebaseUid,
                Email,
                DisplayName,
                Role,
                CreatedAt
            FROM Users
            WHERE Id = $id;
            """;

        command.Parameters.AddWithValue(
            "$id",
            id.ToString());

        using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return UserMapper.Map(reader);
    }

    public async Task<User?> GetByFirebaseUidAsync(
        string firebaseUid,
        CancellationToken cancellationToken = default)
    {
        using var connection = _database.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT
                Id,
                FirebaseUid,
                Email,
                DisplayName,
                Role,
                CreatedAt
            FROM Users
            WHERE FirebaseUid = $firebaseUid;
            """;

        command.Parameters.AddWithValue(
            "$firebaseUid",
            firebaseUid);

        using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return UserMapper.Map(reader);
    }

    public async Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        using var connection = _database.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT
                Id,
                FirebaseUid,
                Email,
                DisplayName,
                Role,
                CreatedAt
            FROM Users
            WHERE Email = $email;
            """;

        command.Parameters.AddWithValue(
            "$email",
            email);

        using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return UserMapper.Map(reader);
    }

    public async Task UpdateAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        using var connection = _database.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();

        command.CommandText = """
            UPDATE Users
            SET
                FirebaseUid = $firebaseUid,
                Email = $email,
                DisplayName = $displayName,
                Role = $role,
                CreatedAt = $createdAt
            WHERE Id = $id;
            """;

        command.Parameters.AddWithValue(
            "$id",
            user.Id.ToString());

        command.Parameters.AddWithValue(
            "$firebaseUid",
            user.FirebaseUid);

        command.Parameters.AddWithValue(
            "$email",
            user.Email);

        command.Parameters.AddWithValue(
            "$displayName",
            user.DisplayName);

        command.Parameters.AddWithValue(
            "$role",
            (int)user.Role);

        command.Parameters.AddWithValue(
            "$createdAt",
            user.CreatedAt.ToString("O"));

        var rowsAffected =
            await command.ExecuteNonQueryAsync(cancellationToken);

        if (rowsAffected == 0)
        {
            command.CommandText = """
                INSERT INTO Users
                (
                    Id,
                    FirebaseUid,
                    Email,
                    DisplayName,
                    Role,
                    CreatedAt
                )
                VALUES
                (
                    $id,
                    $firebaseUid,
                    $email,
                    $displayName,
                    $role,
                    $createdAt
                );
                """;

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}