using MapsterMapper;
using Microsoft.Data.Sqlite;
using SmartX.Domain.Entities;
using SmartX.Shared.DTOs;
using SmartX.WPF.Data;

namespace SmartX.WPF.Repositories.Local
{
    public class SQLiteUserBehaviorCache(SmartXCacheDatabase database, IMapper mapper) : ILocalUserBehaviorCache
    {
        private readonly SmartXCacheDatabase _database = database;
        private readonly IMapper _mapper = mapper;

        public async Task<UserBehaviorDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        {
            using var connection = _database.CreateConnection();

            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();

            command.CommandText = """
            SELECT
                Id,
                UserId,
                SensorId,
                Command,
                Timestamp,
                Succeeded,
                Outcome,
                CreatedAt,
                UpdatedAt
            FROM UserBehaviors
            WHERE Id = $id;
            """;

            command.Parameters.AddWithValue(
                "$id",
                id.ToString());

            using var reader =
                await command.ExecuteReaderAsync(
                    cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
                return null;

            var entity = ReadEntity(reader);
            return _mapper.Map<UserBehaviorDto>(entity);
        }

        // GET ALL

        public async Task<IReadOnlyList<UserBehaviorDto>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            using var connection = _database.CreateConnection();

            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();

            command.CommandText = """
            SELECT
                Id,
                UserId,
                SensorId,
                Command,
                Timestamp,
                Succeeded,
                Outcome,
                CreatedAt,
                UpdatedAt
            FROM UserBehaviors
            ORDER BY Timestamp;
            """;

            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var results = new List<UserBehaviorDto>();

            while (await reader.ReadAsync(cancellationToken))
            {
                var entity = ReadEntity(reader);
                results.Add(_mapper.Map<UserBehaviorDto>(entity));
            }

            return results;
        }

        // GET BY SENSOR

        public async Task<IReadOnlyList<UserBehaviorDto>> GetBySensorIdAsync(
            Guid sensorId,
            CancellationToken cancellationToken = default)
        {
            using var connection = _database.CreateConnection();

            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();

            command.CommandText = """
                SELECT
                    Id,
                    UserId,
                    SensorId,
                    Command,
                    Timestamp,
                    Succeeded,
                    Outcome,
                    CreatedAt,
                    UpdatedAt
                FROM UserBehaviors
                WHERE SensorId = $sensorId
                ORDER BY Timestamp;
                """;


            command.Parameters.AddWithValue("$sensorId", sensorId.ToString());

            using var reader = await command.ExecuteReaderAsync(
                cancellationToken);

            var results = new List<UserBehaviorDto>();

            while (await reader.ReadAsync(cancellationToken))
            {
                var entity = ReadEntity(reader);
                results.Add(_mapper.Map<UserBehaviorDto>(entity));
            }

            return results;
        }

        // UPDATE / INSERT

        public async Task UpdateAsync(
            UserBehaviorDto userBehavior,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(userBehavior);

            var entity = _mapper.Map<UserBehavior>(userBehavior);

            using var connection = _database.CreateConnection();

            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();

            command.CommandText = """
            UPDATE UserBehaviors
            SET
                UserId = $userId,
                SensorId = $sensorId,
                Command = $command,
                Timestamp = $timestamp,
                Succeeded = $succeeded,
                Outcome = $outcome,
                CreatedAt = $createdAt,
                UpdatedAt = $updatedAt
            WHERE Id = $id;
            """;

            command.Parameters.AddWithValue(
                "$id",
                entity.Id.ToString());

            command.Parameters.AddWithValue(
                "$userId",
                entity.UserId.ToString());

            command.Parameters.AddWithValue(
                "$sensorId",
                entity.SensorId.ToString());

            command.Parameters.AddWithValue(
                "$command",
                entity.Command);

            command.Parameters.AddWithValue(
                "$timestamp",
                entity.Timestamp.ToString("O"));

            command.Parameters.AddWithValue(
                "$succeeded",
                entity.Succeeded);

            command.Parameters.AddWithValue(
                "$outcome",
                entity.Outcome);

            command.Parameters.AddWithValue(
                "$createdAt",
                entity.CreatedAt.ToString("O"));

            command.Parameters.AddWithValue(
                "$updatedAt",
                entity.UpdatedAt.ToString("O"));

            var rowsAffected =
                await command.ExecuteNonQueryAsync(
                    cancellationToken);

            if (rowsAffected > 0)
                return;

            // INSERT
            command.CommandText = """
            INSERT INTO UserBehaviors
            (
                id,
                userId,
                sensorId,
                command,
                timestamp,
                succeeded,
                outcome,
                createdAt,
                updatedAt
            )
            VALUES
            (
                $id,
                $userId,
                $sensorId,
                $command,
                $timestamp,
                $succeeded,
                $outcome,
                $createdAt,
                $updatedAt
            )
            """;

            await command.ExecuteNonQueryAsync(
                cancellationToken);
        }

        // DELETE
        public async Task DeleteAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            using var connection = _database.CreateConnection();

            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();

            command.CommandText = """
                DELETE FROM UserBehaviors
                WHERE Id = $id;
                """;

            command.Parameters.AddWithValue("$id",
                id.ToString());

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static UserBehavior ReadEntity(SqliteDataReader reader)
        {
            return new UserBehavior
            {
                Id = Guid.Parse(reader.GetString(reader.GetOrdinal("Id"))),
                UserId = Guid.Parse(reader.GetString(reader.GetOrdinal("UserId"))),
                SensorId = Guid.Parse(reader.GetString(reader.GetOrdinal("SensorId"))),
                Command = reader.GetString(reader.GetOrdinal("Command")),

                Timestamp = DateTime.Parse(reader.GetString(reader.GetOrdinal("Timestamp")),
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.RoundtripKind),

                Succeeded = reader.GetBoolean(reader.GetOrdinal("Succeeded")),

                Outcome = reader.GetString(reader.GetOrdinal("Outcome")),

                CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("CreatedAt")),
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.RoundtripKind),

                UpdatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("UpdatedAt")),
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.RoundtripKind)
            };
        }
    }
}
