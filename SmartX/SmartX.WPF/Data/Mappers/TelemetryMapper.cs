using Microsoft.Data.Sqlite;
using SmartX.Domain.Entities;

namespace SmartX.WPF.Data.Mappers
{
    public static class TelemetryMapper
    {
        public static Telemetry Map(SqliteDataReader reader)
        {
            var id = reader.GetOrdinal("Id");
            var sensorId = reader.GetOrdinal("SensorId");
            var timestamp = reader.GetOrdinal("Timestamp");
            var voltage = reader.GetOrdinal("Voltage");
            var current = reader.GetOrdinal("Current");
            var power = reader.GetOrdinal("Power");
            var temperature = reader.GetOrdinal("Temperature");
            var CreatedAt = reader.GetOrdinal("UpdatedAt");
            var updatedAt = reader.GetOrdinal("UpdatedAt");

            return new Telemetry
            {
                Id = Guid.Parse(reader.GetString(id)),
                SensorId = Guid.Parse(reader.GetString(sensorId)),
                Timestamp = DateTime.Parse(reader.GetString(timestamp)),

                Voltage = reader.IsDBNull(voltage)
                    ? null
                    : reader.GetDouble(voltage),

                Current = reader.IsDBNull(current)
                    ? null
                    : reader.GetDouble(current),

                Power = reader.IsDBNull(power)
                    ? null
                    : reader.GetDouble(power),

                Temperature = reader.IsDBNull(temperature)
                    ? null
                    : reader.GetDouble(temperature),

                    CreatedAt = DateTime.Parse(reader.GetString(CreatedAt)),

                                UpdatedAt = DateTime.Parse(
                reader.GetString(updatedAt))
            };
        }
    }
}
