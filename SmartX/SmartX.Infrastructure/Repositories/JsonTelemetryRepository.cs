using SmartX.Domain.Entities;
using SmartX.Domain.Interfaces;
using System.Text.Json;

namespace SmartX.Infrastructure.Repositories;

public class JsonTelemetryRepository : ITelemetryRepository
{
    private readonly string _filePath;
    private readonly ISensorRepository _sensorRepository;


    public JsonTelemetryRepository(ISensorRepository sensorRepository)
    {
        _filePath = Path.Combine(
            AppContext.BaseDirectory,
            "Data", "Local", "telemetry.json");
        _sensorRepository = sensorRepository;
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

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var telemetry = await GetByIdAsync(id, cancellationToken);

        if (telemetry is null)
            return;

    }

    public async Task<IReadOnlyList<Telemetry>> GetByGatewayIdAsync(
    Guid gatewayId,
    CancellationToken cancellationToken = default)
    {
        if (gatewayId == Guid.Empty)
        {
            return [];
        }

        var sensors =
            await _sensorRepository.GetByGatewayIdAsync(
                gatewayId,
                cancellationToken);

        if (sensors is null || sensors.Count == 0)
        {
            return [];
        }

        var sensorIds =
            sensors
                .Select(x => x.Id)
                .ToHashSet();

        var telemetryRecords =
            await GetAllAsync(
                cancellationToken);

        return telemetryRecords
            .Where(x => sensorIds.Contains(x.SensorId))
            .OrderByDescending(x => x.Timestamp)
            .ToList();
    }


}