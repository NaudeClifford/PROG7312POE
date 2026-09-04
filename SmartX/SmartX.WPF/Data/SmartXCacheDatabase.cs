using Microsoft.Data.Sqlite;

namespace SmartX.WPF.Data;

public class SmartXCacheDatabase
{
    private readonly string _connectionString;

    public SmartXCacheDatabase()
    {
        _connectionString =
            "Data Source=smartx-cache.db";
    }

    public SqliteConnection CreateConnection()
    {
        return new SqliteConnection(_connectionString);
    }

    public async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();

        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();

        // SENSORS

        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Sensors
            (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                DeviceIdentifier TEXT NOT NULL,
                Category INTEGER NOT NULL,
                Location TEXT NOT NULL,
                Description TEXT NOT NULL,
                IsActive INTEGER NOT NULL,
                GatewayId TEXT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );
            """;

        await command.ExecuteNonQueryAsync(
            cancellationToken);


        // TELEMETRY

        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Telemetry
            (
                Id TEXT PRIMARY KEY,
                SensorId TEXT NOT NULL,
                Timestamp TEXT NOT NULL,
                Voltage REAL NULL,
                Current REAL NULL,
                Power REAL NULL,
                Temperature REAL NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );
            """;

        await command.ExecuteNonQueryAsync(
            cancellationToken);


        // USERS

        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Users
            (
                Id TEXT PRIMARY KEY,
                CompanyId TEXT NOT NULL,
                FirebaseUid TEXT NOT NULL,
                Email TEXT NOT NULL,
                DisplayName TEXT NOT NULL,
                Role INTEGER NOT NULL,
                IsActive INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );
            """;

        await command.ExecuteNonQueryAsync(
            cancellationToken);


        // COMPANIES

        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Companies
            (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                Description TEXT NOT NULL,
                IsActive INTEGER NOT NULL,
                DeletionRequested INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );
            """;

        await command.ExecuteNonQueryAsync(
            cancellationToken);


        // =====================================================
        // GATEWAYS
        // =====================================================

        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Gateways
            (
                Id TEXT PRIMARY KEY,
                CompanyId TEXT NOT NULL,
                Name TEXT NOT NULL,
                Description TEXT NOT NULL,
                SerialNumber TEXT NULL,
                IpAddress TEXT NULL,
                IsActive INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );
            """;

        await command.ExecuteNonQueryAsync(
            cancellationToken);


        // =====================================================
        // SENSOR LOG FILES
        // =====================================================

        command.CommandText = """
            CREATE TABLE IF NOT EXISTS SensorLogFiles
            (
                Id TEXT PRIMARY KEY,
                SensorId TEXT NOT NULL,
                FileName TEXT NOT NULL,
                ContentType TEXT NOT NULL,
                FileSize INTEGER NOT NULL,
                UploadedAt TEXT NOT NULL,
                UploadedByUserId TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );
            """;

        await command.ExecuteNonQueryAsync(
            cancellationToken);


        // =====================================================
        // CACHE METADATA
        // =====================================================

        command.CommandText = """
            CREATE TABLE IF NOT EXISTS CacheMetadata
            (
                EntityId TEXT PRIMARY KEY,
                EntityType TEXT NOT NULL,
                LastSyncedAt TEXT NOT NULL
            );
            """;

        await command.ExecuteNonQueryAsync(
            cancellationToken);
    }
}