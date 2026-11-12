
using Microsoft.Data.Sqlite;
using System.IO;

namespace SmartX.WPF.Data;

public class SmartXCacheDatabase
{
    private readonly string _connectionString;

    private const int CurrentSchemaVersion = 3;

    public SmartXCacheDatabase()
    {
        var databasePath = Path.Combine(
            AppContext.BaseDirectory,
            "Data",
            "Local",
            "smartx-cache.db");

        Directory.CreateDirectory(
            Path.GetDirectoryName(databasePath)!);

        _connectionString =
            $"Data Source={databasePath}";
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

        // Run schema migrations first.
        var version = await GetSchemaVersionAsync(
            connection,
            cancellationToken);

        if (version > CurrentSchemaVersion)
        {
            throw new InvalidOperationException(
                $"Database schema version {version} is newer than " +
                $"the supported version {CurrentSchemaVersion}.");
        }

        if (version < 1)
        {
            using var transaction =
                (SqliteTransaction)await connection.BeginTransactionAsync(
                    cancellationToken);

            await CreateInitialSensorsTableAsync(
                connection,
                transaction,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            version = 1;
        }

        if (version < 2)
        {
            using var transaction =
                (SqliteTransaction)await connection.BeginTransactionAsync(
                    cancellationToken);

            await AddSensorAlertFieldsAsync(
                connection,
                transaction,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            version = 2;
        }

        if (version < 3)
        {
            using var transaction =
                (SqliteTransaction)await connection.BeginTransactionAsync(
                    cancellationToken);

            await CreateMeshTablesAsync(
                connection,
                transaction,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            version = 3;
        }

        await SetSchemaVersionAsync(
            connection,
            version,
            cancellationToken);

        // Create remaining tables.
        using var command = connection.CreateCommand();

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

            CREATE TABLE IF NOT EXISTS CacheMetadata
            (
                EntityId TEXT PRIMARY KEY,
                EntityType TEXT NOT NULL,
                LastSyncedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS UserBehaviors
            (
                Id TEXT NOT NULL PRIMARY KEY,
                UserId TEXT NOT NULL,
                SensorId TEXT NOT NULL,
                Command TEXT NOT NULL,
                Timestamp TEXT NOT NULL,
                Succeeded INTEGER NOT NULL,
                Outcome TEXT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS IX_UserBehaviors_SensorId
            ON UserBehaviors (SensorId);
            """;

        await command.ExecuteNonQueryAsync(
            cancellationToken);
    }

    // SCHEMA VERSION

    private static async Task<int> GetSchemaVersionAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();

        command.CommandText = "PRAGMA user_version;";

        var result = await command.ExecuteScalarAsync(
            cancellationToken);

        return Convert.ToInt32(result);
    }

    private static async Task SetSchemaVersionAsync(
        SqliteConnection connection,
        int version,
        CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();

        command.CommandText =
            $"PRAGMA user_version = {version};";

        await command.ExecuteNonQueryAsync(
            cancellationToken);
    }

    // VERSION 1: INITIAL SENSOR TABLE

    private static async Task CreateInitialSensorsTableAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();

        command.Transaction = transaction;

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
                IpAddress TEXT NULL,
                Port INTEGER NULL,
                Protocol INTEGER NOT NULL DEFAULT 0,
                IsSimulation INTEGER NOT NULL DEFAULT 1,
                IsConnectionEnabled INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );
            """;

        await command.ExecuteNonQueryAsync(
            cancellationToken);
    }

    // VERSION 2: SENSOR ALERT FIELDS

    private static async Task AddSensorAlertFieldsAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();

        command.Transaction = transaction;

        command.CommandText = """
            ALTER TABLE Sensors
            ADD COLUMN HasActiveAlert INTEGER NOT NULL DEFAULT 0;

            ALTER TABLE Sensors
            ADD COLUMN ActiveAlertMessage TEXT NULL;

            ALTER TABLE Sensors
            ADD COLUMN ActiveAlertSeverity INTEGER NULL;
            """;

        await command.ExecuteNonQueryAsync(
            cancellationToken);

        //MESH

    }

    // VERSION 3: MESH TABLES

    private static async Task CreateMeshTablesAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();

        command.Transaction = transaction;

        command.CommandText = """
        CREATE TABLE IF NOT EXISTS MeshNodes
        (
            Id TEXT PRIMARY KEY,
            GatewayId TEXT NOT NULL,
            DeviceId TEXT NOT NULL,
            NodeType INTEGER NOT NULL,
            DeviceIdentifier TEXT NOT NULL,
            Name TEXT NOT NULL,
            Status INTEGER NOT NULL,
            IsGateway INTEGER NOT NULL,
            BatteryLevel REAL NULL,
            LastSeen TEXT NULL,
            CreatedAt TEXT NOT NULL,
            UpdatedAt TEXT NOT NULL
        );

        CREATE INDEX IF NOT EXISTS IX_MeshNodes_GatewayId
        ON MeshNodes(GatewayId);

        CREATE TABLE IF NOT EXISTS MeshLinks
        (
            Id TEXT PRIMARY KEY,
            GatewayId TEXT NOT NULL,
            SourceNodeId TEXT NOT NULL,
            SourceNodeType INTEGER NOT NULL,
            TargetNodeId TEXT NOT NULL,
            TargetNodeType INTEGER NOT NULL,
            SignalStrengthDbm REAL NULL,
            LatencyMilliseconds REAL NULL,
            EnergyCost REAL NULL,
            IsAvailable INTEGER NOT NULL,
            LastSeenAt TEXT NOT NULL,
            CreatedAt TEXT NOT NULL,
            UpdatedAt TEXT NOT NULL
        );

        CREATE INDEX IF NOT EXISTS IX_MeshLinks_GatewayId
        ON MeshLinks(GatewayId);

        CREATE TABLE IF NOT EXISTS MeshLogs
        (
            Id TEXT PRIMARY KEY,
            GatewayId TEXT NOT NULL,
            NodeId TEXT NULL,
            ConnectionId TEXT NULL,
            EventType TEXT NOT NULL,
            Message TEXT NOT NULL,
            Severity INTEGER NOT NULL,
            Details TEXT NULL,
            Timestamp TEXT NOT NULL
        );

        CREATE INDEX IF NOT EXISTS IX_MeshLogs_GatewayId
        ON MeshLogs(GatewayId);

        CREATE TABLE IF NOT EXISTS MeshRoutes
        (
            Id TEXT PRIMARY KEY,
            GatewayId TEXT NOT NULL,
            SourceNodeId TEXT NOT NULL,
            DestinationNodeId TEXT NOT NULL,
            NodePath TEXT NOT NULL,
            Algorithm INTEGER NOT NULL,
            TotalCost REAL NOT NULL,
            TotalLatency REAL NOT NULL,
            HopCount INTEGER NOT NULL,
            IsSuccessful INTEGER NOT NULL,
            CalculatedAt TEXT NOT NULL
        );

        CREATE INDEX IF NOT EXISTS IX_MeshRoutes_GatewayId
        ON MeshRoutes(GatewayId);

        CREATE TABLE IF NOT EXISTS MeshConnections
        (
            Id TEXT PRIMARY KEY,
            GatewayId TEXT NOT NULL,
            SourceNodeId TEXT NOT NULL,
            DestinationNodeId TEXT NOT NULL,
            SignalStrength REAL NULL,
            LinkQuality REAL NOT NULL,
            Latency REAL NOT NULL,
            EnergyCost REAL NOT NULL,
            RoutingCost REAL NOT NULL,
            IsActive INTEGER NOT NULL,
            LastUpdated TEXT NOT NULL,
            CreatedAt TEXT NOT NULL
        );

        CREATE INDEX IF NOT EXISTS IX_MeshConnections_SourceNodeId
        ON MeshConnections(SourceNodeId);

        CREATE INDEX IF NOT EXISTS IX_MeshConnections_DestinationNodeId
        ON MeshConnections(DestinationNodeId);
        """;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }


}
