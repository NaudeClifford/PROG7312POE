using SmartX.Domain.Entities;
using SmartX.Domain.Interfaces;
using System.Text.Json;

namespace SmartX.Infrastructure.Repositories
{

    public class JsonSensorRepository : ISensorRepository
    {
        private readonly string _filePath;

        public JsonSensorRepository() 
        {
            _filePath = Path.Combine(
                AppContext.BaseDirectory,
                "Data", "Local", "sensors.json");
        }

        public async Task<Sensor?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var sensors = await GetAllAsync(cancellationToken);

            return sensors.FirstOrDefault(x => x.Id == id);
        }

        public async Task<IReadOnlyList<Sensor>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            if (!File.Exists(_filePath)) return [];

            string json = await File.ReadAllTextAsync(
                _filePath, cancellationToken);

            if (string.IsNullOrWhiteSpace(json)) return [];

            var sensors = JsonSerializer.Deserialize<List<Sensor>>(json);

            return sensors ?? [];
        }

        public async Task AddAsync(
            Sensor sensor,
            CancellationToken cancellationToken = default)
        {
            var sensors = await GetAllAsync(cancellationToken);

            var sensorList = sensors.ToList();

            sensorList.Add(sensor);

            string json = JsonSerializer.Serialize(sensorList,
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                });

            await File.WriteAllTextAsync(
                _filePath,
                json,
                cancellationToken);
        }

        public async Task UpdateAsync(
            Sensor sensor,
            CancellationToken cancellationToken = default)
        {
            var sensors = await GetAllAsync(cancellationToken);

            var sensorList = sensors.ToList();

            var existingSensor = sensorList.FirstOrDefault(
                x => x.Id == sensor.Id);

            if (existingSensor is null) return;

            var index = sensorList.IndexOf(existingSensor);

            sensorList[index] = sensor;

            string json = JsonSerializer.Serialize(sensorList,
            new JsonSerializerOptions
            {
                WriteIndented = true,
            });

            await File.WriteAllTextAsync(
                _filePath,
                json,
                cancellationToken);
        }

        public async Task DeleteAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var sensors = await GetAllAsync(cancellationToken);

            var sensorList = sensors.ToList();

            var sensor = sensorList.FirstOrDefault(x => x.Id == id);

            if (sensor is null) return;

            sensorList.Remove(sensor);

            string json = JsonSerializer.Serialize(sensorList,
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                });

            await File.WriteAllTextAsync(
                _filePath,
                json,
                cancellationToken);
        }
    }
}
