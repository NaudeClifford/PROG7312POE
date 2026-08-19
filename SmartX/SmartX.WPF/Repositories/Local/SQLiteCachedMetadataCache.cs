using SmartX.WPF.Cache;
using SmartX.WPF.Data;
using SmartX.WPF.Data.Mappers;

namespace SmartX.WPF.Repositories.Local
{
    public class SQLiteCachedMetadataCache(SmartXCacheDatabase database) : ICacheMetadataCache
    {

        private readonly SmartXCacheDatabase _database = database;

        public async Task<CacheMetadata?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            using var connection = _database.CreateConnection();

            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();

            command.CommandText = """
                SELECT
                    EntityId,
                    EntityType,
                    LastSyncedAt
                FROM CacheMetadata
                WHERE EntityId = $entityId;
                """;

            command.Parameters.AddWithValue("$entityId", id.ToString());

            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
                return null;

            return CacheMetadataMapper.Map(reader);
        }

        public async Task<IReadOnlyList<CacheMetadata>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            using var connection = _database.CreateConnection();

            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();

            command.CommandText = """
                SELECT
                    EntityId,
                    EntityType,
                    LastSyncedAt
                FROM CacheMetadata;

                """;

            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var cache = new List<CacheMetadata>();

            while (await reader.ReadAsync(cancellationToken))
                cache.Add(CacheMetadataMapper.Map(reader));

            return cache;
        }

        public async Task UpdateAsync(
            CacheMetadata data, CancellationToken cancellationToken = default)
        {
            using var connection = _database.CreateConnection();

            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();

            command.CommandText = """
                UPDATE CacheMetadata
                SET
                    EntityType = $entityType,
                    LastSyncedAt = $lastSyncedAt

               WHERE EntityId = $entityId;

               """;

            command.Parameters.AddWithValue("$entityId", data.EntityId.ToString());
            command.Parameters.AddWithValue("$entityType", data.EntityType.ToString());
            command.Parameters.AddWithValue("$lastSyncedAt", data.LastSyncedAt.ToString());

            var rowsaffected = await command.ExecuteNonQueryAsync(cancellationToken);

            if (rowsaffected == 0)
            {
                command.CommandText = """
                
                INSERT INTO CacheMetadata
                (
                    EntityId,
                    EntityType,
                    LastSyncedAt
                )
                VALUES
                (
                    $entityId,
                    $entityType,
                    $lastSyncedAt
                );
                """;

                await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        public async Task DeleteAsync(
            Guid entityId, CancellationToken cancellationToken = default)
        {
            using var connection = _database.CreateConnection();

            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();

            command.CommandText = """
                DELETE FROM CacheMetadata
                WHERE EntityId = $entityId;
                """;

            command.Parameters.AddWithValue("$entityId", entityId.ToString());

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

    }
}
