using Microsoft.Data.Sqlite;

namespace SmartX.WPF.Data;

public class SmartXCacheDatabase
{
    private readonly string _connectionString;

    public SmartXCacheDatabase()
    {
        _connectionString = "Data Source=smartx-cache.db";
    }

    public SqliteConnection CreateConnection()
    {
        return new SqliteConnection(_connectionString);
    }

    public async Task InitializeAsync()
    {
        using var connection = CreateConnection();

        await connection.OpenAsync();

        using var command = connection.CreateCommand();

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
            CreatedAt TEXT NOT NULL
        );
        """;

        await command.ExecuteNonQueryAsync();

        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Telemetry
            (
                Id TEXT PRIMARY KEY,
                SensorId TEXT NOT NULL,
                Timestamp TEXT NOT NULL,
                Voltage REAL NULL,
                Current REAL NULL,
                Power REAL NULL,
                Temperature REAL NULL
            );
            """;

        await command.ExecuteNonQueryAsync();

        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Users
            (
                Id TEXT PRIMARY KEY,
                FirebaseUid TEXT NOT NULL,
                Email TEXT NOT NULL,
                DisplayName TEXT NOT NULL,
                Role INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL
            );
            """;

        await command.ExecuteNonQueryAsync();
    }
}