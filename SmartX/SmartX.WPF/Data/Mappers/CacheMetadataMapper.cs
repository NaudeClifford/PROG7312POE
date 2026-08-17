using Microsoft.Data.Sqlite;
using SmartX.WPF.Cache;

namespace SmartX.WPF.Data.Mappers
{
    public static class CacheMetadataMapper
    {
        public static CacheMetadata Map(SqliteDataReader reader)
        {
            var EntityId = reader.GetOrdinal("EntityId");
            var EntityType = reader.GetOrdinal("EntityType");
            var LastSyncedAt = reader.GetOrdinal("LastSyncedAt");

            return new CacheMetadata
            {
                EntityId = Guid.Parse(reader.GetString(EntityId)),

                EntityType = reader.GetString(EntityType),

                LastSyncedAt = DateTime.Parse(
                    reader.GetString(LastSyncedAt))
            };
        }
    }
}
