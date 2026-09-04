using SmartX.Domain.Entities;
using SmartX.Domain.Interfaces;
using System.Text.Json;

namespace SmartX.Infrastructure.Repositories;

public class JsonTelemetryRepository : ITelemetryRepository
{
    private readonly string _filePath;

    public JsonTelemetryRepository()
    {
        _filePath = Path.Combine(
            AppContext.BaseDirectory,
            "Data", "Local", "telemetry.json");
    }

    public async Task<Telemetry?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
    {
        var telemetryRecords = await GetAllAsync(cancellationToken);

        return telemetryRecords.FirstOrDefault(x => x.Id == id);
    }

    public async Task<IReadOnlyList<Telemetry>> GetBySensorIdAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default)
    {
        var telemetryRecords = await GetAllAsync(cancellationToken);

        return telemetryRecords
            .Where(x => x.SensorId == sensorId)
            .ToList();
    }
    public async Task<Telemetry?> GetLatestBySensorIdAsync(
            Guid sensorId,
            CancellationToken cancellationToken = default)
    {
        var telemetryRecords = await GetAllAsync(cancellationToken);

        return telemetryRecords          
            .Where(x => x.SensorId == sensorId)
            .OrderByDescending(x => x.Timestamp)
            .FirstOrDefault();
    }

    public async Task<IReadOnlyList<Telemetry>> GetBySensorAndDateAsync(
    Guid sensorId,
    DateTime from,
    DateTime to,
    CancellationToken cancellationToken = default)
    {
        var telemetryRecords = await GetAllAsync(cancellationToken);

        return telemetryRecords
            .Where(x => x.SensorId == sensorId &&
            x.Timestamp >= from &&
            x.Timestamp <= to)
            .OrderBy(x => x.Timestamp)
            .ToList();
    }

    private async Task<IReadOnlyList<Telemetry>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
            return [];

        string json = await File.ReadAllTextAsync(
            _filePath, cancellationToken);

        if (string.IsNullOrWhiteSpace(json)) return [];

        var telemetry = JsonSerializer.Deserialize<List<Telemetry>>(json);

        return telemetry ?? [];
    }

    public async Task AddAsync(
        Telemetry telemetry,
        CancellationToken cancellationToken = default)
    {
        var telemetryRecords = await GetAllAsync(cancellationToken);

        var telemetryList = telemetryRecords.ToList();

        var existing = telemetryList.FirstOrDefault(
                x => x.Id == telemetry.Id);

        if (existing is not null)
        {
            telemetryList.Remove(existing);
        }

        telemetryList.Add(telemetry);

        string json = JsonSerializer.Serialize(
            telemetryList, new JsonSerializerOptions
            {
                WriteIndented = true
            });

        var directory = Path.GetDirectoryName(_filePath);

        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory!);

        await File.WriteAllTextAsync(
            _filePath,
            json,
            cancellationToken);
    }
}