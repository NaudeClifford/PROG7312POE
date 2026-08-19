using SmartX.Domain.Entities;
using SmartX.WPF.Data;
using SmartX.WPF.Data.Mappers;

namespace SmartX.WPF.Repositories.Local;

public class SQLiteGatewayCache(
    SmartXCacheDatabase database) : ILocalGatewayCache
{
    private readonly SmartXCacheDatabase _database = database;

    public async Task<IReadOnlyList<Gateway>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        using var connection = _database.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT
                Id,
                CompanyId,
                Name,
                Description,
                SerialNumber,
                IpAddress,
                IsActive,
                CreatedAt,
                UpdatedAt
            FROM Gateways;
            """;

        using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        var gateways = new List<Gateway>();

        while (await reader.ReadAsync(cancellationToken))
        {
            gateways.Add(GatewayMapper.Map(reader));
        }

        return gateways;
    }

    public async Task<Gateway?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var connection = _database.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT
                Id,
                CompanyId,
                Name,
                Description,
                SerialNumber,
                IpAddress,
                IsActive,
                CreatedAt,
                UpdatedAt
            FROM Gateways
            WHERE Id = $id;
            """;

        command.Parameters.AddWithValue(
            "$id",
            id.ToString());

        using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return GatewayMapper.Map(reader);
    }

    public async Task<IReadOnlyList<Gateway>> GetByCompanyIdAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        using var connection = _database.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT
                Id,
                CompanyId,
                Name,
                Description,
                SerialNumber,
                IpAddress,
                IsActive,
                CreatedAt,
                UpdatedAt
            FROM Gateways
            WHERE CompanyId = $companyId;
            """;

        command.Parameters.AddWithValue(
            "$companyId",
            companyId.ToString());

        using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        var gateways = new List<Gateway>();

        while (await reader.ReadAsync(cancellationToken))
        {
            gateways.Add(GatewayMapper.Map(reader));
        }

        return gateways;
    }

    public async Task UpdateAsync(
        Gateway gateway,
        CancellationToken cancellationToken = default)
    {
        using var connection = _database.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();

        command.CommandText = """
            UPDATE Gateways
            SET
                CompanyId = $companyId,
                Name = $name,
                Description = $description,
                SerialNumber = $serialNumber,
                IpAddress = $ipAddress,
                IsActive = $isActive,
                CreatedAt = $createdAt,
                UpdatedAt = $updatedAt
            WHERE Id = $id;
            """;

        command.Parameters.AddWithValue(
            "$id",
            gateway.Id.ToString());

        command.Parameters.AddWithValue(
            "$companyId",
            gateway.CompanyId.ToString());

        command.Parameters.AddWithValue(
            "$name",
            gateway.Name);

        command.Parameters.AddWithValue(
            "$description",
            gateway.Description);

        command.Parameters.AddWithValue(
            "$serialNumber",
            gateway.SerialNumber ?? (object)DBNull.Value);

        command.Parameters.AddWithValue(
            "$ipAddress",
            gateway.IpAddress ?? (object)DBNull.Value);

        command.Parameters.AddWithValue(
            "$isActive",
            gateway.IsActive ? 1 : 0);

        command.Parameters.AddWithValue(
            "$createdAt",
            gateway.CreatedAt.ToString("O"));

        command.Parameters.AddWithValue(
            "$updatedAt",
            gateway.UpdatedAt.ToString("O"));

        var rowsAffected =
            await command.ExecuteNonQueryAsync(cancellationToken);

        if (rowsAffected == 0)
        {
            command.CommandText = """
                INSERT INTO Gateways
                (
                    Id,
                    CompanyId,
                    Name,
                    Description,
                    SerialNumber,
                    IpAddress,
                    IsActive,
                    CreatedAt,
                    UpdatedAt
                )
                VALUES
                (
                    $id,
                    $companyId,
                    $name,
                    $description,
                    $serialNumber,
                    $ipAddress,
                    $isActive,
                    $createdAt,
                    $updatedAt
                );
                """;

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}