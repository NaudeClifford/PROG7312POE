using Microsoft.Data.Sqlite;
using SmartX.Domain.Entities;

namespace SmartX.WPF.Data.Mappers
{
    public class UserBehaviorMapper
    {

        public static UserBehavior Map(SqliteDataReader reader)
        {
            var id = reader.GetOrdinal("Id");
            var userId = reader.GetOrdinal("UserId");
            var sensorId = reader.GetOrdinal("SensorId");
            var command = reader.GetOrdinal("Command");
            var timestamp = reader.GetOrdinal("Timestamp");
            var succeeded = reader.GetOrdinal("Succeeded");
            var outcome = reader.GetOrdinal("Outcome");
            var createdAt = reader.GetOrdinal("CreatedAt");
            var updatedAt = reader.GetOrdinal("UpdatedAt");

            return new UserBehavior
            {
                Id = Guid.Parse(reader.GetString(id)),
                UserId = Guid.Parse(reader.GetString(userId)),
                SensorId = Guid.Parse(reader.GetString(sensorId)),
                Command = reader.GetString(command),
                Timestamp = DateTime.Parse(reader.GetString(timestamp)),
                Succeeded = reader.GetBoolean(succeeded),
                Outcome = reader.GetString(outcome),
                CreatedAt = DateTime.Parse(reader.GetString(createdAt)),
                UpdatedAt = DateTime.Parse(reader.GetString(updatedAt))
            };
        }
    }
}
