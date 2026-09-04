using SmartX.Domain.Entities;
using SmartX.WPF.Data;
using SmartX.WPF.Data.Mappers;

namespace SmartX.WPF.Repositories.Local;

public class SQLiteCompanyCache(
    SmartXCacheDatabase database) : ILocalCompanyCache
{
    private readonly SmartXCacheDatabase _database = database;

    public async Task<Company?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var connection = _database.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT
                Id,
                Name,
                Description,
                IsActive,
                DeletionRequested,
                CreatedAt,
                UpdatedAt
            FROM Companies
            WHERE Id = $id;
            """;

        command.Parameters.AddWithValue("$id", id.ToString());

        using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return CompanyMapper.Map(reader);
    }

    public async Task<IReadOnlyList<Company>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        using var connection = _database.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT
                Id,
                Name,
                Description,
                IsActive,
                DeletionRequested,
                CreatedAt,
                UpdatedAt
            FROM Companies
            ORDER BY Name;
            """;

        using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        var companies = new List<Company>();

        while (await reader.ReadAsync(cancellationToken))
        {
            companies.Add(CompanyMapper.Map(reader));
        }

        return companies;
    }

    public async Task UpdateAsync(
        Company company,
        CancellationToken cancellationToken = default)
    {
        using var connection = _database.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();

        command.CommandText = """
            UPDATE Companies
            SET
                Name = $name,
                Description = $description,
                IsActive = $isActive,
                DeletionRequested = $deletionRequested,
                CreatedAt = $createdAt,
                UpdatedAt = $updatedAt
            WHERE Id = $id;
            """;

        command.Parameters.AddWithValue(
            "$id", company.Id.ToString());

        command.Parameters.AddWithValue(
            "$name", company.Name);

        command.Parameters.AddWithValue(
            "$description", company.Description);

        command.Parameters.AddWithValue(
            "$isActive", company.IsActive ? 1 : 0);

        command.Parameters.AddWithValue(
            "$deletionRequested", company.DeletionRequested ? 1 : 0);

        command.Parameters.AddWithValue(
            "$createdAt", company.CreatedAt.ToString("O"));

        command.Parameters.AddWithValue(
            "$updatedAt", company.UpdatedAt.ToString("O"));

        var rowsAffected =
            await command.ExecuteNonQueryAsync(cancellationToken);

        if (rowsAffected > 0)
            return;

        command.CommandText = """
            INSERT INTO Companies
            (
                Id,
                Name,
                Description,
                IsActive,
                DeletionRequested,
                CreatedAt,
                UpdatedAt
            )
            VALUES
            (
                $id,
                $name,
                $description,
                $isActive,
                $deletionRequested,
                $createdAt,
                $updatedAt
            );
            """;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var connection = _database.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();

        command.CommandText = """
            DELETE FROM Companies
            WHERE Id = $id;
            """;

        command.Parameters.AddWithValue("$id", id.ToString());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}