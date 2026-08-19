using Microsoft.Data.Sqlite;
using SmartX.Domain.Entities;
using SmartX.WPF.Data;
using SmartX.WPF.Data.Mappers;

namespace SmartX.WPF.Repositories.Local;

public class SQLiteTelemetryCache(
    SmartXCacheDatabase database) : ILocalTelemetryCache
{
    private readonly SmartXCacheDatabase _database = database;

    public async Task<IReadOnlyList<Telemetry>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        using var connection = _database.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT
                Id,
                SensorId,
                Timestamp,
                Voltage,
                Current,
                Power,
                Temperature,
                CreatedAt
            FROM Telemetry;
            """;

        using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        var telemetry = new List<Telemetry>();

        while (await reader.ReadAsync(cancellationToken))
        {
            telemetry.Add(TelemetryMapper.Map(reader));
        }

        return telemetry;
    }

    public async Task<Telemetry?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var connection = _database.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT
                Id,
                SensorId,
                Timestamp,
                Voltage,
                Current,
                Power,
                Temperature,
                CreatedAt
            FROM Telemetry
            WHERE Id = $id;
            """;

        command.Parameters.AddWithValue(
            "$id",
            id.ToString());

        using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return TelemetryMapper.Map(reader);
    }

    public async Task<IReadOnlyList<Telemetry>> GetBySensorAndDateAsync(
        Guid sensorId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        using var connection = _database.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT
                Id,
                SensorId,
                Timestamp,
                Voltage,
                Current,
                Power,
                Temperature,
                CreatedAt
            FROM Telemetry
            WHERE SensorId = $sensorId
              AND Timestamp >= $from
              AND Timestamp <= $to
            ORDER BY Timestamp ASC;
            """;

        command.Parameters.AddWithValue(
            "$sensorId",
            sensorId.ToString());

        command.Parameters.AddWithValue(
            "$from",
            from.ToString("O"));

        command.Parameters.AddWithValue(
            "$to",
            to.ToString("O"));

        using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        var telemetry = new List<Telemetry>();

        while (await reader.ReadAsync(cancellationToken))
        {
            telemetry.Add(TelemetryMapper.Map(reader));
        }

        return telemetry;
    }

    public async Task<IReadOnlyList<Telemetry>> GetBySensorIdAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default)
    {
        using var connection = _database.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT
                Id,
                SensorId,
                Timestamp,
                Voltage,
                Current,
                Power,
                Temperature,
                CreatedAt
            FROM Telemetry
            WHERE SensorId = $sensorId
            ORDER BY Timestamp ASC;
            """;

        command.Parameters.AddWithValue(
            "$sensorId",
            sensorId.ToString());

        using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        var telemetry = new List<Telemetry>();

        while (await reader.ReadAsync(cancellationToken))
        {
            telemetry.Add(TelemetryMapper.Map(reader));
        }

        return telemetry;
    }

    public async Task<IReadOnlyList<Telemetry>> GetLatestBySensorIdAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default)
    {
        using var connection = _database.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT
                Id,
                SensorId,
                Timestamp,
                Voltage,
                Current,
                Power,
                Temperature,
                CreatedAt
            FROM Telemetry
            WHERE SensorId = $sensorId
            ORDER BY Timestamp DESC
            LIMIT 1;
            """;

        command.Parameters.AddWithValue(
            "$sensorId",
            sensorId.ToString());

        using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        var telemetry = new List<Telemetry>();

        while (await reader.ReadAsync(cancellationToken))
        {
            telemetry.Add(TelemetryMapper.Map(reader));
        }

        return telemetry;
    }

    public async Task UpdateAsync(
        Telemetry telemetry,
        CancellationToken cancellationToken = default)
    {
        using var connection = _database.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();

        command.CommandText = """
            UPDATE Telemetry
            SET
                SensorId = $sensorId,
                Timestamp = $timestamp,
                Voltage = $voltage,
                Current = $current,
                Power = $power,
                Temperature = $temperature,
                CreatedAt = $createdAt
            WHERE Id = $id;
            """;

        command.Parameters.AddWithValue(
            "$id",
            telemetry.Id.ToString());

        command.Parameters.AddWithValue(
            "$sensorId",
            telemetry.SensorId.ToString());

        command.Parameters.AddWithValue(
            "$timestamp",
            telemetry.Timestamp.ToString("O"));

        command.Parameters.AddWithValue(
            "$voltage",
            telemetry.Voltage ?? (object)DBNull.Value);

        command.Parameters.AddWithValue(
            "$current",
            telemetry.Current ?? (object)DBNull.Value);

        command.Parameters.AddWithValue(
            "$power",
            telemetry.Power ?? (object)DBNull.Value);

        command.Parameters.AddWithValue(
            "$temperature",
            telemetry.Temperature ?? (object)DBNull.Value);

        command.Parameters.AddWithValue(
            "$createdAt",
            telemetry.CreatedAt.ToString("O"));

        var rowsAffected =
            await command.ExecuteNonQueryAsync(cancellationToken);

        if (rowsAffected == 0)
        {
            command.CommandText = """
                INSERT INTO Telemetry
                (
                    Id,
                    SensorId,
                    Timestamp,
                    Voltage,
                    Current,
                    Power,
                    Temperature,
                    CreatedAt
                )
                VALUES
                (
                    $id,
                    $sensorId,
                    $timestamp,
                    $voltage,
                    $current,
                    $power,
                    $temperature,
                    $createdAt
                );
                """;

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}