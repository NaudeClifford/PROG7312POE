using SmartX.Domain.Entities;
using SmartX.WPF.Data;
using SmartX.WPF.Data.Mappers;

namespace SmartX.WPF.Repositories.Local
{

    public class SQLiteSensorCache(SmartXCacheDatabase database) : ILocalSensorCache
    {

        private readonly SmartXCacheDatabase _database = database;

        public async Task<Sensor?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            using var connection = _database.CreateConnection();

            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();

            command.CommandText = """
                SELECT
                    Id,
                    Name,
                    DeviceIdentifier,
                    Category,
                    Location,
                    Description,
                    IsActive,
                    GatewayId,
                    CreatedAt,
                    UpdatedAt
                FROM Sensors
                WHERE Id = $id;
                """;

            command.Parameters.AddWithValue("$id", id.ToString());

            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
                return null;

            return SensorMapper.Map(reader);
        }

        public async Task<IReadOnlyList<Sensor>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            using var connection = _database.CreateConnection();

            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();

            command.CommandText = """
                SELECT
                    Id,
                    Name,
                    DeviceIdentifier,
                    Category,
                    Location,
                    Description,
                    IsActive,
                    GatewayId,
                    CreatedAt,
                    UpdatedAt
                FROM Sensors
                """;

            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var sensors = new List<Sensor>();

            while (await reader.ReadAsync(cancellationToken))
                sensors.Add(SensorMapper.Map(reader));
            
            return sensors;
        }

        public async Task UpdateAsync(
            Sensor sensor, CancellationToken cancellationToken = default)
        {
            using var connection = _database.CreateConnection();

            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();

            command.CommandText = """
                UPDATE Sensors
                SET
                    Name = $name,
                    DeviceIdentifier = $deviceIdentifier,
                    Category = $category,
                    Location = $location ,
                    Description = $description,
                    IsActive = $isActive,
                    GatewayId = $gatewayId,
                    CreatedAt = $createdAt,
                    UpdatedAt = $updatedAt
               WHERE Id = $id;
               """;

            command.Parameters.AddWithValue("$id", sensor.Id.ToString());
            command.Parameters.AddWithValue("$name", sensor.Name.ToString());
            command.Parameters.AddWithValue("$deviceIdentifier", sensor.DeviceIdentifier.ToString());
            command.Parameters.AddWithValue("$category", (int)sensor.Category);
            command.Parameters.AddWithValue("$location", sensor.Location.ToString());
            command.Parameters.AddWithValue("$description", sensor.Description.ToString());
            command.Parameters.AddWithValue("$isActive", sensor.IsActive ? 1 : 0);
            command.Parameters.AddWithValue("$gatewayId", sensor.GatewayId?.ToString() ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("$createdAt", sensor.CreatedAt.ToString());
            command.Parameters.AddWithValue("$updatedAt", sensor.UpdatedAt.ToString());

            var rowsaffected = await command.ExecuteNonQueryAsync(cancellationToken);

            if (rowsaffected == 0)
            {
                command.CommandText = """
                
                INSERT INTO Sensors
                (
                    Id,
                    Name,
                    DeviceIdentifier,
                    Category,
                    Location,
                    Description,
                    IsActive,
                    GatewayId,
                    CreatedAt,
                    UpdatedAt
                )
                VALUES
                (
                    $id,
                    $name,
                    $deviceIdentifier,
                    $category,
                    $location,
                    $description,
                    $isActive,
                    $gatewayId,
                    $createdAt,
                    $updatedAt
                );
                """;

                await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }
    }
}
